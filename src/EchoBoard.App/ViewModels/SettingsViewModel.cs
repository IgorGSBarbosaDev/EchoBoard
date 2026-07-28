using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Controls;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Domain.Enums;
using EchoBoard.Domain.Exceptions;
using Microsoft.UI.Xaml;

namespace EchoBoard.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private static readonly TimeSpan DefaultMeterInterval = TimeSpan.FromMilliseconds(1000.0 / 30.0);
    private readonly ListHotkeyBindingsUseCase listHotkeys;
    private readonly AssignGlobalHotkeyUseCase assignGlobalHotkey;
    private readonly RemoveHotkeyBindingUseCase removeHotkeyBinding;
    private readonly SetHotkeyBindingEnabledUseCase setHotkeyBindingEnabled;
    private readonly ListMicrophoneDevicesUseCase listMicrophoneDevices;
    private readonly LoadMicrophoneSettingsUseCase loadMicrophoneSettings;
    private readonly SelectMicrophoneDeviceUseCase selectMicrophoneDevice;
    private readonly SetMicrophoneGainUseCase setMicrophoneGain;
    private readonly SetMicrophoneMuteUseCase setMicrophoneMute;
    private readonly StartMicrophoneCaptureUseCase startMicrophoneCapture;
    private readonly StopMicrophoneCaptureUseCase stopMicrophoneCapture;
    private readonly GetMicrophoneCaptureSnapshotUseCase getMicrophoneCaptureSnapshot;
    private readonly ListAudioOutputDevicesUseCase? listAudioOutputDevices;
    private readonly LoadAudioRoutingSettingsUseCase? loadAudioRoutingSettings;
    private readonly SaveAudioRoutingSettingsUseCase? saveAudioRoutingSettings;
    private readonly GetAudioRoutingSnapshotUseCase? getAudioRoutingSnapshot;
    private readonly AudioRoutingSettingsCoordinator? audioSettings;
    private readonly MicrophoneLevelSmoother microphoneLevelSmoother = new();
    private readonly object routingSaveSync = new();
    private Task routingSaveTask = Task.CompletedTask;
    private AudioRoutingSettingsDto routingSettings = AudioRoutingSettingsDto.Default;
    private bool isApplyingRouting;
    private ToastPreviewModel? feedbackToast;
    private MicrophoneDeviceOptionViewModel? selectedMicrophoneDevice;
    private string microphoneStatusText = "Stopped";
    private string selectedMicrophoneName = "No microphone selected";
    private DeviceStatusKind microphoneStatusKind = DeviceStatusKind.Unavailable;
    private double microphoneLevel;
    private string microphoneLevelText = "Idle";
    private double microphoneGainPercent = 100;
    private bool isMicrophoneMuted;
    private AudioOutputDeviceOptionViewModel? selectedMonitorDevice;
    private AudioOutputDeviceOptionViewModel? selectedVirtualOutputDevice;
    private double effectsVolumePercent = 100;
    private double monitorVolumePercent = 80;
    private double virtualOutputVolumePercent = 100;
    private bool areEffectsMuted;
    private bool isMonitorEnabled = true;
    private bool isMonitorMuted;
    private bool isVirtualOutputMuted;
    private string routingStatusText = "Audio mixer is starting";
    private string virtualOutputWarningText = "Install and select a virtual cable to send microphone and effects to other applications.";
    private Visibility virtualOutputWarningVisibility = Visibility.Visible;

    public SettingsViewModel(
        ListHotkeyBindingsUseCase listHotkeys,
        AssignGlobalHotkeyUseCase assignGlobalHotkey,
        RemoveHotkeyBindingUseCase removeHotkeyBinding,
        SetHotkeyBindingEnabledUseCase setHotkeyBindingEnabled,
        ListMicrophoneDevicesUseCase listMicrophoneDevices,
        LoadMicrophoneSettingsUseCase loadMicrophoneSettings,
        SelectMicrophoneDeviceUseCase selectMicrophoneDevice,
        SetMicrophoneGainUseCase setMicrophoneGain,
        SetMicrophoneMuteUseCase setMicrophoneMute,
        StartMicrophoneCaptureUseCase startMicrophoneCapture,
        StopMicrophoneCaptureUseCase stopMicrophoneCapture,
        GetMicrophoneCaptureSnapshotUseCase getMicrophoneCaptureSnapshot,
        ListAudioOutputDevicesUseCase? listAudioOutputDevices = null,
        LoadAudioRoutingSettingsUseCase? loadAudioRoutingSettings = null,
        SaveAudioRoutingSettingsUseCase? saveAudioRoutingSettings = null,
        GetAudioRoutingSnapshotUseCase? getAudioRoutingSnapshot = null,
        AudioRoutingSettingsCoordinator? audioSettings = null)
    {
        this.listHotkeys = listHotkeys;
        this.assignGlobalHotkey = assignGlobalHotkey;
        this.removeHotkeyBinding = removeHotkeyBinding;
        this.setHotkeyBindingEnabled = setHotkeyBindingEnabled;
        this.listMicrophoneDevices = listMicrophoneDevices;
        this.loadMicrophoneSettings = loadMicrophoneSettings;
        this.selectMicrophoneDevice = selectMicrophoneDevice;
        this.setMicrophoneGain = setMicrophoneGain;
        this.setMicrophoneMute = setMicrophoneMute;
        this.startMicrophoneCapture = startMicrophoneCapture;
        this.stopMicrophoneCapture = stopMicrophoneCapture;
        this.getMicrophoneCaptureSnapshot = getMicrophoneCaptureSnapshot;
        this.listAudioOutputDevices = listAudioOutputDevices;
        this.loadAudioRoutingSettings = loadAudioRoutingSettings;
        this.saveAudioRoutingSettings = saveAudioRoutingSettings;
        this.getAudioRoutingSnapshot = getAudioRoutingSnapshot;
        this.audioSettings = audioSettings;

        GlobalHotkeys =
        [
            CreateRow(GlobalHotkeyCommand.StopAllSounds, "Stop all sounds", "Stops every active sound when playback is available."),
            CreateRow(GlobalHotkeyCommand.PauseResumePlayback, "Pause/resume playback", "Toggles current playback when playback is available."),
            CreateRow(GlobalHotkeyCommand.ShowHideMainWindow, "Show/hide main window", "Toggles EchoBoard without focusing the app first.")
        ];

        MicrophoneDevices = [];
        MonitorDevices = [];
        VirtualOutputDevices = [];
        RefreshMicrophoneDevicesCommand = new AsyncRelayCommand(RefreshMicrophoneDevicesAsync);
        StartMicrophoneCaptureCommand = new AsyncRelayCommand(StartMicrophoneCaptureAsync);
        StopMicrophoneCaptureCommand = new AsyncRelayCommand(StopMicrophoneCaptureAsync);
        ToggleMicrophoneMuteCommand = new AsyncRelayCommand(ToggleMicrophoneMuteAsync);
        if (audioSettings is not null)
        {
            audioSettings.PropertyChanged += OnAudioSettingsPropertyChanged;
        }
    }

    public string Title => "Settings";

    public string Subtitle => "Application preferences and daily-use behavior.";

    public ObservableCollection<GlobalHotkeySettingViewModel> GlobalHotkeys { get; }

    public ObservableCollection<MicrophoneDeviceOptionViewModel> MicrophoneDevices { get; }

    public ObservableCollection<AudioOutputDeviceOptionViewModel> MonitorDevices { get; }

    public ObservableCollection<AudioOutputDeviceOptionViewModel> VirtualOutputDevices { get; }

    public MicrophoneDeviceOptionViewModel? SelectedMicrophoneDevice
    {
        get => selectedMicrophoneDevice;
        set
        {
            if (SetProperty(ref selectedMicrophoneDevice, value) && value is not null)
            {
                if (audioSettings is not null)
                {
                    audioSettings.SetInputDevice(value.Id, value.Name);
                }
                else
                {
                    _ = SelectMicrophoneDeviceAsync(value.Id, CancellationToken.None);
                }
            }
        }
    }

    public string MicrophoneStatusText
    {
        get => microphoneStatusText;
        private set => SetProperty(ref microphoneStatusText, value);
    }

    public string SelectedMicrophoneName
    {
        get => selectedMicrophoneName;
        private set => SetProperty(ref selectedMicrophoneName, value);
    }

    public DeviceStatusKind MicrophoneStatusKind
    {
        get => microphoneStatusKind;
        private set => SetProperty(ref microphoneStatusKind, value);
    }

    public double MicrophoneLevel
    {
        get => microphoneLevel;
        private set => SetProperty(ref microphoneLevel, value);
    }

    public string MicrophoneLevelText
    {
        get => microphoneLevelText;
        private set => SetProperty(ref microphoneLevelText, value);
    }

    public double MicrophoneGainPercent
    {
        get => audioSettings?.MicrophonePercent ?? microphoneGainPercent;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.MicrophonePercent = value;
                return;
            }

            if (SetProperty(ref microphoneGainPercent, value))
            {
                _ = SetMicrophoneGainAsync(value / 100.0, CancellationToken.None);
            }
        }
    }

    public bool IsMicrophoneMuted
    {
        get => audioSettings?.IsMicrophoneMuted ?? isMicrophoneMuted;
        private set
        {
            if (audioSettings is not null)
            {
                audioSettings.IsMicrophoneMuted = value;
            }
            else
            {
                SetProperty(ref isMicrophoneMuted, value);
            }
        }
    }

    public string MicrophoneMuteButtonText => IsMicrophoneMuted ? "Unmute" : "Mute";

    public AudioOutputDeviceOptionViewModel? SelectedMonitorDevice
    {
        get => selectedMonitorDevice;
        set
        {
            if (SetProperty(ref selectedMonitorDevice, value) && !isApplyingRouting)
            {
                if (audioSettings is not null)
                {
                    audioSettings.SetMonitorDevice(value?.Id, value?.Name);
                }
                else
                {
                    _ = PersistRoutingAsync(CancellationToken.None);
                }
            }
        }
    }

    public AudioOutputDeviceOptionViewModel? SelectedVirtualOutputDevice
    {
        get => selectedVirtualOutputDevice;
        set
        {
            if (!isApplyingRouting &&
                value?.Id is not null &&
                SelectedMicrophoneDevice?.EndpointFamily is { } inputFamily &&
                string.Equals(inputFamily, value.EndpointFamily, StringComparison.Ordinal))
            {
                FeedbackToast = new ToastPreviewModel(
                    ToastNotificationKind.Warning,
                    "Virtual output not selected",
                    "Choose an output from a different device family to prevent audio feedback.");
                OnPropertyChanged();
                return;
            }

            if (SetProperty(ref selectedVirtualOutputDevice, value) && !isApplyingRouting)
            {
                if (audioSettings is not null)
                {
                    audioSettings.SetVirtualOutputDevice(value?.Id, value?.Name);
                }
                else
                {
                    _ = PersistRoutingAsync(CancellationToken.None);
                }
            }
        }
    }

    public double EffectsVolumePercent
    {
        get => audioSettings?.EffectsPercent ?? effectsVolumePercent;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.EffectsPercent = value;
                return;
            }

            if (SetProperty(ref effectsVolumePercent, Math.Clamp(value, 0, 100)) && !isApplyingRouting)
            {
                _ = PersistRoutingAsync(CancellationToken.None);
            }
        }
    }

    public double MonitorVolumePercent
    {
        get => audioSettings?.MonitorPercent ?? monitorVolumePercent;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.MonitorPercent = value;
                return;
            }

            if (SetProperty(ref monitorVolumePercent, Math.Clamp(value, 0, 100)) && !isApplyingRouting)
            {
                _ = PersistRoutingAsync(CancellationToken.None);
            }
        }
    }

    public double VirtualOutputVolumePercent
    {
        get => audioSettings?.VirtualOutputPercent ?? virtualOutputVolumePercent;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.VirtualOutputPercent = value;
                return;
            }

            if (SetProperty(ref virtualOutputVolumePercent, Math.Clamp(value, 0, 100)) && !isApplyingRouting)
            {
                _ = PersistRoutingAsync(CancellationToken.None);
            }
        }
    }

    public bool AreEffectsMuted
    {
        get => audioSettings?.AreEffectsMuted ?? areEffectsMuted;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.AreEffectsMuted = value;
                return;
            }

            if (SetProperty(ref areEffectsMuted, value) && !isApplyingRouting)
            {
                _ = PersistRoutingAsync(CancellationToken.None);
            }
        }
    }

    public bool IsMonitorEnabled
    {
        get => audioSettings?.IsMonitorEnabled ?? isMonitorEnabled;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.IsMonitorEnabled = value;
                return;
            }

            if (SetProperty(ref isMonitorEnabled, value) && !isApplyingRouting)
            {
                _ = PersistRoutingAsync(CancellationToken.None);
            }
        }
    }

    public bool IsMonitorMuted
    {
        get => audioSettings?.IsMonitorMuted ?? isMonitorMuted;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.IsMonitorMuted = value;
                return;
            }

            if (SetProperty(ref isMonitorMuted, value) && !isApplyingRouting)
            {
                _ = PersistRoutingAsync(CancellationToken.None);
            }
        }
    }

    public bool IsVirtualOutputMuted
    {
        get => audioSettings?.IsVirtualOutputMuted ?? isVirtualOutputMuted;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.IsVirtualOutputMuted = value;
                return;
            }

            if (SetProperty(ref isVirtualOutputMuted, value) && !isApplyingRouting)
            {
                _ = PersistRoutingAsync(CancellationToken.None);
            }
        }
    }

    public string RoutingStatusText
    {
        get => routingStatusText;
        private set => SetProperty(ref routingStatusText, value);
    }

    public string VirtualOutputWarningText
    {
        get => virtualOutputWarningText;
        private set => SetProperty(ref virtualOutputWarningText, value);
    }

    public Visibility VirtualOutputWarningVisibility
    {
        get => virtualOutputWarningVisibility;
        private set => SetProperty(ref virtualOutputWarningVisibility, value);
    }

    public IAsyncRelayCommand RefreshMicrophoneDevicesCommand { get; }

    public IAsyncRelayCommand StartMicrophoneCaptureCommand { get; }

    public IAsyncRelayCommand StopMicrophoneCaptureCommand { get; }

    public IAsyncRelayCommand ToggleMicrophoneMuteCommand { get; }

    public ToastPreviewModel? FeedbackToast
    {
        get => feedbackToast;
        private set
        {
            if (SetProperty(ref feedbackToast, value))
            {
                OnPropertyChanged(nameof(FeedbackToastVisibility));
            }
        }
    }

    public Visibility FeedbackToastVisibility => FeedbackToast is null ? Visibility.Collapsed : Visibility.Visible;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var bindings = await listHotkeys.ExecuteAsync(cancellationToken);
        foreach (var row in GlobalHotkeys)
        {
            var binding = bindings.SingleOrDefault(item => item.GlobalCommand == row.Command);
            row.Apply(binding);
        }

        await RefreshMicrophoneDevicesAsync(cancellationToken);
        await RefreshOutputDevicesAsync(cancellationToken);
        if (audioSettings is not null)
        {
            await audioSettings.LoadAsync(cancellationToken);
            ApplyRoutingSettings(audioSettings.Current);
        }
        else if (loadAudioRoutingSettings is not null)
        {
            ApplyRoutingSettings(await loadAudioRoutingSettings.ExecuteAsync(cancellationToken));
        }
        else
        {
            await loadMicrophoneSettings.ExecuteAsync(cancellationToken);
        }
        ApplyMicrophoneSnapshot(getMicrophoneCaptureSnapshot.Execute());
        ApplyRoutingSnapshot();
    }

    public async Task RefreshMicrophoneDevicesAsync(CancellationToken cancellationToken)
    {
        var devices = await listMicrophoneDevices.ExecuteAsync(cancellationToken);
        MicrophoneDevices.Clear();
        foreach (var device in devices)
        {
            MicrophoneDevices.Add(new MicrophoneDeviceOptionViewModel(
                device.Id,
                device.Name,
                device.IsDefault,
                device.IsAvailable,
                device.EndpointFamily));
        }

        FindOrAddUnavailableMicrophone(
            MicrophoneDevices,
            routingSettings.InputDeviceId,
            routingSettings.InputDeviceName);
    }

    public async Task SelectMicrophoneDeviceAsync(string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            if (saveAudioRoutingSettings is not null)
            {
                await PersistRoutingAsync(cancellationToken);
                ApplyMicrophoneSnapshot(getMicrophoneCaptureSnapshot.Execute());
            }
            else
            {
                var snapshot = await selectMicrophoneDevice.ExecuteAsync(deviceId, cancellationToken);
                ApplyMicrophoneSnapshot(snapshot);
            }
        }
        catch (Exception exception)
        {
            FeedbackToast = new ToastPreviewModel(ToastNotificationKind.Error, "Microphone not selected", exception.Message);
        }
    }

    public async Task StartMicrophoneCaptureAsync(CancellationToken cancellationToken)
    {
        var snapshot = await startMicrophoneCapture.ExecuteAsync(cancellationToken);
        ApplyMicrophoneSnapshot(snapshot);
        FeedbackToast = ToastForMicrophone(snapshot);
    }

    public async Task StopMicrophoneCaptureAsync(CancellationToken cancellationToken)
    {
        var snapshot = await stopMicrophoneCapture.ExecuteAsync(cancellationToken);
        ApplyMicrophoneSnapshot(snapshot);
        FeedbackToast = new ToastPreviewModel(ToastNotificationKind.Info, "Microphone stopped", "Capture stopped and input level cleared.");
    }

    public void RefreshMicrophoneSnapshot(TimeSpan? elapsed = null)
    {
        ApplyMicrophoneSnapshot(getMicrophoneCaptureSnapshot.Execute(), elapsed ?? DefaultMeterInterval);
        ApplyRoutingSnapshot();
    }

    public async Task DeactivateAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        microphoneLevelSmoother.Reset();
        ApplyMicrophoneSnapshot(getMicrophoneCaptureSnapshot.Execute());
    }

    private GlobalHotkeySettingViewModel CreateRow(GlobalHotkeyCommand command, string title, string description)
    {
        return new GlobalHotkeySettingViewModel(
            command,
            title,
            description,
            new AsyncRelayCommand<GlobalHotkeySettingViewModel>(SaveGlobalHotkeyAsync),
            new AsyncRelayCommand<GlobalHotkeySettingViewModel>(RemoveGlobalHotkeyAsync),
            new AsyncRelayCommand<GlobalHotkeySettingViewModel>(ToggleGlobalHotkeyEnabledAsync));
    }

    private async Task SetMicrophoneGainAsync(double gain, CancellationToken cancellationToken)
    {
        if (audioSettings is not null)
        {
            audioSettings.MicrophonePercent = gain * 100;
            return;
        }

        var snapshot = await setMicrophoneGain.ExecuteAsync(gain, cancellationToken);
        ApplyMicrophoneSnapshot(snapshot);
        if (saveAudioRoutingSettings is not null && !isApplyingRouting)
        {
            await PersistRoutingAsync(cancellationToken);
        }
    }

    private async Task ToggleMicrophoneMuteAsync(CancellationToken cancellationToken)
    {
        if (audioSettings is not null)
        {
            audioSettings.IsMicrophoneMuted = !audioSettings.IsMicrophoneMuted;
            OnPropertyChanged(nameof(IsMicrophoneMuted));
            OnPropertyChanged(nameof(MicrophoneMuteButtonText));
            return;
        }

        var snapshot = await setMicrophoneMute.ExecuteAsync(!IsMicrophoneMuted, cancellationToken);
        ApplyMicrophoneSnapshot(snapshot);
        if (saveAudioRoutingSettings is not null)
        {
            await PersistRoutingAsync(cancellationToken);
        }
        FeedbackToast = new ToastPreviewModel(
            snapshot.IsMuted ? ToastNotificationKind.Warning : ToastNotificationKind.Success,
            snapshot.IsMuted ? "Microphone muted" : "Microphone unmuted",
            snapshot.IsMuted ? "Microphone input is muted for capture." : "Microphone input is active when capture is running.");
    }

    private void ApplyMicrophoneSnapshot(MicrophoneCaptureSnapshot snapshot, TimeSpan? meterElapsed = null)
    {
        SelectedMicrophoneName = string.IsNullOrWhiteSpace(snapshot.SelectedDeviceName)
            ? "No microphone selected"
            : snapshot.SelectedDeviceName;
        MicrophoneStatusText = snapshot.State.ToString();
        MicrophoneStatusKind = ToDeviceStatus(snapshot.State);
        if (meterElapsed is { } elapsed)
        {
            var targetLevel = snapshot.State == MicrophoneCaptureState.Active && !snapshot.IsMuted
                ? snapshot.Level
                : 0.0;
            MicrophoneLevel = microphoneLevelSmoother.Update(targetLevel, elapsed);
        }
        MicrophoneLevelText = snapshot.IsMuted ? "Muted" : snapshot.State == MicrophoneCaptureState.Active ? $"{MicrophoneLevel:P0}" : "Idle";
        if (audioSettings is null)
        {
            isMicrophoneMuted = snapshot.IsMuted;
        }
        OnPropertyChanged(nameof(IsMicrophoneMuted));
        OnPropertyChanged(nameof(MicrophoneMuteButtonText));
        if (audioSettings is null)
        {
            microphoneGainPercent = Math.Clamp(snapshot.Gain * 100.0, 0, 100);
        }
        OnPropertyChanged(nameof(MicrophoneGainPercent));

        if (snapshot.SelectedDeviceId is not null)
        {
            var selected = MicrophoneDevices.SingleOrDefault(device => device.Id == snapshot.SelectedDeviceId);
            if (selectedMicrophoneDevice != selected)
            {
                selectedMicrophoneDevice = selected;
                OnPropertyChanged(nameof(SelectedMicrophoneDevice));
            }
        }
    }

    private static DeviceStatusKind ToDeviceStatus(MicrophoneCaptureState state)
    {
        return state switch
        {
            MicrophoneCaptureState.Active => DeviceStatusKind.Connected,
            MicrophoneCaptureState.Starting => DeviceStatusKind.Loading,
            MicrophoneCaptureState.Unavailable => DeviceStatusKind.Unavailable,
            MicrophoneCaptureState.Failed => DeviceStatusKind.Warning,
            _ => DeviceStatusKind.Disconnected
        };
    }

    private static ToastPreviewModel ToastForMicrophone(MicrophoneCaptureSnapshot snapshot)
    {
        return snapshot.State switch
        {
            MicrophoneCaptureState.Active => new ToastPreviewModel(ToastNotificationKind.Success, "Microphone active", snapshot.StatusMessage),
            MicrophoneCaptureState.Unavailable => new ToastPreviewModel(ToastNotificationKind.Warning, "Microphone unavailable", snapshot.StatusMessage),
            MicrophoneCaptureState.Failed => new ToastPreviewModel(ToastNotificationKind.Error, "Microphone failed", snapshot.ErrorMessage ?? snapshot.StatusMessage),
            _ => new ToastPreviewModel(ToastNotificationKind.Info, "Microphone", snapshot.StatusMessage)
        };
    }

    private async Task RefreshOutputDevicesAsync(CancellationToken cancellationToken)
    {
        if (listAudioOutputDevices is null)
        {
            return;
        }

        var devices = await listAudioOutputDevices.ExecuteAsync(cancellationToken);
        MonitorDevices.Clear();
        VirtualOutputDevices.Clear();
        VirtualOutputDevices.Add(AudioOutputDeviceOptionViewModel.None);
        foreach (var device in devices)
        {
            MonitorDevices.Add(AudioOutputDeviceOptionViewModel.From(device));
        }

        foreach (var device in devices
                     .OrderByDescending(device => device.IsVirtualOutputCandidate)
                     .ThenByDescending(device => device.IsDefault)
                     .ThenBy(device => device.Name))
        {
            VirtualOutputDevices.Add(AudioOutputDeviceOptionViewModel.From(device));
        }
    }

    private void ApplyRoutingSettings(AudioRoutingSettingsDto value)
    {
        isApplyingRouting = true;
        try
        {
            routingSettings = value;
            selectedMicrophoneDevice = FindOrAddUnavailableMicrophone(
                MicrophoneDevices,
                value.InputDeviceId,
                value.InputDeviceName);
            selectedMonitorDevice = FindOrAddUnavailableOutput(
                MonitorDevices,
                value.MonitorDeviceId,
                value.MonitorDeviceName,
                allowNone: false);
            selectedVirtualOutputDevice = FindOrAddUnavailableOutput(
                VirtualOutputDevices,
                value.VirtualOutputDeviceId,
                value.VirtualOutputDeviceName,
                allowNone: true);
            microphoneGainPercent = value.MicrophoneVolume * 100;
            effectsVolumePercent = value.EffectsVolume * 100;
            monitorVolumePercent = value.MonitorVolume * 100;
            virtualOutputVolumePercent = value.VirtualOutputVolume * 100;
            isMicrophoneMuted = value.IsMicrophoneMuted;
            areEffectsMuted = value.AreEffectsMuted;
            isMonitorEnabled = value.IsMonitorEnabled;
            isMonitorMuted = value.IsMonitorMuted;
            isVirtualOutputMuted = value.IsVirtualOutputMuted;
            foreach (var property in new[]
                     {
                         nameof(SelectedMicrophoneDevice), nameof(SelectedMonitorDevice), nameof(SelectedVirtualOutputDevice),
                         nameof(MicrophoneGainPercent), nameof(EffectsVolumePercent), nameof(MonitorVolumePercent),
                         nameof(VirtualOutputVolumePercent), nameof(IsMicrophoneMuted), nameof(AreEffectsMuted),
                         nameof(IsMonitorEnabled), nameof(IsMonitorMuted), nameof(IsVirtualOutputMuted), nameof(MicrophoneMuteButtonText)
                     })
            {
                OnPropertyChanged(property);
            }
        }
        finally
        {
            isApplyingRouting = false;
        }
    }

    private Task PersistRoutingAsync(CancellationToken cancellationToken)
    {
        if (saveAudioRoutingSettings is null || isApplyingRouting)
        {
            return Task.CompletedTask;
        }

        var value = routingSettings with
        {
            InputDeviceId = SelectedMicrophoneDevice?.Id ?? routingSettings.InputDeviceId,
            InputDeviceName = SelectedMicrophoneDevice?.Name ?? routingSettings.InputDeviceName,
            MonitorDeviceId = SelectedMonitorDevice?.Id,
            MonitorDeviceName = SelectedMonitorDevice?.Name,
            VirtualOutputDeviceId = SelectedVirtualOutputDevice?.Id,
            VirtualOutputDeviceName = SelectedVirtualOutputDevice?.Name,
            MicrophoneVolume = MicrophoneGainPercent / 100.0,
            EffectsVolume = EffectsVolumePercent / 100.0,
            MonitorVolume = MonitorVolumePercent / 100.0,
            VirtualOutputVolume = VirtualOutputVolumePercent / 100.0,
            IsMicrophoneMuted = IsMicrophoneMuted,
            AreEffectsMuted = AreEffectsMuted,
            IsMonitorEnabled = IsMonitorEnabled,
            IsMonitorMuted = IsMonitorMuted,
            IsVirtualOutputMuted = IsVirtualOutputMuted
        };
        routingSettings = value;
        lock (routingSaveSync)
        {
            routingSaveTask = SaveRoutingAfterAsync(routingSaveTask, value, cancellationToken);
            return routingSaveTask;
        }
    }

    private async Task SaveRoutingAfterAsync(
        Task previous,
        AudioRoutingSettingsDto value,
        CancellationToken cancellationToken)
    {
        try
        {
            await previous;
        }
        catch (Exception)
        {
            // A newer complete settings snapshot is still allowed to repair the route.
        }

        try
        {
            var snapshot = await saveAudioRoutingSettings!.ExecuteAsync(value, cancellationToken);
            RoutingStatusText = snapshot.StatusMessage;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            RoutingStatusText = "Audio route could not be updated.";
            FeedbackToast = new ToastPreviewModel(ToastNotificationKind.Error, "Routing not updated", exception.Message);
        }
    }

    private void ApplyRoutingSnapshot()
    {
        if (getAudioRoutingSnapshot is not null)
        {
            var snapshot = getAudioRoutingSnapshot.Execute();
            RoutingStatusText = snapshot.StatusMessage;
            VirtualOutputWarningVisibility = snapshot.VirtualOutputState == AudioRouteState.Active
                ? Visibility.Collapsed
                : Visibility.Visible;
            VirtualOutputWarningText = snapshot.VirtualOutputState switch
            {
                AudioRouteState.Unconfigured =>
                    "Install and select VB-CABLE, VoiceMeeter, or another virtual cable to transmit microphone and effects.",
                AudioRouteState.Unavailable =>
                    "The saved virtual output is disconnected. EchoBoard will reconnect it automatically.",
                AudioRouteState.Failed =>
                    snapshot.VirtualOutputErrorMessage ?? "The virtual output could not be started. Local playback remains available.",
                _ => snapshot.StatusMessage
            };
        }
    }

    private static AudioOutputDeviceOptionViewModel? FindOrAddUnavailableOutput(
        ObservableCollection<AudioOutputDeviceOptionViewModel> devices,
        string? id,
        string? name,
        bool allowNone)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return allowNone ? AudioOutputDeviceOptionViewModel.None : null;
        }

        var existing = devices.SingleOrDefault(device => string.Equals(device.Id, id, StringComparison.Ordinal));
        if (existing is not null)
        {
            return existing;
        }

        var unavailable = new AudioOutputDeviceOptionViewModel(
            id,
            string.IsNullOrWhiteSpace(name) ? "Previously selected device" : name,
            IsDefault: false,
            IsAvailable: false,
            IsVirtualOutputCandidate: false,
            EndpointFamily: null,
            IsPersistedUnavailable: true);
        devices.Insert(allowNone ? 1 : 0, unavailable);
        return unavailable;
    }

    private static MicrophoneDeviceOptionViewModel? FindOrAddUnavailableMicrophone(
        ObservableCollection<MicrophoneDeviceOptionViewModel> devices,
        string? id,
        string? name)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var existing = devices.SingleOrDefault(device => string.Equals(device.Id, id, StringComparison.Ordinal));
        if (existing is not null)
        {
            return existing;
        }

        var unavailable = new MicrophoneDeviceOptionViewModel(
            id,
            string.IsNullOrWhiteSpace(name) ? "Previously selected microphone" : name,
            IsDefault: false,
            IsAvailable: false,
            EndpointFamily: null,
            IsPersistedUnavailable: true);
        devices.Insert(0, unavailable);
        return unavailable;
    }

    private void OnAudioSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (audioSettings is not null)
        {
            ApplyRoutingSettings(audioSettings.Current);
        }
    }

    private async Task SaveGlobalHotkeyAsync(GlobalHotkeySettingViewModel? row, CancellationToken cancellationToken)
    {
        if (row is null)
        {
            return;
        }

        try
        {
            var result = await assignGlobalHotkey.ExecuteAsync(
                new AssignGlobalHotkeyRequest(row.Command, row.BuildModifiers(), row.PrimaryKey, row.IsEnabled, DateTimeOffset.UtcNow),
                cancellationToken);
            row.Apply(result);
            FeedbackToast = ToastForRegistration("Hotkey saved", result);
        }
        catch (Exception exception) when (exception is DuplicateHotkeyBindingException or DomainValidationException)
        {
            FeedbackToast = new ToastPreviewModel(ToastNotificationKind.Error, "Hotkey not saved", exception.Message);
        }
    }

    private async Task RemoveGlobalHotkeyAsync(GlobalHotkeySettingViewModel? row, CancellationToken cancellationToken)
    {
        if (row?.BindingId is null)
        {
            FeedbackToast = new ToastPreviewModel(ToastNotificationKind.Info, "No hotkey assigned", "This command has no hotkey to remove.");
            return;
        }

        await removeHotkeyBinding.ExecuteAsync(row.BindingId.Value, cancellationToken);
        row.Apply(null);
        FeedbackToast = new ToastPreviewModel(ToastNotificationKind.Success, "Hotkey removed", "The global command hotkey was removed.");
    }

    private async Task ToggleGlobalHotkeyEnabledAsync(GlobalHotkeySettingViewModel? row, CancellationToken cancellationToken)
    {
        if (row?.BindingId is null)
        {
            return;
        }

        var result = await setHotkeyBindingEnabled.ExecuteAsync(row.BindingId.Value, !row.IsEnabled, DateTimeOffset.UtcNow, cancellationToken);
        row.Apply(result);
        FeedbackToast = ToastForRegistration(result.IsEnabled ? "Hotkey enabled" : "Hotkey disabled", result);
    }

    private static ToastPreviewModel ToastForRegistration(string title, HotkeyBindingDto binding)
    {
        var kind = binding.RegistrationState switch
        {
            HotkeyRegistrationState.Active => ToastNotificationKind.Success,
            HotkeyRegistrationState.Disabled => ToastNotificationKind.Info,
            HotkeyRegistrationState.Conflicting => ToastNotificationKind.Warning,
            _ => ToastNotificationKind.Error
        };

        return new ToastPreviewModel(kind, title, binding.RegistrationMessage);
    }
}

public sealed record MicrophoneDeviceOptionViewModel(
    string Id,
    string Name,
    bool IsDefault,
    bool IsAvailable,
    string? EndpointFamily = null,
    bool IsPersistedUnavailable = false)
{
    public string DisplayName => IsPersistedUnavailable
        ? $"{Name} (Unavailable)"
        : IsDefault ? $"{Name} (Default)" : Name;
}

public sealed record AudioOutputDeviceOptionViewModel(
    string? Id,
    string Name,
    bool IsDefault,
    bool IsAvailable,
    bool IsVirtualOutputCandidate = false,
    string? EndpointFamily = null,
    bool IsPersistedUnavailable = false)
{
    public static AudioOutputDeviceOptionViewModel None { get; } = new(null, "No virtual output", false, true);

    public string DisplayName => IsPersistedUnavailable
        ? $"{Name} (Unavailable)"
        : IsVirtualOutputCandidate
            ? $"{Name} (Virtual cable)"
            : IsDefault ? $"{Name} (Default)" : Name;

    public static AudioOutputDeviceOptionViewModel From(AudioOutputDeviceDto device)
    {
        return new AudioOutputDeviceOptionViewModel(
            device.Id,
            device.Name,
            device.IsDefault,
            device.IsAvailable,
            device.IsVirtualOutputCandidate,
            device.EndpointFamily);
    }
}

public sealed class GlobalHotkeySettingViewModel : ObservableObject
{
    private Guid? bindingId;
    private string primaryKey = string.Empty;
    private string hotkeyText = "No hotkey";
    private bool ctrl = true;
    private bool alt;
    private bool shift;
    private bool win;
    private bool isEnabled = true;
    private HotkeyRegistrationState registrationState = HotkeyRegistrationState.Disabled;

    public GlobalHotkeySettingViewModel(
        GlobalHotkeyCommand command,
        string title,
        string description,
        IAsyncRelayCommand<GlobalHotkeySettingViewModel> saveCommand,
        IAsyncRelayCommand<GlobalHotkeySettingViewModel> removeCommand,
        IAsyncRelayCommand<GlobalHotkeySettingViewModel> toggleEnabledCommand)
    {
        Command = command;
        Title = title;
        Description = description;
        SaveCommand = saveCommand;
        RemoveCommand = removeCommand;
        ToggleEnabledCommand = toggleEnabledCommand;
    }

    public GlobalHotkeyCommand Command { get; }

    public string Title { get; }

    public string Description { get; }

    public Guid? BindingId
    {
        get => bindingId;
        private set => SetProperty(ref bindingId, value);
    }

    public string PrimaryKey
    {
        get => primaryKey;
        set => SetProperty(ref primaryKey, value);
    }

    public bool Ctrl
    {
        get => ctrl;
        set => SetProperty(ref ctrl, value);
    }

    public bool Alt
    {
        get => alt;
        set => SetProperty(ref alt, value);
    }

    public bool Shift
    {
        get => shift;
        set => SetProperty(ref shift, value);
    }

    public bool Win
    {
        get => win;
        set => SetProperty(ref win, value);
    }

    public bool IsEnabled
    {
        get => isEnabled;
        private set => SetProperty(ref isEnabled, value);
    }

    public string HotkeyText
    {
        get => hotkeyText;
        private set => SetProperty(ref hotkeyText, value);
    }

    public HotkeyRegistrationState RegistrationState
    {
        get => registrationState;
        private set => SetProperty(ref registrationState, value);
    }

    public string StatusText => BindingId is null ? "Unavailable" : RegistrationState.ToString();

    public string ToggleEnabledText => IsEnabled ? "Disable" : "Enable";

    public IAsyncRelayCommand<GlobalHotkeySettingViewModel> SaveCommand { get; }

    public IAsyncRelayCommand<GlobalHotkeySettingViewModel> RemoveCommand { get; }

    public IAsyncRelayCommand<GlobalHotkeySettingViewModel> ToggleEnabledCommand { get; }

    public HotkeyModifiers BuildModifiers()
    {
        var modifiers = HotkeyModifiers.None;
        if (Ctrl)
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (Alt)
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (Shift)
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (Win)
        {
            modifiers |= HotkeyModifiers.Windows;
        }

        return modifiers;
    }

    public void Apply(HotkeyBindingDto? binding)
    {
        BindingId = binding?.Id;
        HotkeyText = binding?.NormalizedKeyCombination ?? "No hotkey";
        PrimaryKey = binding?.PrimaryKey ?? string.Empty;
        Ctrl = binding?.Modifiers.HasFlag(HotkeyModifiers.Control) ?? true;
        Alt = binding?.Modifiers.HasFlag(HotkeyModifiers.Alt) ?? false;
        Shift = binding?.Modifiers.HasFlag(HotkeyModifiers.Shift) ?? false;
        Win = binding?.Modifiers.HasFlag(HotkeyModifiers.Windows) ?? false;
        IsEnabled = binding?.IsEnabled ?? true;
        RegistrationState = binding?.RegistrationState ?? HotkeyRegistrationState.Disabled;
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ToggleEnabledText));
    }
}
