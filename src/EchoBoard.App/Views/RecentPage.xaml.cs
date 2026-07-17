using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Views;

public sealed partial class RecentPage : Page
{
    public RecentPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RecentViewModel viewModel)
        {
            await viewModel.LoadAsync(CancellationToken.None);
        }
    }
}
