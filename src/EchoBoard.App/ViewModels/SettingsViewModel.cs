using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Controls;
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
    private ToastPreviewModel? feedbackToast;

    public SettingsViewModel(
        ListHotkeyBindingsUseCase listHotkeys,
        AssignGlobalHotkeyUseCase assignGlobalHotkey,
        RemoveHotkeyBindingUseCase removeHotkeyBinding,
        SetHotkeyBindingEnabledUseCase setHotkeyBindingEnabled)
    {
        this.listHotkeys = listHotkeys;
        this.assignGlobalHotkey = assignGlobalHotkey;
        this.removeHotkeyBinding = removeHotkeyBinding;
        this.setHotkeyBindingEnabled = setHotkeyBindingEnabled;

        GlobalHotkeys =
        [
            CreateRow(GlobalHotkeyCommand.StopAllSounds, "Stop all sounds", "Stops every active sound when playback is available."),
            CreateRow(GlobalHotkeyCommand.PauseResumePlayback, "Pause/resume playback", "Toggles current playback when playback is available."),
            CreateRow(GlobalHotkeyCommand.ShowHideMainWindow, "Show/hide main window", "Toggles EchoBoard without focusing the app first.")
        ];
    }

    public string Title => "Settings";

    public string Subtitle => "Application preferences and daily-use behavior.";

    public ObservableCollection<GlobalHotkeySettingViewModel> GlobalHotkeys { get; }

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
