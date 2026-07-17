using System.Globalization;

namespace EchoBoard.Application.Audio;

public static class MicrophoneSettingKeys
{
    public const string SelectedDeviceId = "audio.microphone.selectedDeviceId";
    public const string SelectedDeviceName = "audio.microphone.selectedDeviceName";
    public const string Gain = "audio.microphone.gain";
    public const string IsMuted = "audio.microphone.isMuted";
}

public enum MicrophoneCaptureState
{
    Stopped,
    Starting,
    Active,
    Unavailable,
    Failed
}

public sealed record AudioInputDeviceDto(string Id, string Name, bool IsDefault, bool IsAvailable);

public sealed record AudioStreamFormatDto(int SampleRate, int Channels, int BitsPerSample, string Encoding)
{
    public string DisplayText => $"{SampleRate.ToString(CultureInfo.InvariantCulture)} Hz, {Channels.ToString(CultureInfo.InvariantCulture)} ch, {BitsPerSample.ToString(CultureInfo.InvariantCulture)}-bit {Encoding}";
}

public sealed record MicrophoneSettingsDto(string? SelectedDeviceId, string? SelectedDeviceName, double Gain, bool IsMuted)
{
    public static MicrophoneSettingsDto Default => new(null, null, 1.0, IsMuted: false);
}

public sealed record MicrophoneCaptureSnapshot(
    MicrophoneCaptureState State,
    string? SelectedDeviceId,
    string? SelectedDeviceName,
    double Level,
    double Gain,
    bool IsMuted,
    string StatusMessage,
    string? ErrorMessage,
    AudioStreamFormatDto? Format)
{
    public MicrophoneSettingsDto Settings => new(SelectedDeviceId, SelectedDeviceName, Gain, IsMuted);

    public static MicrophoneCaptureSnapshot Stopped()
    {
        return new(
            MicrophoneCaptureState.Stopped,
            null,
            null,
            Level: 0,
            Gain: 1.0,
            IsMuted: false,
            "Stopped",
            null,
            null);
    }

    public static MicrophoneCaptureSnapshot Stopped(AudioInputDeviceDto? device, MicrophoneSettingsDto settings)
    {
        return new(
            MicrophoneCaptureState.Stopped,
            device?.Id ?? settings.SelectedDeviceId,
            device?.Name ?? settings.SelectedDeviceName,
            Level: 0,
            Gain: ValidateGain(settings.Gain),
            settings.IsMuted,
            device is null ? "Select a microphone before starting capture." : "Stopped",
            null,
            null);
    }

    public static MicrophoneCaptureSnapshot Unavailable(string message, MicrophoneSettingsDto settings)
    {
        return new(
            MicrophoneCaptureState.Unavailable,
            settings.SelectedDeviceId,
            settings.SelectedDeviceName,
            Level: 0,
            Gain: ValidateGain(settings.Gain),
            settings.IsMuted,
            message,
            null,
            null);
    }

    public static double ValidateGain(double gain)
    {
        if (gain is < 0.0 or > 1.0 || double.IsNaN(gain))
        {
            throw new ArgumentOutOfRangeException(nameof(gain), "Microphone gain must be between 0.0 and 1.0.");
        }

        return gain;
    }
}

public interface IAppSettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken);

    Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken);
}

public interface IMicrophonePcmSource
{
    AudioStreamFormatDto Format { get; }

    bool TryRead(Span<float> destination, out int samplesWritten);
}

public interface IMicrophoneCaptureController
{
    IMicrophonePcmSource? CurrentSource { get; }

    Task<IReadOnlyList<AudioInputDeviceDto>> ListInputDevicesAsync(CancellationToken cancellationToken);

    Task RestoreSelectionAsync(MicrophoneSettingsDto settings, CancellationToken cancellationToken);

    Task SelectDeviceAsync(AudioInputDeviceDto device, CancellationToken cancellationToken);

    Task SetGainAsync(double gain, CancellationToken cancellationToken);

    Task SetMutedAsync(bool isMuted, CancellationToken cancellationToken);

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    MicrophoneCaptureSnapshot GetSnapshot();
}

public interface ISoundPlaybackEngine
{
    Task PlayAsync(string filePath, double volume, CancellationToken cancellationToken);

    Task PlayAsync(string filePath, double volume, SoundPlaybackOptions options, CancellationToken cancellationToken)
        => PlayAsync(filePath, volume, cancellationToken);

    Task StopAllAsync(CancellationToken cancellationToken);

    Task StopSoundAsync(string filePath, CancellationToken cancellationToken)
        => StopAllAsync(cancellationToken);

    Task TogglePauseAsync(CancellationToken cancellationToken);

    Task SeekAsync(TimeSpan position, CancellationToken cancellationToken);

    Task SetVolumeAsync(double volume, CancellationToken cancellationToken);

    SoundPlaybackSnapshot GetSnapshot();
}

public sealed record SoundPlaybackOptions(bool IsLoopEnabled)
{
    public static SoundPlaybackOptions Default { get; } = new(IsLoopEnabled: false);
}

public sealed record SoundPlaybackSnapshot(
    string? FilePath,
    TimeSpan Position,
    TimeSpan Duration,
    bool IsPlaying,
    bool IsPaused)
{
    public static SoundPlaybackSnapshot Idle { get; } = new(null, TimeSpan.Zero, TimeSpan.Zero, false, false);
}
