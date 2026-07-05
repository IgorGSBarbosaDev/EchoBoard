using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Controls;

public sealed partial class HotkeyCaptureBox : UserControl
{
    public static readonly DependencyProperty CtrlProperty = DependencyProperty.Register(
        nameof(Ctrl),
        typeof(bool),
        typeof(HotkeyCaptureBox),
        new PropertyMetadata(true));

    public static readonly DependencyProperty AltProperty = DependencyProperty.Register(
        nameof(Alt),
        typeof(bool),
        typeof(HotkeyCaptureBox),
        new PropertyMetadata(false));

    public static readonly DependencyProperty ShiftProperty = DependencyProperty.Register(
        nameof(Shift),
        typeof(bool),
        typeof(HotkeyCaptureBox),
        new PropertyMetadata(false));

    public static readonly DependencyProperty WinProperty = DependencyProperty.Register(
        nameof(Win),
        typeof(bool),
        typeof(HotkeyCaptureBox),
        new PropertyMetadata(false));

    public static readonly DependencyProperty PrimaryKeyProperty = DependencyProperty.Register(
        nameof(PrimaryKey),
        typeof(string),
        typeof(HotkeyCaptureBox),
        new PropertyMetadata(string.Empty));

    public HotkeyCaptureBox()
    {
        InitializeComponent();
    }

    public bool Ctrl
    {
        get => (bool)GetValue(CtrlProperty);
        set => SetValue(CtrlProperty, value);
    }

    public bool Alt
    {
        get => (bool)GetValue(AltProperty);
        set => SetValue(AltProperty, value);
    }

    public bool Shift
    {
        get => (bool)GetValue(ShiftProperty);
        set => SetValue(ShiftProperty, value);
    }

    public bool Win
    {
        get => (bool)GetValue(WinProperty);
        set => SetValue(WinProperty, value);
    }

    public string PrimaryKey
    {
        get => (string)GetValue(PrimaryKeyProperty);
        set => SetValue(PrimaryKeyProperty, value);
    }
}
