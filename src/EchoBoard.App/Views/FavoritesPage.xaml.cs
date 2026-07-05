using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Views;

public sealed partial class FavoritesPage : Page
{
    private bool hasLoaded;

    public FavoritesPage()
    {
        InitializeComponent();
    }

    private FavoritesViewModel? ViewModel => DataContext as FavoritesViewModel;

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
