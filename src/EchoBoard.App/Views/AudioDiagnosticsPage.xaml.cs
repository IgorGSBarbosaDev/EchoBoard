using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using EchoBoard.App.ViewModels;

namespace EchoBoard.App.Views;

public sealed partial class AudioDiagnosticsPage : Page
{
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    public AudioDiagnosticsPage()
    {
        InitializeComponent();
        refreshTimer.Tick += OnRefreshTimerTick;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private AudioDiagnosticsViewModel? ViewModel => DataContext as AudioDiagnosticsViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel?.Refresh();
        refreshTimer.Start();
    }

    private void OnRefreshTimerTick(object? sender, object e)
    {
        ViewModel?.Refresh();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        refreshTimer.Stop();
    }
}
