using System.Collections.Concurrent;
using EchoBoard.Application.Audio;
using EchoBoard.Audio.Decoding;
using EchoBoard.Audio.Mixing;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace EchoBoard.Audio.Playback;

public sealed class WasapiSoundPlaybackEngine : ISoundPlaybackEngine, IAudioRoutingEngine, IDisposable
{
    private static readonly WaveFormat MixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
    private readonly object sync = new();
    private readonly object routingSnapshotSync = new();
    private readonly ConcurrentDictionary<Guid, PlaybackSession> sessions = new();
    private readonly IMicrophoneCaptureController microphone;
    private readonly ILogger<WasapiSoundPlaybackEngine> logger;
    private readonly IAudioRenderSessionFactory renderSessions;
    private readonly AudioMixerBus monitorMixer;
    private readonly AudioMixerBus virtualMixer;
    private readonly AudioPeakMeter effectsPeakMeter = new();
    private IAudioRenderSession? monitorOutput;
    private IAudioRenderSession? virtualOutput;
    private MicrophoneSampleProvider? microphoneProvider;
    private AudioRoutingSettingsDto settings = AudioRoutingSettingsDto.Default;
    private AudioRoutingSnapshot routingSnapshot = AudioRoutingSnapshot.Stopped;
    private string? monitorError;
    private string? virtualOutputError;
    private bool virtualOutputFeedbackBlocked;
    private long lastEffectsPeakGeneration;
    private long lastMonitorPeakGeneration;
    private long lastVirtualOutputPeakGeneration;
    private Guid? latestSessionId;
    private Timer? reconnectTimer;
    private int reconnecting;
    private int microphoneReconnecting;
    private bool disposed;

    public WasapiSoundPlaybackEngine(
        IMicrophoneCaptureController microphone,
        ILogger<WasapiSoundPlaybackEngine> logger)
        : this(microphone, logger, new WasapiAudioRenderSessionFactory())
    {
    }

    internal WasapiSoundPlaybackEngine(
        IMicrophoneCaptureController microphone,
        ILogger<WasapiSoundPlaybackEngine> logger,
        IAudioRenderSessionFactory renderSessions)
    {
        this.microphone = microphone;
        this.logger = logger;
        this.renderSessions = renderSessions;
        monitorMixer = new AudioMixerBus(
            MixerFormat,
            () => settings.IsMonitorEnabled && !settings.IsMonitorMuted ? settings.MonitorVolume : 0);
        virtualMixer = new AudioMixerBus(
            MixerFormat,
            () => settings.IsVirtualOutputMuted ? 0 : settings.VirtualOutputVolume);
    }

    public Task<SoundPlaybackStartResult> PlayAsync(
        string filePath,
        double volume,
        CancellationToken cancellationToken)
    {
        return PlayAsync(filePath, volume, SoundPlaybackOptions.Default, cancellationToken);
    }

    public Task<SoundPlaybackStartResult> PlayAsync(
        string filePath,
        double volume,
        SoundPlaybackOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(disposed, this);

        DecodedAudio decoded;
        try
        {
            decoded = AudioFileDecoder.Decode(filePath, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Audio decoder failed for {FilePath}.", filePath);
            throw;
        }

        lock (sync)
        {
            var monitorActive = settings.IsMonitorEnabled &&
                                monitorOutput?.IsActive == true;
            var virtualActive = virtualOutput?.IsActive == true;
            if (!monitorActive && !virtualActive)
            {
                logger.LogError("No audio output route is available for {FilePath}.", filePath);
                throw new InvalidOperationException("No playback device is available.");
            }

            var id = Guid.NewGuid();
            var session = new PlaybackSession(
                id,
                decoded,
                Math.Clamp(volume, 0.0, 1.0),
                options.IsLoopEnabled,
                () => Math.Clamp(settings.EffectsVolume, 0.0, 1.0),
                () => settings.AreEffectsMuted,
                effectsPeakMeter.Report,
                RemoveCompletedSession);

            if (monitorActive)
            {
                session.AddRoute(monitorMixer);
            }

            if (virtualActive)
            {
                session.AddRoute(virtualMixer);
            }

            if (!sessions.TryAdd(id, session))
            {
                session.Stop();
                throw new InvalidOperationException("Audio playback could not be started.");
            }

            latestSessionId = id;
            var snapshot = session.GetSnapshot();
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Playback started for {FilePath} using codec {Codec}; monitor={MonitorActive}, virtual={VirtualActive}.",
                    filePath,
                    decoded.Codec,
                    monitorActive,
                    virtualActive);
            }
            return Task.FromResult(new SoundPlaybackStartResult(
                id,
                decoded.FilePath,
                decoded.Duration,
                monitorActive,
                virtualActive,
                snapshot));
        }
    }

    public Task StopAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var entry in sessions.ToArray())
        {
            RemoveSession(entry.Key, entry.Value);
        }

        return Task.CompletedTask;
    }

    public Task StopSoundAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var entry in sessions.Where(entry =>
                     string.Equals(entry.Value.FilePath, filePath, StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            RemoveSession(entry.Key, entry.Value);
        }

        return Task.CompletedTask;
    }

    public Task TogglePauseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var activeSessions = sessions.Values.ToArray();
        var shouldPause = activeSessions.Any(session => !session.IsPaused);
        foreach (var session in activeSessions)
        {
            session.SetPaused(shouldPause);
        }

        return Task.CompletedTask;
    }

    public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var latest = GetLatestSession();
        latest?.Seek(position);
        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(double volume, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        settings = settings with { EffectsVolume = Math.Clamp(volume, 0.0, 1.0) };
        return Task.CompletedTask;
    }

    public SoundPlaybackSnapshot GetSnapshot()
    {
        return GetLatestSession()?.GetSnapshot() ?? SoundPlaybackSnapshot.Idle;
    }

    public async Task InitializeAsync(AudioRoutingSettingsDto settings, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await ApplySettingsAsync(settings, cancellationToken);
    }

    public async Task ApplySettingsAsync(AudioRoutingSettingsDto settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(disposed, this);
        var previous = this.settings;
        this.settings = Validate(settings);
        var reconnectMicrophone = microphoneProvider is null ||
                                  !string.Equals(previous.InputDeviceId, this.settings.InputDeviceId, StringComparison.Ordinal);
        if (reconnectMicrophone)
        {
            await ConfigureMicrophoneAsync(cancellationToken);
        }
        else
        {
            await microphone.SetGainAsync(this.settings.MicrophoneVolume, cancellationToken);
            await microphone.SetMutedAsync(this.settings.IsMicrophoneMuted, cancellationToken);
        }

        virtualOutputFeedbackBlocked = AudioEndpointClassifier.WouldCreateFeedback(
            this.settings.InputDeviceName,
            this.settings.VirtualOutputDeviceName);
        if (virtualOutputFeedbackBlocked)
        {
            virtualOutputError = "The selected microphone and virtual output belong to the same endpoint family.";
            logger.LogWarning(
                "Virtual output {VirtualOutput} was blocked because it matches microphone endpoint family {Input}.",
                this.settings.VirtualOutputDeviceName,
                this.settings.InputDeviceName);
        }

        var monitorChanged =
            previous.IsMonitorEnabled != this.settings.IsMonitorEnabled ||
            !string.Equals(previous.MonitorDeviceId, this.settings.MonitorDeviceId, StringComparison.Ordinal);
        var virtualChanged =
            !string.Equals(previous.VirtualOutputDeviceId, this.settings.VirtualOutputDeviceId, StringComparison.Ordinal) ||
            !string.Equals(previous.InputDeviceId, this.settings.InputDeviceId, StringComparison.Ordinal) ||
            AudioEndpointClassifier.WouldCreateFeedback(previous.InputDeviceName, previous.VirtualOutputDeviceName) !=
            virtualOutputFeedbackBlocked;
        lock (sync)
        {
            if (monitorChanged || MonitorNeedsConfiguration())
            {
                ConfigureMonitorOutput();
            }

            if (virtualChanged || VirtualOutputNeedsConfiguration())
            {
                ConfigureVirtualOutput();
            }
        }

        reconnectTimer ??= new Timer(_ => TryReconnectOutputs(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        UpdateRoutingSnapshot();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopAllAsync(cancellationToken);
        lock (sync)
        {
            DisposeOutput(ref monitorOutput);
            DisposeOutput(ref virtualOutput);
            monitorError = null;
            virtualOutputError = null;
            if (microphoneProvider is not null)
            {
                virtualMixer.RemoveInput(microphoneProvider);
                microphoneProvider = null;
            }
        }

        await microphone.StopAsync(cancellationToken);
        lock (routingSnapshotSync)
        {
            routingSnapshot = AudioRoutingSnapshot.Stopped;
        }
        reconnectTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public AudioRoutingSnapshot GetRoutingSnapshot()
    {
        UpdateRoutingSnapshot(force: false);
        lock (routingSnapshotSync)
        {
            return routingSnapshot;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        reconnectTimer?.Dispose();
        reconnectTimer = null;
        foreach (var entry in sessions.ToArray())
        {
            RemoveSession(entry.Key, entry.Value);
        }

        lock (sync)
        {
            DisposeOutput(ref monitorOutput);
            DisposeOutput(ref virtualOutput);
        }
    }

    private async Task ConfigureMicrophoneAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(settings.InputDeviceId))
        {
            var devices = await microphone.ListInputDevicesAsync(cancellationToken);
            var selected = devices.FirstOrDefault(device =>
                string.Equals(device.Id, settings.InputDeviceId, StringComparison.Ordinal));
            if (selected is not null)
            {
                await microphone.SelectDeviceAsync(selected, cancellationToken);
            }
        }

        await microphone.SetGainAsync(settings.MicrophoneVolume, cancellationToken);
        await microphone.SetMutedAsync(settings.IsMicrophoneMuted, cancellationToken);
        await microphone.StartAsync(cancellationToken);

        lock (sync)
        {
            if (microphoneProvider is not null)
            {
                virtualMixer.RemoveInput(microphoneProvider);
                microphoneProvider = null;
            }

            if (microphone.CurrentSource is { } source)
            {
                microphoneProvider = new MicrophoneSampleProvider(source);
                virtualMixer.AddInput(ToMixerFormat(microphoneProvider));
            }
        }
    }

    private void ConfigureMonitorOutput()
    {
        DisposeOutput(ref monitorOutput);
        monitorError = null;
        if (!settings.IsMonitorEnabled)
        {
            return;
        }

        try
        {
            monitorOutput = CreateOutput(
                settings.MonitorDeviceId,
                monitorMixer,
                "monitor",
                HandleMonitorStopped);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Monitor route started on {DeviceName} using {Format}.",
                    monitorOutput.DeviceName,
                    monitorOutput.Format.DisplayText);
            }
        }
        catch (Exception exception)
        {
            monitorError = exception.Message;
            logger.LogError(exception, "Monitor output is unavailable: {DeviceName}.", settings.MonitorDeviceName);
        }
    }

    private void ConfigureVirtualOutput()
    {
        DisposeOutput(ref virtualOutput);
        if (string.IsNullOrWhiteSpace(settings.VirtualOutputDeviceId))
        {
            virtualOutputError = null;
            return;
        }

        if (virtualOutputFeedbackBlocked)
        {
            return;
        }

        virtualOutputError = null;
        try
        {
            virtualOutput = CreateOutput(
                settings.VirtualOutputDeviceId,
                virtualMixer,
                "virtual output",
                HandleVirtualOutputStopped);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Virtual output route started on {DeviceName} using {Format}.",
                    virtualOutput.DeviceName,
                    virtualOutput.Format.DisplayText);
            }
        }
        catch (Exception exception)
        {
            virtualOutputError = exception.Message;
            logger.LogError(exception, "Virtual output is unavailable: {DeviceName}.", settings.VirtualOutputDeviceName);
        }
    }

    private bool MonitorNeedsConfiguration()
    {
        return settings.IsMonitorEnabled && monitorOutput?.IsActive != true;
    }

    private bool VirtualOutputNeedsConfiguration()
    {
        return !virtualOutputFeedbackBlocked &&
               !string.IsNullOrWhiteSpace(settings.VirtualOutputDeviceId) &&
               virtualOutput?.IsActive != true;
    }

    private bool OutputsNeedConfiguration()
    {
        return MonitorNeedsConfiguration() || VirtualOutputNeedsConfiguration();
    }

    private void TryReconnectOutputs()
    {
        if (disposed || Interlocked.Exchange(ref reconnecting, 1) != 0)
        {
            return;
        }

        try
        {
            if (OutputsNeedConfiguration())
            {
                ReconnectMissingOutputs();
                UpdateRoutingSnapshot();
            }

            var microphoneState = microphone.GetSnapshot().State;
            if (microphoneState is MicrophoneCaptureState.Unavailable or MicrophoneCaptureState.Failed)
            {
                _ = TryReconnectMicrophoneAsync();
            }
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Audio route reconnection will be retried.");
        }
        finally
        {
            Interlocked.Exchange(ref reconnecting, 0);
        }
    }

    private async Task TryReconnectMicrophoneAsync()
    {
        if (disposed || Interlocked.Exchange(ref microphoneReconnecting, 1) != 0)
        {
            return;
        }

        try
        {
            await ConfigureMicrophoneAsync(CancellationToken.None);
            UpdateRoutingSnapshot();
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Microphone reconnection will be retried.");
        }
        finally
        {
            Interlocked.Exchange(ref microphoneReconnecting, 0);
        }
    }

    private void ReconnectMissingOutputs()
    {
        lock (sync)
        {
            if (MonitorNeedsConfiguration())
            {
                ConfigureMonitorOutput();
            }

            if (VirtualOutputNeedsConfiguration())
            {
                ConfigureVirtualOutput();
            }
        }
    }

    private IAudioRenderSession CreateOutput(
        string? deviceId,
        ISampleProvider source,
        string route,
        EventHandler<AudioRenderSessionStoppedEventArgs> stopped)
    {
        var output = renderSessions.Create(deviceId, source, route);
        output.Stopped += stopped;
        try
        {
            output.Start();
            if (!output.IsActive)
            {
                throw new InvalidOperationException($"{route} could not be started.");
            }

            return output;
        }
        catch
        {
            output.Stopped -= stopped;
            output.Dispose();
            throw;
        }
    }

    private void UpdateRoutingSnapshot(bool force = true)
    {
        lock (routingSnapshotSync)
        {
            if (!force && routingSnapshot.EngineState == AudioRouteState.Stopped)
            {
                return;
            }

            if (!force &&
                effectsPeakMeter.Generation == lastEffectsPeakGeneration &&
                monitorMixer.PeakGeneration == lastMonitorPeakGeneration &&
                virtualMixer.PeakGeneration == lastVirtualOutputPeakGeneration)
            {
                return;
            }

            var microphoneSnapshot = microphone.GetSnapshot();
            var monitorState = monitorOutput?.IsActive == true
                ? AudioRouteState.Active
                : settings.IsMonitorEnabled
                    ? monitorError is null ? AudioRouteState.Unavailable : AudioRouteState.Failed
                    : AudioRouteState.Stopped;
            var virtualState = string.IsNullOrWhiteSpace(settings.VirtualOutputDeviceId)
                ? AudioRouteState.Unconfigured
                : virtualOutput?.IsActive == true
                    ? AudioRouteState.Active
                    : virtualOutputFeedbackBlocked || virtualOutputError is not null
                        ? AudioRouteState.Failed
                        : AudioRouteState.Unavailable;
            var engineState = monitorState == AudioRouteState.Active || virtualState == AudioRouteState.Active
                ? AudioRouteState.Active
                : AudioRouteState.Unavailable;
            var status = virtualState switch
            {
                AudioRouteState.Unconfigured => "Local playback active. Select a virtual output to transmit.",
                AudioRouteState.Active => "Microphone and effects are routed to the selected virtual output.",
                AudioRouteState.Failed when virtualOutputFeedbackBlocked =>
                    "Virtual output blocked to prevent an audio feedback loop.",
                AudioRouteState.Failed => "Virtual output failed. Local playback remains available.",
                AudioRouteState.Unavailable => "Virtual output is unavailable and will be reconnected automatically.",
                _ when engineState == AudioRouteState.Active => "Audio engine active.",
                _ => "No output route is active."
            };
            var effectsPeak = effectsPeakMeter.Consume();
            var monitorPeak = monitorMixer.ConsumePeakLevel();
            var virtualOutputPeak = virtualMixer.ConsumePeakLevel();
            lastEffectsPeakGeneration = effectsPeakMeter.Generation;
            lastMonitorPeakGeneration = monitorMixer.PeakGeneration;
            lastVirtualOutputPeakGeneration = virtualMixer.PeakGeneration;

            routingSnapshot = new AudioRoutingSnapshot(
                engineState,
                microphoneSnapshot.State switch
                {
                    MicrophoneCaptureState.Active => AudioRouteState.Active,
                    MicrophoneCaptureState.Starting => AudioRouteState.Starting,
                    MicrophoneCaptureState.Unavailable => AudioRouteState.Unavailable,
                    MicrophoneCaptureState.Failed => AudioRouteState.Failed,
                    _ => AudioRouteState.Stopped
                },
                monitorState,
                virtualState,
                microphoneSnapshot.SelectedDeviceName,
                settings.MonitorDeviceName,
                settings.VirtualOutputDeviceName,
                status,
                microphoneSnapshot.ErrorMessage,
                new AudioStreamFormatDto(48000, 2, 32, "IEEE Float"),
                monitorError,
                virtualOutputError,
                monitorOutput?.Format,
                virtualOutput?.Format,
                effectsPeak,
                monitorPeak,
                virtualOutputPeak);
        }
    }

    private void HandleMonitorStopped(object? sender, AudioRenderSessionStoppedEventArgs args)
    {
        lock (sync)
        {
            if (!ReferenceEquals(sender, monitorOutput))
            {
                return;
            }

            monitorError = args.Exception?.Message ?? "The monitor endpoint stopped unexpectedly.";
            logger.LogError(args.Exception, "Monitor route stopped unexpectedly.");
            DisposeOutput(ref monitorOutput);
            UpdateRoutingSnapshot();
        }
    }

    private void HandleVirtualOutputStopped(object? sender, AudioRenderSessionStoppedEventArgs args)
    {
        lock (sync)
        {
            if (!ReferenceEquals(sender, virtualOutput))
            {
                return;
            }

            virtualOutputError = args.Exception?.Message ?? "The virtual output endpoint stopped unexpectedly.";
            logger.LogError(args.Exception, "Virtual output route stopped unexpectedly.");
            DisposeOutput(ref virtualOutput);
            UpdateRoutingSnapshot();
        }
    }

    private PlaybackSession? GetLatestSession()
    {
        if (latestSessionId is Guid id && sessions.TryGetValue(id, out var session))
        {
            return session;
        }

        return sessions.Values.LastOrDefault();
    }

    private void RemoveCompletedSession(Guid id)
    {
        if (sessions.TryGetValue(id, out var session))
        {
            RemoveSession(id, session);
        }
    }

    private void RemoveSession(Guid id, PlaybackSession session)
    {
        if (sessions.TryRemove(id, out _))
        {
            session.Stop();
        }
    }

    private static AudioRoutingSettingsDto Validate(AudioRoutingSettingsDto value)
    {
        static double Volume(double input) => Math.Clamp(
            double.IsFinite(input) ? input : 1.0,
            0.0,
            1.0);

        return value with
        {
            MicrophoneVolume = Volume(value.MicrophoneVolume),
            EffectsVolume = Volume(value.EffectsVolume),
            MonitorVolume = Volume(value.MonitorVolume),
            VirtualOutputVolume = Volume(value.VirtualOutputVolume)
        };
    }

    private static ISampleProvider ToMixerFormat(ISampleProvider source)
    {
        ISampleProvider provider = source;
        if (provider.WaveFormat.SampleRate != MixerFormat.SampleRate)
        {
            provider = new WdlResamplingSampleProvider(provider, MixerFormat.SampleRate);
        }

        return provider.WaveFormat.Channels switch
        {
            1 => new MonoToStereoSampleProvider(provider),
            2 => provider,
            _ => throw new InvalidOperationException("Only mono and stereo microphone formats are supported.")
        };
    }

    private void DisposeOutput(ref IAudioRenderSession? output)
    {
        if (output is null)
        {
            return;
        }

        output.Stopped -= HandleMonitorStopped;
        output.Stopped -= HandleVirtualOutputStopped;
        try
        {
            output.Stop();
        }
        finally
        {
            output.Dispose();
            output = null;
        }
    }

    private sealed class MicrophoneSampleProvider : ISampleProvider
    {
        private readonly IMicrophonePcmSource source;

        public MicrophoneSampleProvider(IMicrophonePcmSource source)
        {
            this.source = source;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(
                source.Format.SampleRate,
                source.Format.Channels);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            var destination = buffer.AsSpan(offset, count);
            if (!source.TryRead(destination, out var written))
            {
                destination.Clear();
                return count;
            }

            destination[written..].Clear();
            return count;
        }
    }

    private sealed class PlaybackSession
    {
        private readonly object sessionSync = new();
        private readonly Guid id;
        private readonly DecodedAudio audio;
        private readonly double soundVolume;
        private readonly bool loop;
        private readonly Func<double> effectsVolume;
        private readonly Func<bool> effectsMuted;
        private readonly Action<float> reportEffectsPeak;
        private readonly Action<Guid> completed;
        private readonly List<SessionSampleProvider> providers = [];
        private bool stopped;
        private bool paused;
        private int completionRaised;

        public PlaybackSession(
            Guid id,
            DecodedAudio audio,
            double soundVolume,
            bool loop,
            Func<double> effectsVolume,
            Func<bool> effectsMuted,
            Action<float> reportEffectsPeak,
            Action<Guid> completed)
        {
            this.id = id;
            this.audio = audio;
            this.soundVolume = soundVolume;
            this.loop = loop;
            this.effectsVolume = effectsVolume;
            this.effectsMuted = effectsMuted;
            this.reportEffectsPeak = reportEffectsPeak;
            this.completed = completed;
        }

        public string FilePath => audio.FilePath;

        public bool IsPaused
        {
            get
            {
                lock (sessionSync)
                {
                    return paused;
                }
            }
        }

        public void AddRoute(AudioMixerBus mixer)
        {
            var provider = new SessionSampleProvider(
                audio.Samples,
                loop,
                () => IsPaused,
                () => stopped,
                () => effectsMuted() ? 0.0 : soundVolume * effectsVolume(),
                reportEffectsPeak,
                OnProviderCompleted);
            providers.Add(provider);
            mixer.AddInput(provider);
        }

        public void SetPaused(bool value)
        {
            lock (sessionSync)
            {
                paused = value;
            }
        }

        public void Seek(TimeSpan position)
        {
            var sample = (long)(Math.Clamp(position.TotalSeconds, 0, audio.Duration.TotalSeconds) *
                                DecodedAudio.SampleRate * DecodedAudio.Channels);
            foreach (var provider in providers)
            {
                provider.Seek(sample);
            }
        }

        public void Stop()
        {
            lock (sessionSync)
            {
                stopped = true;
                paused = false;
            }
        }

        public SoundPlaybackSnapshot GetSnapshot()
        {
            var samplePosition = providers.Count == 0 ? 0 : providers.Max(provider => provider.Position);
            var position = TimeSpan.FromSeconds(
                samplePosition / (double)(DecodedAudio.SampleRate * DecodedAudio.Channels));
            return new SoundPlaybackSnapshot(
                audio.FilePath,
                position,
                audio.Duration,
                !stopped && !paused,
                !stopped && paused);
        }

        private void OnProviderCompleted()
        {
            if (Interlocked.Exchange(ref completionRaised, 1) == 0)
            {
                completed(id);
            }
        }
    }

    private sealed class SessionSampleProvider : ISampleProvider
    {
        private readonly float[] samples;
        private readonly bool loop;
        private readonly Func<bool> paused;
        private readonly Func<bool> stopped;
        private readonly Func<double> volume;
        private readonly Action<float> reportPeak;
        private readonly Action completed;
        private long position;
        private int completionRaised;

        public SessionSampleProvider(
            float[] samples,
            bool loop,
            Func<bool> paused,
            Func<bool> stopped,
            Func<double> volume,
            Action<float> reportPeak,
            Action completed)
        {
            this.samples = samples;
            this.loop = loop;
            this.paused = paused;
            this.stopped = stopped;
            this.volume = volume;
            this.reportPeak = reportPeak;
            this.completed = completed;
        }

        public WaveFormat WaveFormat => MixerFormat;

        public long Position => Interlocked.Read(ref position);

        public bool IsCompleted => Volatile.Read(ref completionRaised) != 0;

        public int Read(float[] buffer, int offset, int count)
        {
            if (stopped())
            {
                Complete();
                return 0;
            }

            if (paused())
            {
                Array.Clear(buffer, offset, count);
                return count;
            }

            var written = 0;
            var gain = (float)Math.Clamp(volume(), 0.0, 1.0);
            while (written < count)
            {
                var current = Interlocked.Read(ref position);
                if (current >= samples.Length)
                {
                    if (!loop)
                    {
                        Complete();
                        break;
                    }

                    Interlocked.Exchange(ref position, 0);
                    current = 0;
                }

                var available = (int)Math.Min(count - written, samples.Length - current);
                for (var index = 0; index < available; index++)
                {
                    buffer[offset + written + index] = Math.Clamp(samples[current + index] * gain, -1f, 1f);
                }

                Interlocked.Add(ref position, available);
                written += available;
            }

            var peak = 0f;
            for (var index = offset; index < offset + written; index++)
            {
                peak = Math.Max(peak, Math.Abs(buffer[index]));
            }

            reportPeak(peak);
            return written;
        }

        public void Seek(long sample)
        {
            Interlocked.Exchange(ref position, Math.Clamp(sample, 0, samples.LongLength));
            Interlocked.Exchange(ref completionRaised, 0);
        }

        private void Complete()
        {
            if (Interlocked.Exchange(ref completionRaised, 1) == 0)
            {
                completed();
            }
        }
    }
}

public sealed class WasapiAudioOutputDeviceEnumerator : IAudioOutputDeviceEnumerator
{
    public Task<IReadOnlyList<AudioOutputDeviceDto>> ListOutputDevicesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var enumerator = new MMDeviceEnumerator();
        string? defaultId = null;
        try
        {
            using var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            defaultId = defaultDevice.ID;
        }
        catch (NAudio.MmException)
        {
            // A machine with no render endpoint starts in degraded mode.
        }

        var devices = new List<AudioOutputDeviceDto>();
        foreach (var device in enumerator.EnumerateAudioEndPoints(
                     DataFlow.Render,
                     DeviceState.Active | DeviceState.Unplugged))
        {
            using (device)
            {
                devices.Add(new AudioOutputDeviceDto(
                    device.ID,
                    device.FriendlyName,
                    string.Equals(device.ID, defaultId, StringComparison.Ordinal),
                    device.State == DeviceState.Active,
                    AudioEndpointClassifier.IsVirtualOutputCandidate(device.FriendlyName),
                    AudioEndpointClassifier.GetFamily(device.FriendlyName)));
            }
        }

        var ordered = devices
            .OrderByDescending(device => device.IsDefault)
            .ThenByDescending(device => device.IsVirtualOutputCandidate)
            .ThenBy(device => device.Name)
            .ToArray();
        return Task.FromResult<IReadOnlyList<AudioOutputDeviceDto>>(ordered);
    }
}
