using EchoBoard.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace EchoBoard.App.Views;

public sealed partial class LibraryPage : Page
{
    private bool hasLoaded;

    public LibraryPage()
    {
        InitializeComponent();
    }

    private LibraryViewModel? ViewModel => DataContext as LibraryViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (hasLoaded || ViewModel is null)
        {
            return;
        }

        hasLoaded = true;
        await ViewModel.LoadAsync(CancellationToken.None);
    }

    private async void OnImportClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".mp3");
        picker.FileTypeFilter.Add(".wav");
        picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;

        if (MainWindow.CurrentInstance is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(MainWindow.CurrentInstance));
        }

        var files = await picker.PickMultipleFilesAsync();
        if (files.Count == 0)
        {
            ViewModel.ReportImportCancelled();
            return;
        }

        await ViewModel.ImportFilePathsAsync(files.Select(file => file.Path).ToArray(), CancellationToken.None);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Import MP3 or WAV files";
        }
        else
        {
            e.AcceptedOperation = DataPackageOperation.None;
        }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (ViewModel is null || !e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        var paths = items
            .Where(item => item is StorageFile || !string.IsNullOrWhiteSpace(item.Path))
            .Select(item => item.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();

        await ViewModel.ImportFilePathsAsync(paths, CancellationToken.None);
    }

    private async void OnCreateCategoryClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.CreateCategoryAsync(CreateCategoryNameTextBox.Text, CancellationToken.None);
        CreateCategoryNameTextBox.Text = string.Empty;
    }

    private async void OnRenameCategoryClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RenameSelectedCategoryAsync(RenameCategoryNameTextBox.Text, CancellationToken.None);
        RenameCategoryNameTextBox.Text = string.Empty;
    }

    private async void OnDeleteCategoryClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.DeleteSelectedCategoryAsync(CancellationToken.None);
    }

    private async void OnAssignSelectedCategoryClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedSoundId is not Guid soundId)
        {
            return;
        }

        var selectedCategory = AssignCategoryComboBox.SelectedItem as SoundLibraryCategoryOptionViewModel;
        await ViewModel.AssignSoundCategoryAsync(soundId, selectedCategory?.Id, CancellationToken.None);
    }
}
