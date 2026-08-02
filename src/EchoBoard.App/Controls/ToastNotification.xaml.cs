using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EchoBoard.App.Controls;

public sealed partial class ToastNotification : UserControl
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind),
        typeof(ToastNotificationKind),
        typeof(ToastNotification),
        new PropertyMetadata(ToastNotificationKind.Info, OnDisplayPropertyChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(ToastNotification),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(ToastNotification),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty DismissCommandProperty = DependencyProperty.Register(
        nameof(DismissCommand),
        typeof(ICommand),
        typeof(ToastNotification),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
        nameof(IsCompact),
        typeof(bool),
        typeof(ToastNotification),
        new PropertyMetadata(false, OnDisplayPropertyChanged));

    public ToastNotification()
    {
        InitializeComponent();
    }

    public ToastNotificationKind Kind
    {
        get => (ToastNotificationKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public Symbol Icon => Kind switch
    {
        ToastNotificationKind.Success => Symbol.Accept,
        ToastNotificationKind.Warning => Symbol.Important,
        ToastNotificationKind.Error => Symbol.Cancel,
        _ => Symbol.Message
    };

    public Brush KindBrush => (Brush)Microsoft.UI.Xaml.Application.Current.Resources[Kind switch
    {
        ToastNotificationKind.Success => "EchoBoardSuccessBrush",
        ToastNotificationKind.Warning => "EchoBoardWarningBrush",
        ToastNotificationKind.Error => "EchoBoardErrorBrush",
        _ => "EchoBoardActionBrush"
    }];

    public Brush NotificationBackground => IsCompact
        ? (Brush)Microsoft.UI.Xaml.Application.Current.Resources["EchoBoardBackgroundSurfaceBrush"]
        : (Brush)Microsoft.UI.Xaml.Application.Current.Resources["EchoBoardCardBrush"];

    public Brush NotificationBorderBrush => IsCompact
        ? (Brush)Microsoft.UI.Xaml.Application.Current.Resources["EchoBoardBorderSoftBrush"]
        : KindBrush;

    public Thickness ContentPadding => IsCompact
        ? new Thickness(12, 6, 12, 6)
        : (Thickness)Microsoft.UI.Xaml.Application.Current.Resources["EchoBoardPanelPadding"];

    public double MinimumHeight => IsCompact ? 52 : 0;

    public Visibility StandardLayoutVisibility => IsCompact ? Visibility.Collapsed : Visibility.Visible;

    public Visibility CompactLayoutVisibility => IsCompact ? Visibility.Visible : Visibility.Collapsed;

    public Visibility DescriptionVisibility => string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;

    public string AccessibleLabel => $"{Kind}: {Title}. {Description}".Trim();

    private static void OnDisplayPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((ToastNotification)dependencyObject).Bindings.Update();
    }
}
