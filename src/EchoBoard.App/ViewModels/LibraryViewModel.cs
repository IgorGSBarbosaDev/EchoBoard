using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Controls;
using EchoBoard.Application.Library;
using EchoBoard.Domain.Exceptions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly QuerySoundLibraryUseCase queryLibrary;
    private readonly ImportSoundsUseCase importSounds;
    private readonly CreateCategoryUseCase createCategory;
    private readonly UpdateCategoryUseCase updateCategory;
    private readonly DeleteCategoryUseCase deleteCategory;
    private readonly SetSoundFavoriteUseCase setSoundFavorite;
    private readonly AssignSoundCategoryUseCase assignSoundCategory;
    private bool isBusy;
    private string searchText = string.Empty;
    private Guid? selectedCategoryId;
    private bool includeUncategorizedOnly;
    private string? loadError;
    private ToastPreviewModel? importToast;

    public LibraryViewModel(
        QuerySoundLibraryUseCase queryLibrary,
        ImportSoundsUseCase importSounds,
        CreateCategoryUseCase createCategory,
        UpdateCategoryUseCase updateCategory,
        DeleteCategoryUseCase deleteCategory,
        SetSoundFavoriteUseCase setSoundFavorite,
        AssignSoundCategoryUseCase assignSoundCategory)
    {
        this.queryLibrary = queryLibrary;
        this.importSounds = importSounds;
        this.createCategory = createCategory;
        this.updateCategory = updateCategory;
        this.deleteCategory = deleteCategory;
        this.setSoundFavorite = setSoundFavorite;
        this.assignSoundCategory = assignSoundCategory;

        Categories = [];
        Sounds = [];
        ImportFeedbackItems = [];
        DismissImportFeedbackCommand = new RelayCommand(ClearImportFeedback);
        ClearFiltersCommand = new AsyncRelayCommand(ct => ClearFiltersAsync(ct));
        SelectCategoryCommand = new AsyncRelayCommand<CategoryPreviewModel>(SelectCategoryAsync);
        SelectSoundCommand = new RelayCommand<Guid>(SelectSound);

        UpdateCategoryFilters([], totalSoundCount: 0, uncategorizedCount: 0);
    }

    public string Title => "Library";

    public string Subtitle => "Import local MP3 and WAV files, organize categories, and mark favorites.";

    public string EmptyStateTitle
    {
        get
        {
            if (IsBusy)
            {
                return "Loading library";
            }

            if (loadError is not null)
            {
                return "Library unavailable";
            }

            return HasActiveFilters ? "No results" : "No sounds imported";
        }
    }

    public string EmptyStateMessage
    {
        get
        {
            if (IsBusy)
            {
                return "Loading persisted sounds from the local library.";
            }

            if (loadError is not null)
            {
                return loadError;
            }

            return HasActiveFilters
                ? "Try clearing search or category filters."
                : "Import MP3 or WAV files to add them to EchoBoard without copying or changing the originals.";
        }
    }

    public ObservableCollection<CategoryPreviewModel> Categories { get; }

    public ObservableCollection<SoundCardPreviewModel> Sounds { get; }

    public ObservableCollection<ImportFeedbackItemViewModel> ImportFeedbackItems { get; }

    public IReadOnlyList<SoundLibraryCategoryOptionViewModel> AssignableCategories =>
        Categories
            .Where(category => category.Id is not null)
            .Select(category => new SoundLibraryCategoryOptionViewModel(category.Id!.Value, category.Name))
            .Prepend(SoundLibraryCategoryOptionViewModel.Unassigned)
            .ToArray();

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
                NotifyStatePropertiesChanged();
            }
        }
    }

    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                _ = RefreshAsync(CancellationToken.None);
            }
        }
    }

    public Guid? SelectedSoundId { get; private set; }

    public string ImportButtonText => IsBusy ? "Working..." : "Import";

    public bool IsImportEnabled => !IsBusy;

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        selectedCategoryId is not null ||
        includeUncategorizedOnly;

    public Visibility EmptyStateVisibility => IsBusy || loadError is not null || Sounds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SoundGridVisibility => !IsBusy && loadError is null && Sounds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ClearFiltersVisibility => HasActiveFilters ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ImportFeedbackVisibility => ImportFeedbackItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ImportToastVisibility => ImportToast is null ? Visibility.Collapsed : Visibility.Visible;

    public IRelayCommand DismissImportFeedbackCommand { get; }

    public IAsyncRelayCommand ClearFiltersCommand { get; }

    public IAsyncRelayCommand<CategoryPreviewModel> SelectCategoryCommand { get; }

    public IRelayCommand<Guid> SelectSoundCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);
    }

    public async Task UpdateSearchTextAsync(string value, CancellationToken cancellationToken)
    {
        searchText = value;
        OnPropertyChanged(nameof(SearchText));
        await RefreshAsync(cancellationToken);
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
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Error, "Import failed", exception.Message);
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshAsync(cancellationToken);
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

    public async Task CreateCategoryAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            var sortOrder = Categories.Count(category => category.Id is not null);
            await createCategory.ExecuteAsync(new CreateCategoryRequest(name, sortOrder, DateTimeOffset.UtcNow), cancellationToken);
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Success, "Category created", $"Created {name.Trim()}.");
            await RefreshAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DuplicateCategoryNameException or DomainValidationException)
        {
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Error, "Category not saved", exception.Message);
        }
    }

    public async Task RenameSelectedCategoryAsync(string name, CancellationToken cancellationToken)
    {
        if (selectedCategoryId is null)
        {
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Warning, "Select a category", "Choose a category before renaming it.");
            return;
        }

        var selected = Categories.SingleOrDefault(category => category.Id == selectedCategoryId);
        if (selected is null)
        {
            return;
        }

        try
        {
            await updateCategory.ExecuteAsync(new UpdateCategoryRequest(selectedCategoryId.Value, name, GetCategorySortOrder(selectedCategoryId.Value)), cancellationToken);
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Success, "Category renamed", $"Renamed to {name.Trim()}.");
            await RefreshAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DuplicateCategoryNameException or DomainValidationException)
        {
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Error, "Category not saved", exception.Message);
        }
    }

    public async Task DeleteSelectedCategoryAsync(CancellationToken cancellationToken)
    {
        if (selectedCategoryId is null)
        {
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Warning, "Select a category", "Choose a category before deleting it.");
            return;
        }

        await deleteCategory.ExecuteAsync(selectedCategoryId.Value, cancellationToken);
        selectedCategoryId = null;
        includeUncategorizedOnly = false;
        ImportToast = new ToastPreviewModel(ToastNotificationKind.Success, "Category deleted", "Sounds in that category are now unassigned.");
        await RefreshAsync(cancellationToken);
    }

    public async Task ToggleFavoriteAsync(Guid soundId, CancellationToken cancellationToken)
    {
        var sound = Sounds.SingleOrDefault(item => item.Id == soundId);
        if (sound is null)
        {
            return;
        }

        await setSoundFavorite.ExecuteAsync(new SetSoundFavoriteRequest(soundId, !sound.IsFavorite, DateTimeOffset.UtcNow), cancellationToken);
        ImportToast = new ToastPreviewModel(
            ToastNotificationKind.Success,
            !sound.IsFavorite ? "Added to favorites" : "Removed from favorites",
            sound.Title);
        await RefreshAsync(cancellationToken);
    }

    public async Task AssignSoundCategoryAsync(Guid soundId, Guid? categoryId, CancellationToken cancellationToken)
    {
        try
        {
            await assignSoundCategory.ExecuteAsync(new AssignSoundCategoryRequest(soundId, categoryId, DateTimeOffset.UtcNow), cancellationToken);
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Success, "Category updated", "Sound category was updated.");
            await RefreshAsync(cancellationToken);
        }
        catch (CategoryNotFoundException exception)
        {
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Error, "Category not found", exception.Message);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            loadError = null;
            var result = await queryLibrary.ExecuteAsync(
                new SoundLibraryFilter(SearchText, selectedCategoryId, includeUncategorizedOnly, FavoritesOnly: false),
                cancellationToken);
            ReplaceSounds(result.Sounds);
            UpdateCategoryFilters(result.Categories, result.TotalSoundCount, result.UncategorizedSoundCount);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException)
        {
            loadError = exception.Message;
            Sounds.Clear();
        }
        finally
        {
            IsBusy = false;
            NotifyStatePropertiesChanged();
        }
    }

    private async Task ClearFiltersAsync(CancellationToken cancellationToken)
    {
        searchText = string.Empty;
        selectedCategoryId = null;
        includeUncategorizedOnly = false;
        OnPropertyChanged(nameof(SearchText));
        await RefreshAsync(cancellationToken);
    }

    private async Task SelectCategoryAsync(CategoryPreviewModel? category)
    {
        if (category is null)
        {
            return;
        }

        selectedCategoryId = category.FilterKind == SoundLibraryCategoryFilterKinds.Category ? category.Id : null;
        includeUncategorizedOnly = category.FilterKind == SoundLibraryCategoryFilterKinds.Uncategorized;
        await RefreshAsync(CancellationToken.None);
    }

    private void SelectSound(Guid soundId)
    {
        SelectedSoundId = soundId;
        for (var index = 0; index < Sounds.Count; index++)
        {
            var item = Sounds[index];
            Sounds[index] = item with { IsSelected = item.Id == soundId };
        }
    }

    private void ReplaceSounds(IReadOnlyList<SoundLibraryItemDto> sounds)
    {
        Sounds.Clear();
        foreach (var sound in sounds)
        {
            Sounds.Add(new SoundCardPreviewModel(
                sound.Name,
                BuildSoundSubtitle(sound),
                FormatDuration(sound.Duration),
                "No hotkey",
                sound.CategoryName ?? "Uncategorized",
                null,
                IsSelected: sound.Id == SelectedSoundId,
                IsFavorite: sound.IsFavorite,
                IsEnabled: true,
                Id: sound.Id,
                IsMissingFile: sound.IsMissingFile,
                StatusText: sound.IsMissingFile ? "File missing" : string.Empty,
                SelectCommand: SelectSoundCommand,
                FavoriteCommand: new AsyncRelayCommand(_ => ToggleFavoriteAsync(sound.Id, CancellationToken.None))));
        }

        NotifyStatePropertiesChanged();
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

    private void UpdateCategoryFilters(
        IReadOnlyList<SoundLibraryCategoryDto> categories,
        int totalSoundCount,
        int uncategorizedCount)
    {
        Categories.Clear();
        Categories.Add(new CategoryPreviewModel(
            "All sounds",
            FormatCount(totalSoundCount),
            Symbol.Library,
            null,
            IsSelected: selectedCategoryId is null && !includeUncategorizedOnly,
            Id: null,
            FilterKind: SoundLibraryCategoryFilterKinds.All,
            SelectCommand: SelectCategoryCommand));
        Categories.Add(new CategoryPreviewModel(
            "Uncategorized",
            FormatCount(uncategorizedCount),
            Symbol.Audio,
            null,
            IsSelected: includeUncategorizedOnly,
            Id: null,
            FilterKind: SoundLibraryCategoryFilterKinds.Uncategorized,
            SelectCommand: SelectCategoryCommand));

        foreach (var category in categories)
        {
            Categories.Add(new CategoryPreviewModel(
                category.Name,
                FormatCount(category.SoundCount),
                Symbol.Tag,
                null,
                IsSelected: selectedCategoryId == category.Id,
                Id: category.Id,
                FilterKind: SoundLibraryCategoryFilterKinds.Category,
                SelectCommand: SelectCategoryCommand));
        }

        OnPropertyChanged(nameof(AssignableCategories));
        NotifyStatePropertiesChanged();
    }

    private int GetCategorySortOrder(Guid categoryId)
    {
        var index = Categories
            .Where(category => category.Id is not null)
            .Select((category, index) => new { category.Id, Index = index })
            .SingleOrDefault(item => item.Id == categoryId);

        return index?.Index ?? 0;
    }

    private void NotifyImportFeedbackChanged()
    {
        OnPropertyChanged(nameof(ImportFeedbackVisibility));
    }

    private void NotifyStatePropertiesChanged()
    {
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateMessage));
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(SoundGridVisibility));
        OnPropertyChanged(nameof(ClearFiltersVisibility));
        OnPropertyChanged(nameof(HasActiveFilters));
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

    private static string BuildSoundSubtitle(SoundLibraryItemDto sound)
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

    private static string FormatCount(int count)
    {
        return count.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

public static class SoundLibraryCategoryFilterKinds
{
    public const string All = "All";
    public const string Uncategorized = "Uncategorized";
    public const string Category = "Category";
}

public sealed record SoundLibraryCategoryOptionViewModel(Guid? Id, string Name)
{
    public static SoundLibraryCategoryOptionViewModel Unassigned { get; } = new(null, "Unassigned");
}

public sealed record ImportFeedbackItemViewModel(string FileName, string Status, string Message);
