using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Controls;
using EchoBoard.Application.Library;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly ListSoundsUseCase listSounds;
    private readonly ImportSoundsUseCase importSounds;
    private bool isBusy;
    private ToastPreviewModel? importToast;

    public LibraryViewModel(ListSoundsUseCase listSounds, ImportSoundsUseCase importSounds)
    {
        this.listSounds = listSounds;
        this.importSounds = importSounds;

        Categories = [];
        Sounds = [];
        ImportFeedbackItems = [];
        DismissImportFeedbackCommand = new RelayCommand(ClearImportFeedback);

        UpdateCategories(soundCount: 0);
    }

    public string Title => "Library";

    public string Subtitle => "Import local MP3 and WAV files and keep their original paths in your sound library.";

    public string EmptyStateTitle => "No sounds imported";

    public string EmptyStateMessage => "Import MP3 or WAV files to add them to EchoBoard without copying or changing the originals.";

    public ObservableCollection<CategoryPreviewModel> Categories { get; }

    public ObservableCollection<SoundCardPreviewModel> Sounds { get; }

    public ObservableCollection<ImportFeedbackItemViewModel> ImportFeedbackItems { get; }

    public ToastPreviewModel? ImportToast
    {
        get => importToast;
        private set
        {
            if (SetProperty(ref importToast, value))
            {
                OnPropertyChanged(nameof(ImportToastVisibility));
            }
        }
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnPropertyChanged(nameof(ImportButtonText));
                OnPropertyChanged(nameof(IsImportEnabled));
                OnPropertyChanged(nameof(EmptyStateVisibility));
            }
        }
    }

    public string ImportButtonText => IsBusy ? "Importing..." : "Import";

    public bool IsImportEnabled => !IsBusy;

    public Visibility EmptyStateVisibility => Sounds.Count == 0 && !IsBusy ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SoundGridVisibility => Sounds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ImportFeedbackVisibility => ImportFeedbackItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ImportToastVisibility => ImportToast is null ? Visibility.Collapsed : Visibility.Visible;

    public IRelayCommand DismissImportFeedbackCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var sounds = await listSounds.ExecuteAsync(cancellationToken);
            ReplaceSounds(sounds);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ImportFilePathsAsync(IReadOnlyList<string> filePaths, CancellationToken cancellationToken)
    {
        if (filePaths.Count == 0)
        {
            ReportImportCancelled();
            return;
        }

        IsBusy = true;
        try
        {
            var result = await importSounds.ExecuteAsync(
                new ImportSoundsRequest(filePaths, DateTimeOffset.UtcNow),
                cancellationToken);

            ReplaceImportFeedback(result.Items);
            ImportToast = BuildImportToast(result.Items);

            await LoadAsync(cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ReportImportCancelled()
    {
        ImportFeedbackItems.Clear();
        ImportToast = new ToastPreviewModel(
            ToastNotificationKind.Info,
            "Import cancelled",
            "No files were added to the library.");
        NotifyImportFeedbackChanged();
    }

    private void ReplaceSounds(IReadOnlyList<SoundDto> sounds)
    {
        Sounds.Clear();
        foreach (var sound in sounds)
        {
            Sounds.Add(new SoundCardPreviewModel(
                sound.Name,
                BuildSoundSubtitle(sound),
                FormatDuration(sound.Duration),
                string.Empty,
                "Uncategorized",
                null,
                IsFavorite: sound.IsFavorite));
        }

        UpdateCategories(sounds.Count);
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(SoundGridVisibility));
    }

    private void ReplaceImportFeedback(IReadOnlyList<ImportSoundItemResult> items)
    {
        ImportFeedbackItems.Clear();
        foreach (var item in items.Where(item => item.Status != ImportSoundStatus.Imported))
        {
            ImportFeedbackItems.Add(new ImportFeedbackItemViewModel(
                Path.GetFileName(item.FilePath),
                item.Status.ToString(),
                item.Message));
        }

        NotifyImportFeedbackChanged();
    }

    private void ClearImportFeedback()
    {
        ImportFeedbackItems.Clear();
        ImportToast = null;
        NotifyImportFeedbackChanged();
    }

    private void NotifyImportFeedbackChanged()
    {
        OnPropertyChanged(nameof(ImportFeedbackVisibility));
    }

    private void UpdateCategories(int soundCount)
    {
        Categories.Clear();
        var countText = soundCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Categories.Add(new CategoryPreviewModel("All sounds", countText, Symbol.Library, null, IsSelected: true));
        Categories.Add(new CategoryPreviewModel("Uncategorized", countText, Symbol.Audio, null));
    }

    private static ToastPreviewModel BuildImportToast(IReadOnlyList<ImportSoundItemResult> items)
    {
        var imported = items.Count(item => item.Status == ImportSoundStatus.Imported);
        var duplicates = items.Count(item => item.Status == ImportSoundStatus.SkippedDuplicate);
        var invalid = items.Count(item => item.Status is ImportSoundStatus.InvalidExtension or ImportSoundStatus.Unreadable or ImportSoundStatus.MetadataFailed);
        var duplicateLabel = duplicates == 1 ? "duplicate" : "duplicates";
        var invalidLabel = invalid == 1 ? "invalid file" : "invalid files";
        var description = $"Imported {imported}, skipped {duplicates} {duplicateLabel}, {invalid} {invalidLabel}.";
        var kind = imported > 0 && invalid == 0 && duplicates == 0
            ? ToastNotificationKind.Success
            : imported > 0
                ? ToastNotificationKind.Warning
                : ToastNotificationKind.Error;

        return new ToastPreviewModel(kind, imported > 0 ? "Import complete" : "No sounds imported", description);
    }

    private static string BuildSoundSubtitle(SoundDto sound)
    {
        var sizeText = FormatFileSize(sound.FileSize);
        var extension = sound.Extension.TrimStart('.').ToUpperInvariant();

        return $"{extension} - {sizeText}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
    }

    private static string FormatFileSize(long fileSize)
    {
        const double kb = 1024;
        const double mb = kb * 1024;

        return fileSize >= mb
            ? $"{fileSize / mb:0.#} MB"
            : $"{Math.Max(fileSize / kb, 0.1):0.#} KB";
    }
}

public sealed record ImportFeedbackItemViewModel(string FileName, string Status, string Message);
