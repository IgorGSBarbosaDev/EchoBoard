using System.Globalization;

namespace EchoBoard.Application.Audio;

public sealed class ListMicrophoneDevicesUseCase
{
    private readonly IMicrophoneCaptureController controller;

    public ListMicrophoneDevicesUseCase(IMicrophoneCaptureController controller)
    {
        this.controller = controller;
    }

    public Task<IReadOnlyList<AudioInputDeviceDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return controller.ListInputDevicesAsync(cancellationToken);
    }
}

public sealed class LoadMicrophoneSettingsUseCase
{
    private readonly IAppSettingRepository settings;
    private readonly IMicrophoneCaptureController controller;

    public LoadMicrophoneSettingsUseCase(IAppSettingRepository settings, IMicrophoneCaptureController controller)
    {
        this.settings = settings;
        this.controller = controller;
    }

    public async Task<MicrophoneSettingsDto> ExecuteAsync(CancellationToken cancellationToken)
    {
        var loaded = new MicrophoneSettingsDto(
            await settings.GetValueAsync(MicrophoneSettingKeys.SelectedDeviceId, cancellationToken),
            await settings.GetValueAsync(MicrophoneSettingKeys.SelectedDeviceName, cancellationToken),
            ParseGain(await settings.GetValueAsync(MicrophoneSettingKeys.Gain, cancellationToken)),
            ParseMuted(await settings.GetValueAsync(MicrophoneSettingKeys.IsMuted, cancellationToken)));

        await controller.RestoreSelectionAsync(loaded, cancellationToken);
        return loaded;
    }

    private static double ParseGain(string? value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gain))
        {
            return MicrophoneCaptureSnapshot.ValidateGain(gain);
        }

        return MicrophoneSettingsDto.Default.Gain;
    }

    private static bool ParseMuted(string? value)
    {
        return bool.TryParse(value, out var muted) && muted;
    }
}

public sealed class SelectMicrophoneDeviceUseCase
{
    private readonly IAppSettingRepository settings;
    private readonly IMicrophoneCaptureController controller;

    public SelectMicrophoneDeviceUseCase(IAppSettingRepository settings, IMicrophoneCaptureController controller)
    {
        this.settings = settings;
        this.controller = controller;
    }

    public async Task<MicrophoneCaptureSnapshot> ExecuteAsync(string deviceId, CancellationToken cancellationToken)
    {
        var devices = await controller.ListInputDevicesAsync(cancellationToken);
        var device = devices.SingleOrDefault(item => string.Equals(item.Id, deviceId, StringComparison.Ordinal));
        if (device is null)
        {
            throw new InvalidOperationException($"Microphone device '{deviceId}' was not found.");
        }

        await controller.SelectDeviceAsync(device, cancellationToken);
        await settings.UpsertValueAsync(MicrophoneSettingKeys.SelectedDeviceId, device.Id, cancellationToken);
        await settings.UpsertValueAsync(MicrophoneSettingKeys.SelectedDeviceName, device.Name, cancellationToken);
        return controller.GetSnapshot();
    }
}

public sealed class SetMicrophoneGainUseCase
{
    private readonly IAppSettingRepository settings;
    private readonly IMicrophoneCaptureController controller;

    public SetMicrophoneGainUseCase(IAppSettingRepository settings, IMicrophoneCaptureController controller)
    {
        this.settings = settings;
        this.controller = controller;
    }

    public async Task<MicrophoneCaptureSnapshot> ExecuteAsync(double gain, CancellationToken cancellationToken)
    {
        MicrophoneCaptureSnapshot.ValidateGain(gain);
        await controller.SetGainAsync(gain, cancellationToken);
        await settings.UpsertValueAsync(MicrophoneSettingKeys.Gain, gain.ToString("0.########", CultureInfo.InvariantCulture), cancellationToken);
        return controller.GetSnapshot();
    }
}

public sealed class SetMicrophoneMuteUseCase
{
    private readonly IAppSettingRepository settings;
    private readonly IMicrophoneCaptureController controller;

    public SetMicrophoneMuteUseCase(IAppSettingRepository settings, IMicrophoneCaptureController controller)
    {
        this.settings = settings;
        this.controller = controller;
    }

    public async Task<MicrophoneCaptureSnapshot> ExecuteAsync(bool isMuted, CancellationToken cancellationToken)
    {
        await controller.SetMutedAsync(isMuted, cancellationToken);
        await settings.UpsertValueAsync(MicrophoneSettingKeys.IsMuted, isMuted.ToString(CultureInfo.InvariantCulture), cancellationToken);
        return controller.GetSnapshot();
    }
}

public sealed class StartMicrophoneCaptureUseCase
{
    private readonly IMicrophoneCaptureController controller;

    public StartMicrophoneCaptureUseCase(IMicrophoneCaptureController controller)
    {
        this.controller = controller;
    }

    public async Task<MicrophoneCaptureSnapshot> ExecuteAsync(CancellationToken cancellationToken)
    {
        await controller.StartAsync(cancellationToken);
        return controller.GetSnapshot();
    }
}

public sealed class StopMicrophoneCaptureUseCase
{
    private readonly IMicrophoneCaptureController controller;

    public StopMicrophoneCaptureUseCase(IMicrophoneCaptureController controller)
    {
        this.controller = controller;
    }

    public async Task<MicrophoneCaptureSnapshot> ExecuteAsync(CancellationToken cancellationToken)
    {
        await controller.StopAsync(cancellationToken);
        return controller.GetSnapshot();
    }
}

public sealed class GetMicrophoneCaptureSnapshotUseCase
{
    private readonly IMicrophoneCaptureController controller;

    public GetMicrophoneCaptureSnapshotUseCase(IMicrophoneCaptureController controller)
    {
        this.controller = controller;
    }

    public MicrophoneCaptureSnapshot Execute()
    {
        return controller.GetSnapshot();
    }
}
