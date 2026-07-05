using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
        VisualStateManager.GoToState(this, IsUnavailable || string.IsNullOrWhiteSpace(Text) ? "Unavailable" : "Available", true);
    }
}
