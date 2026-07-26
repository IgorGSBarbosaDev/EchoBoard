using EchoBoard.Application.Audio;
using EchoBoard.Audio.Playback;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using Xunit;

namespace EchoBoard.Audio.Tests;

public sealed class WasapiSoundPlaybackEngineRoutingTests
{
    [Fact]
    public async Task VirtualRouteContainsVoiceWhileMonitorRemainsSilentWithoutEffects()
    {
        var microphone = new FakeMicrophoneCaptureController(0.2f);
        var renders = new FakeRenderSessionFactory();
        using var engine = CreateEngine(microphone, renders);

        await engine.InitializeAsync(Settings(), TestContext.Current.CancellationToken);

        renders.Latest("virtual output").Read(32).Should().OnlyContain(sample => CloseTo(sample, 0.2f));
        renders.Latest("monitor").Read(32).Should().OnlyContain(sample => CloseTo(sample, 0f));
        microphone.StartCount.Should().Be(1);
    }

    [Fact]
    public async Task VirtualRouteMixesVoiceAndEffectWhileMonitorReceivesOnlyEffect()
    {
        var filePath = CreateWaveFile(0.25f, sampleFrames: 4096);
        try
        {
            var microphone = new FakeMicrophoneCaptureController(0.2f);
            var renders = new FakeRenderSessionFactory();
            using var engine = CreateEngine(microphone, renders);
            await engine.InitializeAsync(Settings(), TestContext.Current.CancellationToken);

            var started = await engine.PlayAsync(
                filePath,
                1,
                TestContext.Current.CancellationToken);

            started.IsMonitorActive.Should().BeTrue();
            started.IsVirtualOutputActive.Should().BeTrue();
            renders.Latest("virtual output").Read(64).Should().OnlyContain(sample => CloseTo(sample, 0.45f));
            renders.Latest("monitor").Read(64).Should().OnlyContain(sample => CloseTo(sample, 0.2f));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task MutedMicrophoneAllowsEffectOnlyOnVirtualRoute()
    {
        var filePath = CreateWaveFile(0.25f, sampleFrames: 4096);
        try
        {
            var renders = new FakeRenderSessionFactory();
            using var engine = CreateEngine(new FakeMicrophoneCaptureController(0.2f), renders);
            await engine.InitializeAsync(
                Settings() with { IsMicrophoneMuted = true },
                TestContext.Current.CancellationToken);
            await engine.PlayAsync(filePath, 1, TestContext.Current.CancellationToken);

            renders.Latest("virtual output").Read(64).Should().OnlyContain(sample => CloseTo(sample, 0.25f));
            renders.Latest("monitor").Read(64).Should().OnlyContain(sample => CloseTo(sample, 0.2f));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task VoiceContinuesAfterEffectFinishesWithoutRecreatingRoutes()
    {
        var filePath = CreateWaveFile(0.25f, sampleFrames: 64);
        try
        {
            var microphone = new FakeMicrophoneCaptureController(0.2f);
            var renders = new FakeRenderSessionFactory();
            using var engine = CreateEngine(microphone, renders);
            await engine.InitializeAsync(Settings(), TestContext.Current.CancellationToken);
            var virtualRoute = renders.Latest("virtual output");
            var monitorRoute = renders.Latest("monitor");
            await engine.PlayAsync(filePath, 1, TestContext.Current.CancellationToken);

            virtualRoute.Read(256);
            monitorRoute.Read(256);
            var after = virtualRoute.Read(32);

            after.Should().OnlyContain(sample => CloseTo(sample, 0.2f));
            renders.CreatedFor("virtual output").Should().Be(1);
            renders.CreatedFor("monitor").Should().Be(1);
            microphone.StartCount.Should().Be(1);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task FailedVirtualRouteDoesNotStopMonitorOrLocalPlayback()
    {
        var filePath = CreateWaveFile(0.25f, sampleFrames: 512);
        try
        {
            var renders = new FakeRenderSessionFactory { FailVirtualOutput = true };
            using var engine = CreateEngine(new FakeMicrophoneCaptureController(0.2f), renders);
            await engine.InitializeAsync(Settings(), TestContext.Current.CancellationToken);

            var snapshot = engine.GetRoutingSnapshot();
            var started = await engine.PlayAsync(filePath, 1, TestContext.Current.CancellationToken);

            snapshot.MonitorState.Should().Be(AudioRouteState.Active);
            snapshot.VirtualOutputState.Should().Be(AudioRouteState.Failed);
            snapshot.VirtualOutputErrorMessage.Should().Contain("virtual output");
            started.IsMonitorActive.Should().BeTrue();
            started.IsVirtualOutputActive.Should().BeFalse();
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReconnectsOnlyDisconnectedVirtualRouteAndKeepsMicrophoneAndMonitor()
    {
        var microphone = new FakeMicrophoneCaptureController(0.2f);
        var renders = new FakeRenderSessionFactory();
        using var engine = CreateEngine(microphone, renders);
        var settings = Settings();
        await engine.InitializeAsync(settings, TestContext.Current.CancellationToken);
        var monitor = renders.Latest("monitor");
        renders.Latest("virtual output").Disconnect(new IOException("cable removed"));

        engine.GetRoutingSnapshot().VirtualOutputState.Should().Be(AudioRouteState.Failed);
        await engine.ApplySettingsAsync(settings, TestContext.Current.CancellationToken);

        engine.GetRoutingSnapshot().VirtualOutputState.Should().Be(AudioRouteState.Active);
        renders.Latest("monitor").Should().BeSameAs(monitor);
        renders.CreatedFor("monitor").Should().Be(1);
        renders.CreatedFor("virtual output").Should().Be(2);
        microphone.StartCount.Should().Be(1);
    }

    [Fact]
    public async Task AppliesVolumesAndMutesWithoutRecreatingRenderSessions()
    {
        var filePath = CreateWaveFile(0.25f, sampleFrames: 4096);
        try
        {
            var renders = new FakeRenderSessionFactory();
            using var engine = CreateEngine(new FakeMicrophoneCaptureController(0.2f), renders);
            var settings = Settings();
            await engine.InitializeAsync(settings, TestContext.Current.CancellationToken);
            await engine.PlayAsync(filePath, 1, TestContext.Current.CancellationToken);
            var virtualRoute = renders.Latest("virtual output");
            var monitorRoute = renders.Latest("monitor");

            await engine.ApplySettingsAsync(
                settings with
                {
                    MicrophoneVolume = 0.5,
                    EffectsVolume = 0.5,
                    MonitorVolume = 0.4
                },
                TestContext.Current.CancellationToken);

            virtualRoute.Read(64).Should().OnlyContain(sample => CloseTo(sample, 0.225f));
            monitorRoute.Read(64).Should().OnlyContain(sample => CloseTo(sample, 0.05f));
            renders.CreatedFor("virtual output").Should().Be(1);
            renders.CreatedFor("monitor").Should().Be(1);

            await engine.ApplySettingsAsync(
                settings with { IsVirtualOutputMuted = true, IsMonitorMuted = true },
                TestContext.Current.CancellationToken);
            virtualRoute.Read(32).Should().OnlyContain(sample => CloseTo(sample, 0f));
            monitorRoute.Read(32).Should().OnlyContain(sample => CloseTo(sample, 0f));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task BlocksVirtualOutputFromSameEndpointFamilyToPreventFeedback()
    {
        var renders = new FakeRenderSessionFactory();
        using var engine = CreateEngine(new FakeMicrophoneCaptureController(0.2f), renders);
        var settings = Settings() with
        {
            InputDeviceName = "Microphone (NVIDIA Broadcast)",
            VirtualOutputDeviceName = "Speakers (NVIDIA Broadcast)"
        };

        await engine.InitializeAsync(settings, TestContext.Current.CancellationToken);

        var snapshot = engine.GetRoutingSnapshot();
        snapshot.VirtualOutputState.Should().Be(AudioRouteState.Failed);
        snapshot.StatusMessage.Should().Contain("feedback");
        renders.CreatedFor("virtual output").Should().Be(0);
        snapshot.MonitorState.Should().Be(AudioRouteState.Active);
    }

    [Fact]
    public void FormatAdapterResamplesAndConvertsChannelsForEndpointMixFormat()
    {
        var source = new ConstantSampleProvider(0.25f, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));

        var mono = AudioRenderFormatAdapter.Adapt(source, WaveFormat.CreateIeeeFloatWaveFormat(44100, 1));
        var surround = AudioRenderFormatAdapter.Adapt(source, WaveFormat.CreateIeeeFloatWaveFormat(48000, 6));

        mono.WaveFormat.SampleRate.Should().Be(44100);
        mono.WaveFormat.Channels.Should().Be(1);
        surround.WaveFormat.SampleRate.Should().Be(48000);
        surround.WaveFormat.Channels.Should().Be(6);
    }

    [Theory]
    [InlineData("CABLE Input (VB-Audio Virtual Cable)", true, "vb-cable")]
    [InlineData("VoiceMeeter AUX Input", true, "voicemeeter-aux")]
    [InlineData("Speakers (NVIDIA Broadcast)", false, "nvidia-broadcast")]
    [InlineData("Alto-falantes (Realtek(R) Audio)", false, null)]
    public void EndpointClassifierIdentifiesVirtualCandidates(
        string name,
        bool expectedCandidate,
        string? expectedFamily)
    {
        AudioEndpointClassifier.IsVirtualOutputCandidate(name).Should().Be(expectedCandidate);
        AudioEndpointClassifier.GetFamily(name).Should().Be(expectedFamily);
    }

    private static WasapiSoundPlaybackEngine CreateEngine(
        IMicrophoneCaptureController microphone,
        IAudioRenderSessionFactory renders)
    {
        return new WasapiSoundPlaybackEngine(
            microphone,
            NullLogger<WasapiSoundPlaybackEngine>.Instance,
            renders);
    }

    private static AudioRoutingSettingsDto Settings()
    {
        return AudioRoutingSettingsDto.Default with
        {
            InputDeviceId = "microphone",
            InputDeviceName = "Microphone (NVIDIA Broadcast)",
            MonitorDeviceId = "monitor",
            MonitorDeviceName = "Alto-falantes (Realtek(R) Audio)",
            VirtualOutputDeviceId = "virtual",
            VirtualOutputDeviceName = "CABLE Input (VB-Audio Virtual Cable)",
            IsMonitorEnabled = true
        };
    }

    private static string CreateWaveFile(float value, int sampleFrames)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"echoboard-routing-{Guid.NewGuid():N}.wav");
        var format = new WaveFormat(48000, 16, 2);
        using var writer = new WaveFileWriter(filePath, format);
        var sample = (short)Math.Round(Math.Clamp(value, -1f, 1f) * short.MaxValue);
        var frame = new byte[format.BlockAlign];
        BitConverter.TryWriteBytes(frame.AsSpan(0, 2), sample);
        BitConverter.TryWriteBytes(frame.AsSpan(2, 2), sample);
        for (var index = 0; index < sampleFrames; index++)
        {
            writer.Write(frame, 0, frame.Length);
        }

        return filePath;
    }

    private static bool CloseTo(float actual, float expected)
    {
        return Math.Abs(actual - expected) < 0.002f;
    }

    private sealed class FakeMicrophoneCaptureController : IMicrophoneCaptureController
    {
        private readonly FakePcmSource source = new();
        private AudioInputDeviceDto selected = new(
            "microphone",
            "Microphone (NVIDIA Broadcast)",
            true,
            true,
            "nvidia-broadcast");
        private double gain = 1;
        private bool muted;

        public FakeMicrophoneCaptureController(float sample)
        {
            source.ReadSample = () => muted ? 0f : sample * (float)gain;
        }

        public int StartCount { get; private set; }

        public IMicrophonePcmSource? CurrentSource { get; private set; }

        public Task<IReadOnlyList<AudioInputDeviceDto>> ListInputDevicesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AudioInputDeviceDto>>([selected]);
        }

        public Task RestoreSelectionAsync(MicrophoneSettingsDto settings, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SelectDeviceAsync(AudioInputDeviceDto device, CancellationToken cancellationToken)
        {
            selected = device;
            return Task.CompletedTask;
        }

        public Task SetGainAsync(double gain, CancellationToken cancellationToken)
        {
            this.gain = gain;
            return Task.CompletedTask;
        }

        public Task SetMutedAsync(bool isMuted, CancellationToken cancellationToken)
        {
            muted = isMuted;
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCount++;
            CurrentSource = source;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            CurrentSource = null;
            return Task.CompletedTask;
        }

        public MicrophoneCaptureSnapshot GetSnapshot()
        {
            return new MicrophoneCaptureSnapshot(
                CurrentSource is null ? MicrophoneCaptureState.Stopped : MicrophoneCaptureState.Active,
                selected.Id,
                selected.Name,
                Math.Abs(source.ReadSample()),
                1,
                false,
                CurrentSource is null ? "Stopped" : "Capturing",
                null,
                source.Format);
        }
    }

    private sealed class FakePcmSource : IMicrophonePcmSource
    {
        public Func<float> ReadSample { get; set; } = () => 0;

        public AudioStreamFormatDto Format { get; } = new(48000, 2, 32, "IEEE Float");

        public bool TryRead(Span<float> destination, out int samplesWritten)
        {
            destination.Fill(ReadSample());
            samplesWritten = destination.Length;
            return true;
        }
    }

    private sealed class FakeRenderSessionFactory : IAudioRenderSessionFactory
    {
        private readonly List<FakeRenderSession> sessions = [];

        public bool FailVirtualOutput { get; set; }

        public IAudioRenderSession Create(string? deviceId, ISampleProvider source, string routeName)
        {
            if (FailVirtualOutput && routeName == "virtual output")
            {
                throw new InvalidOperationException("virtual output unavailable");
            }

            var session = new FakeRenderSession(deviceId ?? "default", routeName, source);
            sessions.Add(session);
            return session;
        }

        public FakeRenderSession Latest(string routeName)
        {
            return sessions.Last(session => session.RouteName == routeName);
        }

        public int CreatedFor(string routeName)
        {
            return sessions.Count(session => session.RouteName == routeName);
        }
    }

    private sealed class FakeRenderSession(
        string deviceId,
        string routeName,
        ISampleProvider source) : IAudioRenderSession
    {
        public event EventHandler<AudioRenderSessionStoppedEventArgs>? Stopped;

        public string RouteName { get; } = routeName;

        public string DeviceId { get; } = deviceId;

        public string DeviceName { get; } = routeName;

        public AudioStreamFormatDto Format { get; } = new(
            source.WaveFormat.SampleRate,
            source.WaveFormat.Channels,
            32,
            "IEEE Float");

        public bool IsActive { get; private set; }

        public bool IsDisposed { get; private set; }

        public void Start() => IsActive = true;

        public void Stop() => IsActive = false;

        public void Dispose()
        {
            IsActive = false;
            IsDisposed = true;
        }

        public float[] Read(int count)
        {
            var buffer = new float[count];
            source.Read(buffer, 0, count);
            return buffer;
        }

        public void Disconnect(Exception exception)
        {
            IsActive = false;
            Stopped?.Invoke(this, new AudioRenderSessionStoppedEventArgs(exception));
        }
    }

    private sealed class ConstantSampleProvider(float value, WaveFormat format) : ISampleProvider
    {
        public WaveFormat WaveFormat { get; } = format;

        public int Read(float[] buffer, int offset, int count)
        {
            buffer.AsSpan(offset, count).Fill(value);
            return count;
        }
    }
}
