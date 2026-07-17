using EchoBoard.App.ViewModels;
using EchoBoard.App.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace EchoBoard.App.Views;

public sealed partial class LibraryPage : Page
{
    private bool hasLoaded;
    private readonly DispatcherTimer playbackTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    public LibraryPage()
    {
        InitializeComponent();
        playbackTimer.Tick += OnPlaybackTimerTick;
    }

    private LibraryViewModel? ViewModel => DataContext as LibraryViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        playbackTimer.Start();
        if (hasLoaded || ViewModel is null)
        {
            return;
        }

        hasLoaded = true;
        await ViewModel.LoadAsync(CancellationToken.None);
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        playbackTimer.Stop();
        if (ViewModel is not null)
        {
            await ViewModel.StopPlaybackAsync(CancellationToken.None);
        }
    }

    private void OnPlaybackTimerTick(object? sender, object e)
    {
        ViewModel?.RefreshPlaybackState();
    }

    private async void OnImportClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var paths = await AudioFilePicker.PickMultipleAsync();
        if (paths.Count == 0)
        {
            ViewModel.ReportImportCancelled();
            return;
        }

        await ViewModel.ImportFilePathsAsync(paths, CancellationToken.None);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Import audio files";
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
