using System.Globalization;

namespace EchoBoard.Application.Audio;

public sealed class ListAudioOutputDevicesUseCase
{
    private readonly IAudioOutputDeviceEnumerator devices;

    public ListAudioOutputDevicesUseCase(IAudioOutputDeviceEnumerator devices)
    {
        this.devices = devices;
    }

    public Task<IReadOnlyList<AudioOutputDeviceDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return devices.ListOutputDevicesAsync(cancellationToken);
    }
}

public sealed class LoadAudioRoutingSettingsUseCase
{
    private readonly IAppSettingRepository settings;
    private readonly IMicrophoneCaptureController microphones;
    private readonly IAudioOutputDeviceEnumerator outputs;

    public LoadAudioRoutingSettingsUseCase(
        IAppSettingRepository settings,
        IMicrophoneCaptureController microphones,
        IAudioOutputDeviceEnumerator outputs)
    {
        this.settings = settings;
        this.microphones = microphones;
        this.outputs = outputs;
    }

    public async Task<AudioRoutingSettingsDto> ExecuteAsync(CancellationToken cancellationToken)
    {
        var inputDevices = await microphones.ListInputDevicesAsync(cancellationToken);
        var outputDevices = await outputs.ListOutputDevicesAsync(cancellationToken);
        var savedInputId = await settings.GetValueAsync(AudioRoutingSettingKeys.InputDeviceId, cancellationToken);
        var savedInputName = await settings.GetValueAsync(AudioRoutingSettingKeys.InputDeviceName, cancellationToken);
        var legacyInputId = await settings.GetValueAsync(MicrophoneSettingKeys.SelectedDeviceId, cancellationToken);

        var input = Find(inputDevices, savedInputId)
                    ?? (string.IsNullOrWhiteSpace(savedInputId)
                        ? inputDevices.FirstOrDefault(device =>
                              device.Name.Contains("NVIDIA Broadcast", StringComparison.OrdinalIgnoreCase))
                          ?? Find(inputDevices, legacyInputId)
                          ?? inputDevices.FirstOrDefault(device => device.IsDefault)
                          ?? (inputDevices.Count > 0 ? inputDevices[0] : null)
                        : null);
        var monitorId = await settings.GetValueAsync(AudioRoutingSettingKeys.MonitorDeviceId, cancellationToken);
        var monitor = Find(outputDevices, monitorId)
                      ?? (string.IsNullOrWhiteSpace(monitorId)
                          ? outputDevices.FirstOrDefault(device => device.IsDefault)
                            ?? outputDevices.FirstOrDefault(device => device.IsAvailable)
                          : null);

        return new AudioRoutingSettingsDto(
            input?.Id ?? savedInputId,
            input?.Name ?? savedInputName,
            monitor?.Id ?? monitorId,
            monitor?.Name ?? await settings.GetValueAsync(AudioRoutingSettingKeys.MonitorDeviceName, cancellationToken),
            await settings.GetValueAsync(AudioRoutingSettingKeys.VirtualOutputDeviceId, cancellationToken),
            await settings.GetValueAsync(AudioRoutingSettingKeys.VirtualOutputDeviceName, cancellationToken),
            ParseVolume(await settings.GetValueAsync(AudioRoutingSettingKeys.MicrophoneVolume, cancellationToken), 1.0),
            ParseVolume(await settings.GetValueAsync(AudioRoutingSettingKeys.EffectsVolume, cancellationToken), 1.0),
            ParseVolume(await settings.GetValueAsync(AudioRoutingSettingKeys.MonitorVolume, cancellationToken), 0.8),
            ParseVolume(await settings.GetValueAsync(AudioRoutingSettingKeys.VirtualOutputVolume, cancellationToken), 1.0),
            ParseBoolean(await settings.GetValueAsync(AudioRoutingSettingKeys.IsMicrophoneMuted, cancellationToken)),
            ParseBoolean(await settings.GetValueAsync(AudioRoutingSettingKeys.AreEffectsMuted, cancellationToken)),
            ParseBoolean(await settings.GetValueAsync(AudioRoutingSettingKeys.IsMonitorEnabled, cancellationToken), defaultValue: true),
            ParseBoolean(await settings.GetValueAsync(AudioRoutingSettingKeys.IsMonitorMuted, cancellationToken)),
            ParseBoolean(await settings.GetValueAsync(AudioRoutingSettingKeys.IsVirtualOutputMuted, cancellationToken)));
    }

    private static T? Find<T>(IEnumerable<T> devices, string? id) where T : class
    {
        return devices.FirstOrDefault(device =>
        {
            var value = device switch
            {
                AudioInputDeviceDto input => input.Id,
                AudioOutputDeviceDto output => output.Id,
                _ => null
            };
            return string.Equals(value, id, StringComparison.Ordinal);
        });
    }

    private static double ParseVolume(string? value, double defaultValue)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Clamp(parsed, 0.0, 1.0)
            : defaultValue;
    }

    private static bool ParseBoolean(string? value, bool defaultValue = false)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}

public sealed class InitializeAudioRoutingUseCase
{
    private readonly LoadAudioRoutingSettingsUseCase loadSettings;
    private readonly SaveAudioRoutingSettingsUseCase saveSettings;

    public InitializeAudioRoutingUseCase(
        LoadAudioRoutingSettingsUseCase loadSettings,
        SaveAudioRoutingSettingsUseCase saveSettings)
    {
        this.loadSettings = loadSettings;
        this.saveSettings = saveSettings;
    }

    public async Task<AudioRoutingSettingsDto> ExecuteAsync(CancellationToken cancellationToken)
    {
        var loaded = await loadSettings.ExecuteAsync(cancellationToken);
        await saveSettings.ExecuteAsync(loaded, cancellationToken);
        return loaded;
    }
}

public sealed class SaveAudioRoutingSettingsUseCase
{
    private readonly IAppSettingRepository settings;
    private readonly IAudioRoutingEngine engine;

    public SaveAudioRoutingSettingsUseCase(
        IAppSettingRepository settings,
        IAudioRoutingEngine engine)
    {
        this.settings = settings;
        this.engine = engine;
    }

    public async Task<AudioRoutingSnapshot> ExecuteAsync(
        AudioRoutingSettingsDto value,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        await engine.ApplySettingsAsync(value, cancellationToken);
        await SaveDeviceAsync(AudioRoutingSettingKeys.InputDeviceId, value.InputDeviceId, cancellationToken);
        await SaveDeviceAsync(AudioRoutingSettingKeys.InputDeviceName, value.InputDeviceName, cancellationToken);
        await SaveDeviceAsync(AudioRoutingSettingKeys.MonitorDeviceId, value.MonitorDeviceId, cancellationToken);
        await SaveDeviceAsync(AudioRoutingSettingKeys.MonitorDeviceName, value.MonitorDeviceName, cancellationToken);
        await SaveDeviceAsync(AudioRoutingSettingKeys.VirtualOutputDeviceId, value.VirtualOutputDeviceId, cancellationToken);
        await SaveDeviceAsync(AudioRoutingSettingKeys.VirtualOutputDeviceName, value.VirtualOutputDeviceName, cancellationToken);
        await SaveAsync(AudioRoutingSettingKeys.MicrophoneVolume, value.MicrophoneVolume, cancellationToken);
        await SaveAsync(AudioRoutingSettingKeys.EffectsVolume, value.EffectsVolume, cancellationToken);
        await SaveAsync(AudioRoutingSettingKeys.MonitorVolume, value.MonitorVolume, cancellationToken);
        await SaveAsync(AudioRoutingSettingKeys.VirtualOutputVolume, value.VirtualOutputVolume, cancellationToken);
        await settings.UpsertValueAsync(AudioRoutingSettingKeys.IsMicrophoneMuted, value.IsMicrophoneMuted.ToString(CultureInfo.InvariantCulture), cancellationToken);
        await settings.UpsertValueAsync(AudioRoutingSettingKeys.AreEffectsMuted, value.AreEffectsMuted.ToString(CultureInfo.InvariantCulture), cancellationToken);
        await settings.UpsertValueAsync(AudioRoutingSettingKeys.IsMonitorEnabled, value.IsMonitorEnabled.ToString(CultureInfo.InvariantCulture), cancellationToken);
        await settings.UpsertValueAsync(AudioRoutingSettingKeys.IsMonitorMuted, value.IsMonitorMuted.ToString(CultureInfo.InvariantCulture), cancellationToken);
        await settings.UpsertValueAsync(AudioRoutingSettingKeys.IsVirtualOutputMuted, value.IsVirtualOutputMuted.ToString(CultureInfo.InvariantCulture), cancellationToken);
        return engine.GetRoutingSnapshot();
    }

    private Task SaveDeviceAsync(string key, string? value, CancellationToken cancellationToken)
    {
        return settings.UpsertValueAsync(key, value?.Trim() ?? string.Empty, cancellationToken);
    }

    private Task SaveAsync(string key, double value, CancellationToken cancellationToken)
    {
        return settings.UpsertValueAsync(
            key,
            Math.Clamp(value, 0.0, 1.0).ToString("0.########", CultureInfo.InvariantCulture),
            cancellationToken);
    }
}

public sealed class GetAudioRoutingSnapshotUseCase
{
    private readonly IAudioRoutingEngine engine;

    public GetAudioRoutingSnapshotUseCase(IAudioRoutingEngine engine)
    {
        this.engine = engine;
    }

    public AudioRoutingSnapshot Execute() => engine.GetRoutingSnapshot();
}
