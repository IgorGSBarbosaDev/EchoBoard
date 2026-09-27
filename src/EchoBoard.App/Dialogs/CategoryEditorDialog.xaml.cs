using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Dialogs;

public sealed partial class CategoryEditorDialog : ContentDialog
{
    public CategoryEditorDialog()
    {
        InitializeComponent();
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        CategoryNameTextBox.Focus(FocusState.Programmatic);
        if (DataContext is LibraryViewModel viewModel)
        {
            await viewModel.LoadCategoryEditorSoundsAsync(CancellationToken.None);
        }
    }

    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (DataContext is LibraryViewModel { IsCategoryEditorSaving: true })
        {
            args.Cancel = true;
        }
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        var deferral = args.GetDeferral();
        try
        {
            if (DataContext is LibraryViewModel viewModel &&
                await viewModel.SaveCategoryEditorAsync(CancellationToken.None))
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
