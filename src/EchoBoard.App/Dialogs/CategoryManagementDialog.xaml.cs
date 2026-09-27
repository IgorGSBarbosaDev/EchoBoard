using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace EchoBoard.App.Dialogs;

public sealed partial class CategoryManagementDialog : ContentDialog
{
    private LibraryViewModel? viewModel;

    public CategoryManagementDialog()
    {
        InitializeComponent();
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        viewModel = DataContext as LibraryViewModel;
        if (viewModel is null)
        {
            return;
        }

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        await viewModel.LoadCategoryManagementAsync(CancellationToken.None);
    }

    private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        if (viewModel is not null)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            viewModel = null;
        }
    }

    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (viewModel?.IsCategoryEditorSaving == true || viewModel?.IsCategoryDeletionSaving == true)
        {
            args.Cancel = true;
        }
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (viewModel?.IsCategoryEditorActive != true)
        {
            return;
        }

        args.Cancel = true;
        var deferral = args.GetDeferral();
        try
        {
            if (await viewModel.SaveCategoryEditorAsync(CancellationToken.None))
            {
                viewModel.ReturnToCategoryManagementList();
            }
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(LibraryViewModel.IsCategoryEditorActive) &&
            viewModel?.IsCategoryEditorActive == true)
        {
            DispatcherQueue.TryEnqueue(() => CategoryNameTextBox.Focus(FocusState.Programmatic));
        }
    }
}
