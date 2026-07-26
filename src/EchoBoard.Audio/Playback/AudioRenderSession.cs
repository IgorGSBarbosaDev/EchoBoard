using EchoBoard.Application.Audio;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace EchoBoard.Audio.Playback;

internal sealed class AudioRenderSessionStoppedEventArgs(Exception? exception) : EventArgs
{
    public Exception? Exception { get; } = exception;
}

internal interface IAudioRenderSession : IDisposable
{
    event EventHandler<AudioRenderSessionStoppedEventArgs>? Stopped;

    string DeviceId { get; }

    string DeviceName { get; }

    AudioStreamFormatDto Format { get; }

    bool IsActive { get; }

    void Start();

    void Stop();
}

internal interface IAudioRenderSessionFactory
{
    IAudioRenderSession Create(string? deviceId, ISampleProvider source, string routeName);
}

internal sealed class WasapiAudioRenderSessionFactory : IAudioRenderSessionFactory
{
    public IAudioRenderSession Create(string? deviceId, ISampleProvider source, string routeName)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var enumerator = new MMDeviceEnumerator();
        using var device = string.IsNullOrWhiteSpace(deviceId)
            ? enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
            : enumerator.GetDevice(deviceId);
        if (device.State != DeviceState.Active)
        {
            throw new InvalidOperationException($"{device.FriendlyName} is not active.");
        }

        var endpointFormat = device.AudioClient.MixFormat;
        var adapted = AudioRenderFormatAdapter.Adapt(source, endpointFormat);
        var output = new WasapiOut(device, AudioClientShareMode.Shared, useEventSync: true, latency: 50);
        try
        {
            output.Init(adapted);
            return new WasapiAudioRenderSession(
                output,
                device.ID,
                device.FriendlyName,
                ToDto(endpointFormat),
                routeName);
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    private static AudioStreamFormatDto ToDto(WaveFormat format)
    {
        return new AudioStreamFormatDto(
            format.SampleRate,
            format.Channels,
            format.BitsPerSample,
            format.Encoding.ToString());
    }
}

internal sealed class WasapiAudioRenderSession : IAudioRenderSession
{
    private readonly WasapiOut output;
    private readonly string routeName;
    private bool stopping;
    private bool disposed;

    public WasapiAudioRenderSession(
        WasapiOut output,
        string deviceId,
        string deviceName,
        AudioStreamFormatDto format,
        string routeName)
    {
        this.output = output;
        this.routeName = routeName;
        DeviceId = deviceId;
        DeviceName = deviceName;
        Format = format;
        output.PlaybackStopped += OnPlaybackStopped;
    }

    public event EventHandler<AudioRenderSessionStoppedEventArgs>? Stopped;

    public string DeviceId { get; }

    public string DeviceName { get; }

    public AudioStreamFormatDto Format { get; }

    public bool IsActive => !disposed && output.PlaybackState == PlaybackState.Playing;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        output.Play();
        if (output.PlaybackState != PlaybackState.Playing)
        {
            throw new InvalidOperationException($"{routeName} could not be started.");
        }
    }

    public void Stop()
    {
        if (disposed || stopping)
        {
            return;
        }

        stopping = true;
        output.Stop();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        stopping = true;
        output.PlaybackStopped -= OnPlaybackStopped;
        try
        {
            output.Stop();
        }
        finally
        {
            output.Dispose();
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs args)
    {
        if (!stopping && !disposed)
        {
            Stopped?.Invoke(this, new AudioRenderSessionStoppedEventArgs(args.Exception));
        }
    }
}

internal static class AudioRenderFormatAdapter
{
    public static ISampleProvider Adapt(ISampleProvider source, WaveFormat endpointFormat)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(endpointFormat);
        if (endpointFormat.SampleRate <= 0 || endpointFormat.Channels <= 0)
        {
            throw new NotSupportedException("The output device reported an invalid mix format.");
        }

        ISampleProvider adapted = source;
        if (adapted.WaveFormat.SampleRate != endpointFormat.SampleRate)
        {
            adapted = new WdlResamplingSampleProvider(adapted, endpointFormat.SampleRate);
        }

        if (adapted.WaveFormat.Channels == endpointFormat.Channels)
        {
            return adapted;
        }

        if (adapted.WaveFormat.Channels == 2 && endpointFormat.Channels == 1)
        {
            return new StereoToMonoSampleProvider(adapted)
            {
                LeftVolume = 0.5f,
                RightVolume = 0.5f
            };
        }

        if (adapted.WaveFormat.Channels == 1 && endpointFormat.Channels == 2)
        {
            return new MonoToStereoSampleProvider(adapted);
        }

        if (adapted.WaveFormat.Channels is 1 or 2 && endpointFormat.Channels > 2)
        {
            var multiplexed = new MultiplexingSampleProvider([adapted], endpointFormat.Channels);
            multiplexed.ConnectInputToOutput(0, 0);
            if (adapted.WaveFormat.Channels == 2)
            {
                multiplexed.ConnectInputToOutput(1, 1);
            }
            else
            {
                multiplexed.ConnectInputToOutput(0, 1);
            }

            return multiplexed;
        }

        throw new NotSupportedException(
            $"Cannot adapt {adapted.WaveFormat.Channels} input channels to {endpointFormat.Channels} output channels.");
    }
}

internal static class AudioEndpointClassifier
{
    public static string? GetFamily(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (name.Contains("NVIDIA Broadcast", StringComparison.OrdinalIgnoreCase))
        {
            return "nvidia-broadcast";
        }

        if (name.Contains("VoiceMeeter", StringComparison.OrdinalIgnoreCase))
        {
            if (name.Contains("AUX", StringComparison.OrdinalIgnoreCase))
            {
                return "voicemeeter-aux";
            }

            if (name.Contains("VAIO3", StringComparison.OrdinalIgnoreCase))
            {
                return "voicemeeter-vaio3";
            }

            return "voicemeeter-vaio";
        }

        if (name.Contains("Virtual Audio Cable", StringComparison.OrdinalIgnoreCase) &&
            !name.Contains("VB-Audio", StringComparison.OrdinalIgnoreCase))
        {
            return "virtual-audio-cable";
        }

        if (name.Contains("CABLE", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("VB-Audio", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var suffix in new[] { " A", " B", " C", " D" })
            {
                if (name.Contains($"CABLE{suffix}", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains($"CABLE-{suffix.Trim()}", StringComparison.OrdinalIgnoreCase))
                {
                    return $"vb-cable-{suffix.Trim().ToLowerInvariant()}";
                }
            }

            return "vb-cable";
        }

        if (name.Contains("Virtual Audio", StringComparison.OrdinalIgnoreCase))
        {
            return "virtual-audio-cable";
        }

        if (name.Contains("Synchronous Audio Router", StringComparison.OrdinalIgnoreCase))
        {
            return "synchronous-audio-router";
        }

        if (name.Contains("JACK Router", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("JACK Audio", StringComparison.OrdinalIgnoreCase))
        {
            return "jack-router";
        }

        if (name.Contains("Virtual Cable", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("VAC ", StringComparison.OrdinalIgnoreCase))
        {
            return "virtual-audio-cable";
        }

        if (name.Contains("Wave Link", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Elgato Virtual", StringComparison.OrdinalIgnoreCase))
        {
            return "elgato-wave-link";
        }

        return null;
    }

    public static bool IsVirtualOutputCandidate(string? name)
    {
        var family = GetFamily(name);
        return family is not null && !string.Equals(family, "nvidia-broadcast", StringComparison.Ordinal);
    }

    public static bool WouldCreateFeedback(string? inputName, string? outputName)
    {
        var inputFamily = GetFamily(inputName);
        return inputFamily is not null &&
               string.Equals(inputFamily, GetFamily(outputName), StringComparison.Ordinal);
    }
}
