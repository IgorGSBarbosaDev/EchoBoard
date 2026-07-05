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
    private ToastPreviewModel? feedbackToast;
    private MicrophoneDeviceOptionViewModel? selectedMicrophoneDevice;
    private string microphoneStatusText = "Stopped";
    private string selectedMicrophoneName = "No microphone selected";
    private DeviceStatusKind microphoneStatusKind = DeviceStatusKind.Unavailable;
    private double microphoneLevel;
    private string microphoneLevelText = "Idle";
    private double microphoneGainPercent = 100;
    private bool isMicrophoneMuted;

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
        GetMicrophoneCaptureSnapshotUseCase getMicrophoneCaptureSnapshot)
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

        GlobalHotkeys =
        [
            CreateRow(GlobalHotkeyCommand.StopAllSounds, "Stop all sounds", "Stops every active sound when playback is available."),
            CreateRow(GlobalHotkeyCommand.PauseResumePlayback, "Pause/resume playback", "Toggles current playback when playback is available."),
            CreateRow(GlobalHotkeyCommand.ShowHideMainWindow, "Show/hide main window", "Toggles EchoBoard without focusing the app first.")
        ];

        MicrophoneDevices = [];
        RefreshMicrophoneDevicesCommand = new AsyncRelayCommand(RefreshMicrophoneDevicesAsync);
        StartMicrophoneCaptureCommand = new AsyncRelayCommand(StartMicrophoneCaptureAsync);
        StopMicrophoneCaptureCommand = new AsyncRelayCommand(StopMicrophoneCaptureAsync);
        ToggleMicrophoneMuteCommand = new AsyncRelayCommand(ToggleMicrophoneMuteAsync);
    }

    public string Title => "Settings";

    public string Subtitle => "Application preferences and daily-use behavior.";

    public ObservableCollection<GlobalHotkeySettingViewModel> GlobalHotkeys { get; }

    public ObservableCollection<MicrophoneDeviceOptionViewModel> MicrophoneDevices { get; }

    public MicrophoneDeviceOptionViewModel? SelectedMicrophoneDevice
    {
        get => selectedMicrophoneDevice;
        set
        {
            if (SetProperty(ref selectedMicrophoneDevice, value) && value is not null)
            {
                _ = SelectMicrophoneDeviceAsync(value.Id, CancellationToken.None);
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
        get => microphoneGainPercent;
        set
        {
            if (SetProperty(ref microphoneGainPercent, value))
            {
                _ = SetMicrophoneGainAsync(value / 100.0, CancellationToken.None);
            }
        }
    }

    public bool IsMicrophoneMuted
    {
        get => isMicrophoneMuted;
        private set => SetProperty(ref isMicrophoneMuted, value);
    }

    public string MicrophoneMuteButtonText => IsMicrophoneMuted ? "Unmute" : "Mute";

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
        await loadMicrophoneSettings.ExecuteAsync(cancellationToken);
        ApplyMicrophoneSnapshot(getMicrophoneCaptureSnapshot.Execute());
    }

    public async Task RefreshMicrophoneDevicesAsync(CancellationToken cancellationToken)
    {
        var devices = await listMicrophoneDevices.ExecuteAsync(cancellationToken);
        MicrophoneDevices.Clear();
        foreach (var device in devices)
        {
            MicrophoneDevices.Add(new MicrophoneDeviceOptionViewModel(device.Id, device.Name, device.IsDefault, device.IsAvailable));
        }
    }

    public async Task SelectMicrophoneDeviceAsync(string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await selectMicrophoneDevice.ExecuteAsync(deviceId, cancellationToken);
            ApplyMicrophoneSnapshot(snapshot);
            FeedbackToast = new ToastPreviewModel(ToastNotificationKind.Success, "Microphone selected", snapshot.SelectedDeviceName ?? "Selected input device.");
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

    public void RefreshMicrophoneSnapshot()
    {
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
        var snapshot = await setMicrophoneGain.ExecuteAsync(gain, cancellationToken);
        ApplyMicrophoneSnapshot(snapshot);
    }

    private async Task ToggleMicrophoneMuteAsync(CancellationToken cancellationToken)
    {
        var snapshot = await setMicrophoneMute.ExecuteAsync(!IsMicrophoneMuted, cancellationToken);
        ApplyMicrophoneSnapshot(snapshot);
        FeedbackToast = new ToastPreviewModel(
            snapshot.IsMuted ? ToastNotificationKind.Warning : ToastNotificationKind.Success,
            snapshot.IsMuted ? "Microphone muted" : "Microphone unmuted",
            snapshot.IsMuted ? "Microphone input is muted for capture." : "Microphone input is active when capture is running.");
    }

    private void ApplyMicrophoneSnapshot(MicrophoneCaptureSnapshot snapshot)
    {
        SelectedMicrophoneName = string.IsNullOrWhiteSpace(snapshot.SelectedDeviceName)
            ? "No microphone selected"
            : snapshot.SelectedDeviceName;
        MicrophoneStatusText = snapshot.State.ToString();
        MicrophoneStatusKind = ToDeviceStatus(snapshot.State);
        MicrophoneLevel = Math.Clamp(snapshot.Level, 0, 1);
        MicrophoneLevelText = snapshot.IsMuted ? "Muted" : snapshot.State == MicrophoneCaptureState.Active ? $"{MicrophoneLevel:P0}" : "Idle";
        isMicrophoneMuted = snapshot.IsMuted;
        OnPropertyChanged(nameof(IsMicrophoneMuted));
        OnPropertyChanged(nameof(MicrophoneMuteButtonText));
        microphoneGainPercent = Math.Clamp(snapshot.Gain * 100.0, 0, 100);
        OnPropertyChanged(nameof(MicrophoneGainPercent));

        var selected = MicrophoneDevices.SingleOrDefault(device => device.Id == snapshot.SelectedDeviceId);
        if (selectedMicrophoneDevice != selected)
        {
            selectedMicrophoneDevice = selected;
            OnPropertyChanged(nameof(SelectedMicrophoneDevice));
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

public sealed record MicrophoneDeviceOptionViewModel(string Id, string Name, bool IsDefault, bool IsAvailable)
{
    public string DisplayName => IsDefault ? $"{Name} (Default)" : Name;
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
