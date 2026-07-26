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
    Task<SoundPlaybackStartResult> PlayAsync(string filePath, double volume, CancellationToken cancellationToken);

    Task<SoundPlaybackStartResult> PlayAsync(string filePath, double volume, SoundPlaybackOptions options, CancellationToken cancellationToken)
        => PlayAsync(filePath, volume, cancellationToken);

    Task StopAllAsync(CancellationToken cancellationToken);

    Task StopSoundAsync(string filePath, CancellationToken cancellationToken)
        => StopAllAsync(cancellationToken);

    Task TogglePauseAsync(CancellationToken cancellationToken);

    Task SeekAsync(TimeSpan position, CancellationToken cancellationToken);

    Task SetVolumeAsync(double volume, CancellationToken cancellationToken);

    SoundPlaybackSnapshot GetSnapshot();
}

public enum SoundPlaybackState
{
    Stopped,
    Playing,
    Paused
}

public sealed record SoundPlaybackStartResult(
    Guid SessionId,
    string FilePath,
    TimeSpan Duration,
    bool IsMonitorActive,
    bool IsVirtualOutputActive,
    SoundPlaybackSnapshot Snapshot);

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

    public SoundPlaybackState State => IsPlaying
        ? SoundPlaybackState.Playing
        : IsPaused ? SoundPlaybackState.Paused : SoundPlaybackState.Stopped;
}

public static class AudioRoutingSettingKeys
{
    public const string InputDeviceId = "audio.routing.v1.inputDeviceId";
    public const string InputDeviceName = "audio.routing.v1.inputDeviceName";
    public const string MonitorDeviceId = "audio.routing.v1.monitorDeviceId";
    public const string MonitorDeviceName = "audio.routing.v1.monitorDeviceName";
    public const string VirtualOutputDeviceId = "audio.routing.v1.virtualOutputDeviceId";
    public const string VirtualOutputDeviceName = "audio.routing.v1.virtualOutputDeviceName";
    public const string MicrophoneVolume = "audio.routing.v1.microphoneVolume";
    public const string EffectsVolume = "audio.routing.v1.effectsVolume";
    public const string MonitorVolume = "audio.routing.v1.monitorVolume";
    public const string VirtualOutputVolume = "audio.routing.v1.virtualOutputVolume";
    public const string IsMicrophoneMuted = "audio.routing.v1.isMicrophoneMuted";
    public const string AreEffectsMuted = "audio.routing.v1.areEffectsMuted";
    public const string IsMonitorEnabled = "audio.routing.v1.isMonitorEnabled";
    public const string IsMonitorMuted = "audio.routing.v1.isMonitorMuted";
    public const string IsVirtualOutputMuted = "audio.routing.v1.isVirtualOutputMuted";
}

public enum AudioRouteState
{
    Unconfigured,
    Starting,
    Active,
    Unavailable,
    Failed,
    Stopped
}

public sealed record AudioOutputDeviceDto(string Id, string Name, bool IsDefault, bool IsAvailable);

public sealed record AudioRoutingSettingsDto(
    string? InputDeviceId,
    string? InputDeviceName,
    string? MonitorDeviceId,
    string? MonitorDeviceName,
    string? VirtualOutputDeviceId,
    string? VirtualOutputDeviceName,
    double MicrophoneVolume,
    double EffectsVolume,
    double MonitorVolume,
    double VirtualOutputVolume,
    bool IsMicrophoneMuted,
    bool AreEffectsMuted,
    bool IsMonitorEnabled,
    bool IsMonitorMuted,
    bool IsVirtualOutputMuted)
{
    public static AudioRoutingSettingsDto Default { get; } = new(
        null, null, null, null, null, null,
        1.0, 1.0, 0.8, 1.0,
        false, false, true, false, false);
}

public sealed record AudioRoutingSnapshot(
    AudioRouteState EngineState,
    AudioRouteState MicrophoneState,
    AudioRouteState MonitorState,
    AudioRouteState VirtualOutputState,
    string? InputDeviceName,
    string? MonitorDeviceName,
    string? VirtualOutputDeviceName,
    string StatusMessage,
    string? ErrorMessage,
    AudioStreamFormatDto Format)
{
    public static AudioRoutingSnapshot Stopped { get; } = new(
        AudioRouteState.Stopped,
        AudioRouteState.Stopped,
        AudioRouteState.Stopped,
        AudioRouteState.Unconfigured,
        null,
        null,
        null,
        "Audio engine stopped.",
        null,
        new AudioStreamFormatDto(48000, 2, 32, "IEEE Float"));
}

public interface IAudioOutputDeviceEnumerator
{
    Task<IReadOnlyList<AudioOutputDeviceDto>> ListOutputDevicesAsync(CancellationToken cancellationToken);
}

public interface IAudioRoutingEngine
{
    Task InitializeAsync(AudioRoutingSettingsDto settings, CancellationToken cancellationToken);

    Task ApplySettingsAsync(AudioRoutingSettingsDto settings, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    AudioRoutingSnapshot GetRoutingSnapshot();
}
