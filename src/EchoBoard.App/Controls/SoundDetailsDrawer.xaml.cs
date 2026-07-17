using EchoBoard.App.ViewModels;
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

        var confirmation = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Remover som da biblioteca?",
            Content = "A referência será removida do EchoBoard. O arquivo original permanecerá no computador.",
            PrimaryButtonText = "Remover",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirmation.ShowAsync() == ContentDialogResult.Primary)
        {
            await viewModel.DeleteSelectedAsync(CancellationToken.None);
        }
    }
}
