using EchoBoard.App.ViewModels;
using EchoBoard.App.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Controls;

public sealed partial class SoundDetailsDrawer : UserControl
{
    public SoundDetailsDrawer()
    {
        InitializeComponent();
    }

    public void FocusInitial()
    {
        CloseButton.Focus(FocusState.Programmatic);
    }

    private async void OnRemoveClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SoundDetailsViewModel viewModel)
        {
            return;
        }

        if (await SoundDeletionConfirmation.ShowAsync(XamlRoot))
        {
            await viewModel.DeleteSelectedAsync(CancellationToken.None);
        }
    }
}
