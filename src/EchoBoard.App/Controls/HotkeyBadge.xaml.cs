using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using EchoBoard.Domain.Enums;

namespace EchoBoard.App.Controls;

public sealed partial class HotkeyBadge : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(HotkeyBadge),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty IsUnavailableProperty = DependencyProperty.Register(
        nameof(IsUnavailable),
        typeof(bool),
        typeof(HotkeyBadge),
        new PropertyMetadata(false, OnDisplayPropertyChanged));

    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State),
        typeof(HotkeyRegistrationState),
        typeof(HotkeyBadge),
        new PropertyMetadata(HotkeyRegistrationState.Active, OnDisplayPropertyChanged));

    public HotkeyBadge()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateState();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsUnavailable
    {
        get => (bool)GetValue(IsUnavailableProperty);
        set => SetValue(IsUnavailableProperty, value);
    }

    public HotkeyRegistrationState State
    {
        get => (HotkeyRegistrationState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public string DisplayText => IsUnavailable || string.IsNullOrWhiteSpace(Text) ? "No hotkey" : Text;

    public string AutomationName => $"Hotkey {DisplayText}";

    private static void OnDisplayPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var control = (HotkeyBadge)dependencyObject;
        control.Bindings.Update();
        control.UpdateState();
    }

    private void UpdateState()
    {
        var stateName = IsUnavailable || string.IsNullOrWhiteSpace(Text) || string.Equals(Text, "No hotkey", StringComparison.OrdinalIgnoreCase)
            ? "Unavailable"
            : State switch
            {
                HotkeyRegistrationState.Disabled => "Disabled",
                HotkeyRegistrationState.Conflicting => "Conflicting",
                HotkeyRegistrationState.Unavailable => "Unavailable",
                HotkeyRegistrationState.Invalid => "Conflicting",
                _ => "Available"
            };
        VisualStateManager.GoToState(this, stateName, true);
    }
}
