using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Views;

public sealed partial class SettingsPage : Page
{
    private bool hasLoaded;
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    public SettingsPage()
    {
        InitializeComponent();
        refreshTimer.Tick += OnRefreshTimerTick;
        Unloaded += OnUnloaded;
    }

    private SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (hasLoaded || ViewModel is null)
        {
            return;
        }

        hasLoaded = true;
        await ViewModel.LoadAsync(CancellationToken.None);
        refreshTimer.Start();
    }

    private void OnRefreshTimerTick(object? sender, object e)
    {
        ViewModel?.RefreshMicrophoneSnapshot();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        refreshTimer.Stop();
    }
}
