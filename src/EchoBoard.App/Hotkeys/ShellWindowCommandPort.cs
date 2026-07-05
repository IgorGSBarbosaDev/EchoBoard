using EchoBoard.Application.Hotkeys;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace EchoBoard.App.Hotkeys;

public sealed class ShellWindowCommandPort : IShellWindowCommandPort
{
    public Task<HotkeyCommandResult> ShowOrHideMainWindowAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var window = MainWindow.CurrentInstance;
        if (window is null)
        {
            return Task.FromResult(HotkeyCommandResult.Unavailable("Main window is not available."));
        }

        window.DispatcherQueue.TryEnqueue(() => ToggleWindow(window));
        return Task.FromResult(HotkeyCommandResult.Success("Main window toggled."));
    }

    private static void ToggleWindow(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        if (appWindow.Presenter is OverlappedPresenter presenter && presenter.State == OverlappedPresenterState.Minimized)
        {
            presenter.Restore();
            window.Activate();
            return;
        }

        if (appWindow.IsVisible)
        {
            appWindow.Hide();
        }
        else
        {
            appWindow.Show();
            window.Activate();
        }
    }
}
