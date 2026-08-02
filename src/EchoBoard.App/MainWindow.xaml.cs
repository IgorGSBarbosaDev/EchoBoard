using EchoBoard.App.Views;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Infrastructure.Hotkeys;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace EchoBoard.App;

public sealed partial class MainWindow : Window
{
    private readonly HotkeyRuntimeService hotkeyRuntime;

    public MainWindow(
        MainShellPage shellPage,
        WindowsGlobalHotkeyRegistrar hotkeyRegistrar,
        HotkeyRuntimeService hotkeyRuntime)
    {
        this.hotkeyRuntime = hotkeyRuntime;
        InitializeComponent();
        ConfigureWindowIcon();
        CurrentInstance = this;
        Content = shellPage;
        hotkeyRegistrar.Initialize(WindowNative.GetWindowHandle(this));
        Closed += OnClosed;
    }

    public static MainWindow? CurrentInstance { get; private set; }

    private void ConfigureWindowIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "EchoBoard.ico");
        if (!File.Exists(iconPath))
        {
            return;
        }

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow.GetFromWindowId(windowId).SetIcon(iconPath);
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await hotkeyRuntime.DisposeAsync();
        CurrentInstance = null;
    }
}
