using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Dialogs;

public sealed partial class CategoryDeletionDialog : ContentDialog
{
    public CategoryDeletionDialog()
    {
        InitializeComponent();
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        var deferral = args.GetDeferral();
        try
        {
            if (DataContext is LibraryViewModel viewModel &&
                await viewModel.DeleteSelectedCategoryAsync(
                    viewModel.SelectedCategoryDeletionDestination?.Id,
                    CancellationToken.None))
            {
                Hide();
            }
        }
        finally
        {
            deferral.Complete();
        }
    }
}
