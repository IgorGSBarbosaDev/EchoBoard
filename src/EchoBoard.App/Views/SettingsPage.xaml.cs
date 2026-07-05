using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Views;

public sealed partial class SettingsPage : Page
{
    private bool hasLoaded;

    public SettingsPage()
    {
        InitializeComponent();
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
    }
}
