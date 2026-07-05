using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EchoBoard.App.Controls;

public sealed partial class DeviceStatusBadge : UserControl
{
    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status),
        typeof(DeviceStatusKind),
        typeof(DeviceStatusBadge),
        new PropertyMetadata(DeviceStatusKind.Unknown, OnDisplayPropertyChanged));

    public static readonly DependencyProperty DeviceNameProperty = DependencyProperty.Register(
        nameof(DeviceName),
        typeof(string),
        typeof(DeviceStatusBadge),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(DeviceStatusBadge),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(Symbol),
        typeof(DeviceStatusBadge),
        new PropertyMetadata(Symbol.Audio, OnDisplayPropertyChanged));

    public DeviceStatusBadge()
    {
        InitializeComponent();
    }

    public DeviceStatusKind Status
    {
        get => (DeviceStatusKind)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public string DeviceName
    {
        get => (string)GetValue(DeviceNameProperty);
        set => SetValue(DeviceNameProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public Symbol Icon
    {
        get => (Symbol)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string DisplayLabel => string.IsNullOrWhiteSpace(DeviceName) ? Label : $"{Label}: {DeviceName}";

    public string StatusText => Status switch
    {
        DeviceStatusKind.Connected => "Connected",
        DeviceStatusKind.Disconnected => "Disconnected",
        DeviceStatusKind.Unavailable => "Unavailable",
        DeviceStatusKind.Warning => "Warning",
        DeviceStatusKind.Loading => "Loading",
        _ => "Unknown"
    };

    public string AccessibleLabel => $"{Label} {StatusText} {DeviceName}".Trim();

    public Brush StatusBrush => (Brush)Microsoft.UI.Xaml.Application.Current.Resources[Status switch
    {
        DeviceStatusKind.Connected => "EchoBoardSuccessBrush",
        DeviceStatusKind.Warning => "EchoBoardWarningBrush",
        DeviceStatusKind.Disconnected or DeviceStatusKind.Unavailable => "EchoBoardErrorBrush",
        _ => "EchoBoardTextSecondaryBrush"
    }];

    private static void OnDisplayPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((DeviceStatusBadge)dependencyObject).Bindings.Update();
    }
}
