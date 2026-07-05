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

    public Visibility DescriptionVisibility => string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;

    public string AccessibleLabel => $"{Kind}: {Title}. {Description}".Trim();

    private static void OnDisplayPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((ToastNotification)dependencyObject).Bindings.Update();
    }
}
