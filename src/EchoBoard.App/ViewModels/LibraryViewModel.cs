using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Controls;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Application.Library;
using EchoBoard.Domain.Enums;
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
    private readonly SetCategorySoundsUseCase setCategorySounds;
    private readonly ListHotkeyBindingsUseCase listHotkeys;
    private readonly AssignSoundHotkeyUseCase assignSoundHotkey;
    private readonly RemoveHotkeyBindingUseCase removeHotkeyBinding;
    private readonly SetHotkeyBindingEnabledUseCase setHotkeyBindingEnabled;
    private readonly ISoundPlaybackEngine playback;
    private readonly PlaySoundUseCase? playSound;
    private readonly SoundDetailsViewModel? details;
    private readonly PlaybackCoordinator? playbackCoordinator;
    private readonly TransientNotificationService? notifications;
    private readonly SoundLibraryInteractionCoordinator? libraryInteractions;
    private readonly ListSoundsUseCase? listSounds;
    private readonly ListCategoriesUseCase? listCategories;
    private readonly Dictionary<Guid, HotkeyBindingDto> hotkeyBySoundId = [];
    private readonly Dictionary<Guid, SoundLibraryItemDto> soundById = [];
    private bool isBusy;
    private string searchText = string.Empty;
    private string hotkeyPrimaryKey = string.Empty;
    private Guid? selectedCategoryId;
    private bool isFavoritesOnly;
    private bool hotkeyCtrl = true;
    private bool hotkeyAlt;
    private bool hotkeyShift;
    private bool hotkeyWin;
    private bool isSelectedSoundHotkeyEnabled = true;
    private bool isImportNotificationVisible;
    private string? loadError;
    private ToastPreviewModel? importToast;
    private ToastPreviewModel? playbackToast;
    private Guid? playbackSoundId;
    private bool isCategoryManagementLoading;
    private bool isCategoryEditorActive;
    private bool isCategoryDeletionConfirmationVisible;
    private string? categoryManagementError;
    private Guid? categoryEditorId;
    private string categoryEditorName = string.Empty;
    private string? categoryEditorError;
    private bool isCategoryEditorLoading;
    private bool isCategoryEditorSaving;
    private bool categoryEditorLoadFailed;
    private bool isCategoryDeletionSaving;
    private string? categoryDeletionError;
    private Guid? categoryDeletionTargetId;

    public LibraryViewModel(
        QuerySoundLibraryUseCase queryLibrary,
        ImportSoundsUseCase importSounds,
        CreateCategoryUseCase createCategory,
        UpdateCategoryUseCase updateCategory,
        DeleteCategoryUseCase deleteCategory,
        SetSoundFavoriteUseCase setSoundFavorite,
        SetCategorySoundsUseCase setCategorySounds,
        ListHotkeyBindingsUseCase listHotkeys,
        AssignSoundHotkeyUseCase assignSoundHotkey,
        RemoveHotkeyBindingUseCase removeHotkeyBinding,
        SetHotkeyBindingEnabledUseCase setHotkeyBindingEnabled,
        ISoundPlaybackEngine playback,
        PlaySoundUseCase? playSound = null,
        SoundDetailsViewModel? details = null,
        PlaybackCoordinator? playbackCoordinator = null,
        TransientNotificationService? notifications = null,
        SoundLibraryInteractionCoordinator? libraryInteractions = null,
        ListSoundsUseCase? listSounds = null,
        ListCategoriesUseCase? listCategories = null)
    {
        this.queryLibrary = queryLibrary;
        this.importSounds = importSounds;
        this.createCategory = createCategory;
        this.updateCategory = updateCategory;
        this.deleteCategory = deleteCategory;
        this.setSoundFavorite = setSoundFavorite;
        this.setCategorySounds = setCategorySounds;
        this.listHotkeys = listHotkeys;
        this.assignSoundHotkey = assignSoundHotkey;
        this.removeHotkeyBinding = removeHotkeyBinding;
        this.setHotkeyBindingEnabled = setHotkeyBindingEnabled;
        this.playback = playback;
        this.playSound = playSound;
        this.details = details;
        this.playbackCoordinator = playbackCoordinator;
        this.notifications = notifications;
        this.libraryInteractions = libraryInteractions;
        this.listSounds = listSounds;
        this.listCategories = listCategories;

        Categories = [];
        ManagedCategories = [];
        Sounds = [];
        ImportFeedbackItems = [];
        CategoryEditorSounds = [];
        DismissImportNotificationCommand = new RelayCommand(DismissImportNotification);
        DismissPlaybackFeedbackCommand = new RelayCommand(() => PlaybackToast = null);
        ClearFiltersCommand = new AsyncRelayCommand(ct => ClearFiltersAsync(ct));
        SelectCategoryCommand = new AsyncRelayCommand<CategoryPreviewModel>(SelectCategoryAsync);
        SelectSoundCommand = new RelayCommand<Guid>(SelectSound);
        ActivateSoundCommand = new AsyncRelayCommand<Guid>(ActivateSoundAsync);
        PreviewCategorySoundCommand = new AsyncRelayCommand<Guid>(PreviewCategorySoundAsync);
        EditManagedCategoryCommand = new AsyncRelayCommand<Guid>((id, ct) => BeginCategoryEditorAsync(id, ct));
        RequestCategoryDeletionCommand = new RelayCommand<Guid>(PrepareCategoryDeletion);
        ConfirmCategoryDeletionCommand = new AsyncRelayCommand(ct => DeleteManagedCategoryAsync(ct));
        CancelCategoryDeletionCommand = new RelayCommand(CancelCategoryDeletion);
        SaveSoundHotkeyCommand = new AsyncRelayCommand(ct => SaveSelectedSoundHotkeyAsync(ct));
        RemoveSoundHotkeyCommand = new AsyncRelayCommand(ct => RemoveSelectedSoundHotkeyAsync(ct));
        ToggleSelectedSoundHotkeyEnabledCommand = new AsyncRelayCommand(ct => ToggleSelectedSoundHotkeyEnabledAsync(ct));

        UpdateCategoryFilters([], totalSoundCount: 0);
        if (details is not null)
        {
            details.SoundChanged += OnDetailsSoundChanged;
        }
        if (playbackCoordinator is not null)
        {
            playbackCoordinator.SnapshotChanged += OnPlaybackSnapshotChanged;
            playbackCoordinator.PlaybackConfirmed += OnPlaybackConfirmed;
        }
        if (libraryInteractions is not null)
        {
            libraryInteractions.LibraryChanged += OnLibraryChanged;
        }
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
                ? "Try clearing the search, category, or favorites filter."
                : "Import MP3 or WAV files to add them to EchoBoard without copying or changing the originals.";
        }
    }

    public ObservableCollection<CategoryPreviewModel> Categories { get; }

    public ObservableCollection<ManagedCategoryViewModel> ManagedCategories { get; }

    public ObservableCollection<SoundCardPreviewModel> Sounds { get; }

    public ObservableCollection<ImportFeedbackItemViewModel> ImportFeedbackItems { get; }

    public ObservableCollection<CategorySoundSelectionViewModel> CategoryEditorSounds { get; }

    public Guid? SelectedCategoryId => selectedCategoryId;

    public bool IsCategoryManagementLoading
    {
        get => isCategoryManagementLoading;
        private set
        {
            if (SetProperty(ref isCategoryManagementLoading, value))
            {
                OnPropertyChanged(nameof(CategoryManagementLoadingVisibility));
                OnPropertyChanged(nameof(CategoryManagementItemsVisibility));
                OnPropertyChanged(nameof(CategoryManagementEmptyVisibility));
            }
        }
    }

    public Visibility CategoryManagementLoadingVisibility =>
        IsCategoryManagementLoading ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CategoryManagementListVisibility =>
        IsCategoryEditorActive ? Visibility.Collapsed : Visibility.Visible;

    public Visibility CategoryManagementEditorVisibility =>
        IsCategoryEditorActive ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CategoryManagementItemsVisibility =>
        !IsCategoryManagementLoading && !IsCategoryDeletionSaving && ManagedCategories.Count > 0 && !IsCategoryEditorActive
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility CategoryManagementEmptyVisibility =>
        !IsCategoryManagementLoading && ManagedCategories.Count == 0 && !IsCategoryEditorActive &&
        string.IsNullOrWhiteSpace(CategoryManagementError)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public string? CategoryManagementError
    {
        get => categoryManagementError;
        private set
        {
            if (SetProperty(ref categoryManagementError, value))
            {
                OnPropertyChanged(nameof(CategoryManagementErrorVisibility));
                OnPropertyChanged(nameof(CategoryManagementEmptyVisibility));
            }
        }
    }

    public Visibility CategoryManagementErrorVisibility =>
        string.IsNullOrWhiteSpace(CategoryManagementError) ? Visibility.Collapsed : Visibility.Visible;

    public bool IsCategoryEditorActive
    {
        get => isCategoryEditorActive;
        private set
        {
            if (SetProperty(ref isCategoryEditorActive, value))
            {
                OnPropertyChanged(nameof(CategoryManagementListVisibility));
                OnPropertyChanged(nameof(CategoryManagementEditorVisibility));
                OnPropertyChanged(nameof(CategoryManagementItemsVisibility));
                OnPropertyChanged(nameof(CategoryManagementEmptyVisibility));
                OnPropertyChanged(nameof(CategoryManagementPrimaryButtonText));
                OnPropertyChanged(nameof(CategoryManagementPrimaryButtonEnabled));
            }
        }
    }

    public string CategoryManagementPrimaryButtonText =>
        IsCategoryEditorActive ? CategoryEditorSaveButtonText : "Done";

    public bool CategoryManagementPrimaryButtonEnabled =>
        !IsCategoryEditorActive || CategoryEditorSaveEnabled;

    public Visibility CategoryDeletionConfirmationVisibility =>
        isCategoryDeletionConfirmationVisible ? Visibility.Visible : Visibility.Collapsed;

    public string CategoryDeletionTargetName =>
        ManagedCategories.SingleOrDefault(category => category.Id == categoryDeletionTargetId)?.Name ?? "this category";

    public string CategoryEditorTitle => categoryEditorId is null ? "Create category" : "Edit category";

    public string CategoryEditorSaveButtonText => categoryEditorId is null ? "Create" : "Save";

    public string CategoryEditorName
    {
        get => categoryEditorName;
        set
        {
            if (SetProperty(ref categoryEditorName, value))
            {
                CategoryEditorError = null;
                OnPropertyChanged(nameof(CategoryEditorSaveEnabled));
                OnPropertyChanged(nameof(CategoryManagementPrimaryButtonEnabled));
            }
        }
    }

    public string? CategoryEditorError
    {
        get => categoryEditorError;
        private set
        {
            if (SetProperty(ref categoryEditorError, value))
            {
                OnPropertyChanged(nameof(CategoryEditorErrorVisibility));
                OnPropertyChanged(nameof(CategoryEditorEmptyVisibility));
                OnPropertyChanged(nameof(CategoryEditorSaveEnabled));
                OnPropertyChanged(nameof(CategoryManagementPrimaryButtonEnabled));
            }
        }
    }

    public bool IsCategoryEditorLoading
    {
        get => isCategoryEditorLoading;
        private set
        {
            if (SetProperty(ref isCategoryEditorLoading, value))
            {
                OnPropertyChanged(nameof(CategoryEditorLoadingVisibility));
                OnPropertyChanged(nameof(CategoryEditorListVisibility));
                OnPropertyChanged(nameof(CategoryEditorEmptyVisibility));
                OnPropertyChanged(nameof(CategoryEditorSaveEnabled));
                OnPropertyChanged(nameof(CategoryManagementPrimaryButtonEnabled));
            }
        }
    }

    public bool IsCategoryEditorSaving => isCategoryEditorSaving;

    public Visibility CategoryEditorSavingVisibility =>
        IsCategoryEditorSaving ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CategoryEditorErrorVisibility =>
        string.IsNullOrWhiteSpace(CategoryEditorError) ? Visibility.Collapsed : Visibility.Visible;

    public Visibility CategoryEditorLoadingVisibility =>
        IsCategoryEditorLoading ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CategoryEditorListVisibility =>
        !IsCategoryEditorLoading && CategoryEditorSounds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CategoryEditorEmptyVisibility =>
        !IsCategoryEditorLoading &&
        CategoryEditorSounds.Count == 0 &&
        string.IsNullOrWhiteSpace(CategoryEditorError)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public bool CategoryEditorSaveEnabled =>
        !IsCategoryEditorLoading &&
        !IsCategoryEditorSaving &&
        !categoryEditorLoadFailed &&
        !string.IsNullOrWhiteSpace(CategoryEditorName);

    public string CategoryDeletionDescription =>
        "Only the category and its links are removed. The audio stays in your library and in any other categories.";

    public string? CategoryDeletionError
    {
        get => categoryDeletionError;
        private set
        {
            if (SetProperty(ref categoryDeletionError, value))
            {
                OnPropertyChanged(nameof(CategoryDeletionErrorVisibility));
            }
        }
    }

    public Visibility CategoryDeletionErrorVisibility =>
        string.IsNullOrWhiteSpace(CategoryDeletionError) ? Visibility.Collapsed : Visibility.Visible;

    public bool IsCategoryDeletionSaving => isCategoryDeletionSaving;

    public bool CategoryDeletionCancelEnabled =>
        categoryDeletionTargetId is not null && !IsCategoryDeletionSaving;

    public Visibility CategoryDeletionSavingVisibility =>
        IsCategoryDeletionSaving ? Visibility.Visible : Visibility.Collapsed;

    public bool CategoryDeletionSaveEnabled => categoryDeletionTargetId is not null && !IsCategoryDeletionSaving;

    public ToastPreviewModel? ImportToast
    {
        get => importToast;
        private set
        {
            var changed = SetProperty(ref importToast, value);
            IsImportNotificationVisible = value is not null;
            if (changed)
            {
                OnPropertyChanged(nameof(ImportToastVisibility));
            }
        }
    }

    public bool IsImportNotificationVisible
    {
        get => isImportNotificationVisible;
        private set
        {
            if (SetProperty(ref isImportNotificationVisible, value))
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

    public bool IsFavoritesOnly
    {
        get => isFavoritesOnly;
        set
        {
            if (SetProperty(ref isFavoritesOnly, value))
            {
                _ = RefreshAsync(CancellationToken.None);
            }
        }
    }

    public async Task UpdateFavoritesOnlyAsync(bool value, CancellationToken cancellationToken)
    {
        if (!SetProperty(ref isFavoritesOnly, value, nameof(IsFavoritesOnly)))
        {
            return;
        }

        if (value && selectedCategoryId is not null)
        {
            selectedCategoryId = null;
            OnPropertyChanged(nameof(SelectedCategoryId));
        }

        await RefreshAsync(cancellationToken);
    }

    public Guid? SelectedSoundId { get; private set; }

    public string ImportButtonText => IsBusy ? "Working..." : "Import";

    public string HotkeyPrimaryKey
    {
        get => hotkeyPrimaryKey;
        set => SetProperty(ref hotkeyPrimaryKey, value);
    }

    public ToastPreviewModel? PlaybackToast
    {
        get => playbackToast;
        private set
        {
            if (SetProperty(ref playbackToast, value))
            {
                OnPropertyChanged(nameof(PlaybackToastVisibility));
            }
        }
    }

    public bool HotkeyCtrl
    {
        get => hotkeyCtrl;
        set => SetProperty(ref hotkeyCtrl, value);
    }

    public bool HotkeyAlt
    {
        get => hotkeyAlt;
        set => SetProperty(ref hotkeyAlt, value);
    }

    public bool HotkeyShift
    {
        get => hotkeyShift;
        set => SetProperty(ref hotkeyShift, value);
    }

    public bool HotkeyWin
    {
        get => hotkeyWin;
        set => SetProperty(ref hotkeyWin, value);
    }

    public bool IsSelectedSoundHotkeyEnabled
    {
        get => isSelectedSoundHotkeyEnabled;
        set => SetProperty(ref isSelectedSoundHotkeyEnabled, value);
    }

    public string SelectedSoundHotkeyText =>
        SelectedSoundId is not null && hotkeyBySoundId.TryGetValue(SelectedSoundId.Value, out var binding)
            ? $"{binding.NormalizedKeyCombination} ({binding.RegistrationState})"
            : "No hotkey assigned";

    public bool IsImportEnabled => !IsBusy;

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        selectedCategoryId is not null ||
        IsFavoritesOnly;

    public Visibility EmptyStateVisibility => IsBusy || loadError is not null || Sounds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SoundGridVisibility => !IsBusy && loadError is null && Sounds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ClearFiltersVisibility => HasActiveFilters ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ImportFeedbackVisibility => ImportFeedbackItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ImportToastVisibility => IsImportNotificationVisible && ImportToast is not null ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PlaybackToastVisibility => PlaybackToast is null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility SoundHotkeyEditorVisibility => SelectedSoundId is null ? Visibility.Collapsed : Visibility.Visible;

    public IRelayCommand DismissImportNotificationCommand { get; }

    public IRelayCommand DismissPlaybackFeedbackCommand { get; }

    public IAsyncRelayCommand ClearFiltersCommand { get; }

    public IAsyncRelayCommand<CategoryPreviewModel> SelectCategoryCommand { get; }

    public IRelayCommand<Guid> SelectSoundCommand { get; }

    public IAsyncRelayCommand<Guid> ActivateSoundCommand { get; }

    public IAsyncRelayCommand<Guid> PreviewCategorySoundCommand { get; }

    public IAsyncRelayCommand<Guid> EditManagedCategoryCommand { get; }

    public IRelayCommand<Guid> RequestCategoryDeletionCommand { get; }

    public IAsyncRelayCommand ConfirmCategoryDeletionCommand { get; }

    public IRelayCommand CancelCategoryDeletionCommand { get; }

    public IAsyncRelayCommand SaveSoundHotkeyCommand { get; }

    public IAsyncRelayCommand RemoveSoundHotkeyCommand { get; }

    public IAsyncRelayCommand ToggleSelectedSoundHotkeyEnabledCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);
    }

    public void PrepareCategoryManagement()
    {
        ManagedCategories.Clear();
        CategoryManagementError = null;
        IsCategoryManagementLoading = true;
        IsCategoryEditorActive = false;
        CancelCategoryDeletion();
    }

    public async Task LoadCategoryManagementAsync(CancellationToken cancellationToken)
    {
        if (!IsCategoryManagementLoading)
        {
            return;
        }

        try
        {
            if (listSounds is not null && listCategories is not null)
            {
                var categories = await listCategories.ExecuteAsync(cancellationToken);
                var sounds = await listSounds.ExecuteAsync(cancellationToken);
                UpdateManagedCategories(categories, sounds);
            }
            else
            {
                var result = await queryLibrary.ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
                UpdateManagedCategories(result.Categories);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            CategoryManagementError = $"Could not load categories. {exception.Message}";
        }
        finally
        {
            IsCategoryManagementLoading = false;
        }
    }

    public void ReturnToCategoryManagementList()
    {
        IsCategoryEditorActive = false;
        CategoryEditorError = null;
        CategoryEditorSounds.Clear();
        OnPropertyChanged(nameof(CategoryEditorListVisibility));
        OnPropertyChanged(nameof(CategoryEditorEmptyVisibility));
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

    public void ReportCategoryDialogFailure(string title)
    {
        notifications?.Show(
            ToastNotificationKind.Error,
            title,
            "The error was recorded in the application log.");
    }

    public void PrepareCategoryEditor(Guid? categoryId)
    {
        IsCategoryEditorActive = true;
        CancelCategoryDeletion();
        categoryEditorId = categoryId;
        CategoryEditorName = categoryId is null
            ? string.Empty
            : Categories.SingleOrDefault(category => category.Id == categoryId)?.Name ?? string.Empty;
        CategoryEditorError = null;
        categoryEditorLoadFailed = false;
        CategoryEditorSounds.Clear();
        IsCategoryEditorLoading = true;
        OnPropertyChanged(nameof(CategoryEditorTitle));
        OnPropertyChanged(nameof(CategoryEditorSaveButtonText));
        OnPropertyChanged(nameof(CategoryEditorListVisibility));
        OnPropertyChanged(nameof(CategoryEditorEmptyVisibility));
        OnPropertyChanged(nameof(CategoryManagementPrimaryButtonText));
        OnPropertyChanged(nameof(CategoryManagementPrimaryButtonEnabled));
    }

    public async Task BeginCategoryEditorAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        PrepareCategoryEditor(categoryId);
        await LoadCategoryEditorSoundsAsync(cancellationToken);
    }

    public async Task LoadCategoryEditorSoundsAsync(CancellationToken cancellationToken)
    {
        if (!IsCategoryEditorLoading)
        {
            return;
        }

        try
        {
            var editorSounds = listSounds is null
                ? (await queryLibrary.ExecuteAsync(SoundLibraryFilter.All, cancellationToken)).Sounds
                    .Select(sound => (sound.Id, sound.Name, sound.FilePath, sound.CategoryIds, sound.IsMissingFile))
                    .ToArray()
                : (await listSounds.ExecuteAsync(cancellationToken))
                    .Select(sound => (sound.Id, sound.Name, sound.FilePath, sound.CategoryIds, IsMissingFile: false))
                    .ToArray();

            CategoryEditorSounds.Clear();
            foreach (var sound in editorSounds.OrderBy(sound => sound.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                playbackCoordinator?.TrackSound(sound.Id, sound.FilePath);
                CategoryEditorSounds.Add(new CategorySoundSelectionViewModel(
                    sound.Id,
                    sound.Name,
                    sound.CategoryIds,
                    categoryEditorId,
                    sound.IsMissingFile,
                    PreviewCategorySoundCommand));
            }

            OnPropertyChanged(nameof(CategoryEditorListVisibility));
            OnPropertyChanged(nameof(CategoryEditorEmptyVisibility));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            categoryEditorLoadFailed = true;
            CategoryEditorError = $"Could not load the sound library. {exception.Message}";
            OnPropertyChanged(nameof(CategoryEditorSaveEnabled));
        }
        finally
        {
            IsCategoryEditorLoading = false;
        }
    }

    public async Task<bool> SaveCategoryEditorAsync(CancellationToken cancellationToken)
    {
        if (!CategoryEditorSaveEnabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(CategoryEditorName))
        {
            CategoryEditorError = "Enter a category name.";
            return false;
        }

        isCategoryEditorSaving = true;
        OnPropertyChanged(nameof(IsCategoryEditorSaving));
        OnPropertyChanged(nameof(CategoryEditorSavingVisibility));
        OnPropertyChanged(nameof(CategoryEditorSaveEnabled));
        OnPropertyChanged(nameof(CategoryManagementPrimaryButtonEnabled));
        CategoryEditorError = null;
        var wasCreating = categoryEditorId is null;

        try
        {
            if (categoryEditorId is null)
            {
                var sortOrder = Categories.Count(category => category.Id is not null);
                var created = await createCategory.ExecuteAsync(
                    new CreateCategoryRequest(CategoryEditorName, sortOrder, DateTimeOffset.UtcNow),
                    cancellationToken);
                categoryEditorId = created.Id;
                OnPropertyChanged(nameof(CategoryEditorTitle));
                OnPropertyChanged(nameof(CategoryEditorSaveButtonText));
                OnPropertyChanged(nameof(CategoryManagementPrimaryButtonText));
            }
            else
            {
                await updateCategory.ExecuteAsync(
                    new UpdateCategoryRequest(categoryEditorId.Value, CategoryEditorName, GetCategorySortOrder(categoryEditorId.Value)),
                    cancellationToken);
            }

            var selectedSoundIds = CategoryEditorSounds
                .Where(sound => sound.IsSelected)
                .Select(sound => sound.Id)
                .ToArray();
            await setCategorySounds.ExecuteAsync(categoryEditorId!.Value, selectedSoundIds, cancellationToken);

            foreach (var sound in CategoryEditorSounds)
            {
                sound.MarkPersistedSelection();
            }

            ImportToast = new ToastPreviewModel(
                ToastNotificationKind.Success,
                wasCreating ? "Category created" : "Category updated",
                $"Saved {CategoryEditorName.Trim()}.");
            await RefreshAsync(cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            CategoryEditorError = exception.Message;
            return false;
        }
        finally
        {
            isCategoryEditorSaving = false;
            OnPropertyChanged(nameof(IsCategoryEditorSaving));
            OnPropertyChanged(nameof(CategoryEditorSavingVisibility));
            OnPropertyChanged(nameof(CategoryEditorSaveEnabled));
            OnPropertyChanged(nameof(CategoryManagementPrimaryButtonEnabled));
        }
    }

    public void PrepareCategoryDeletion(Guid categoryId)
    {
        categoryDeletionTargetId = categoryId;
        isCategoryDeletionConfirmationVisible = true;
        CategoryDeletionError = null;
        OnPropertyChanged(nameof(CategoryDeletionDescription));
        OnPropertyChanged(nameof(CategoryDeletionSaveEnabled));
        OnPropertyChanged(nameof(CategoryDeletionCancelEnabled));
        OnPropertyChanged(nameof(CategoryDeletionConfirmationVisibility));
        OnPropertyChanged(nameof(CategoryDeletionTargetName));
    }

    public void CancelCategoryDeletion()
    {
        categoryDeletionTargetId = null;
        isCategoryDeletionConfirmationVisible = false;
        CategoryDeletionError = null;
        OnPropertyChanged(nameof(CategoryDeletionConfirmationVisibility));
        OnPropertyChanged(nameof(CategoryDeletionTargetName));
        OnPropertyChanged(nameof(CategoryDeletionSaveEnabled));
        OnPropertyChanged(nameof(CategoryDeletionCancelEnabled));
    }

    public async Task<bool> DeleteManagedCategoryAsync(CancellationToken cancellationToken)
    {
        if (categoryDeletionTargetId is not Guid categoryId || isCategoryDeletionSaving)
        {
            CategoryDeletionError = "Select a category to delete.";
            return false;
        }

        isCategoryDeletionSaving = true;
        OnPropertyChanged(nameof(IsCategoryDeletionSaving));
        OnPropertyChanged(nameof(CategoryDeletionSavingVisibility));
        OnPropertyChanged(nameof(CategoryManagementItemsVisibility));
        OnPropertyChanged(nameof(CategoryDeletionSaveEnabled));
        OnPropertyChanged(nameof(CategoryDeletionCancelEnabled));
        CategoryDeletionError = null;

        try
        {
            await deleteCategory.ExecuteAsync(categoryId, cancellationToken);
            if (selectedCategoryId == categoryId)
            {
                selectedCategoryId = null;
                isFavoritesOnly = false;
                OnPropertyChanged(nameof(SelectedCategoryId));
                OnPropertyChanged(nameof(IsFavoritesOnly));
            }

            ImportToast = new ToastPreviewModel(
                ToastNotificationKind.Success,
                "Category deleted",
                "Its audio remains in the library and in any other categories.");
            isCategoryDeletionConfirmationVisible = false;
            categoryDeletionTargetId = null;
            OnPropertyChanged(nameof(CategoryDeletionConfirmationVisibility));
            await RefreshAsync(cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            CategoryDeletionError = exception.Message;
            return false;
        }
        finally
        {
            isCategoryDeletionSaving = false;
            OnPropertyChanged(nameof(IsCategoryDeletionSaving));
            OnPropertyChanged(nameof(CategoryDeletionSavingVisibility));
            OnPropertyChanged(nameof(CategoryManagementItemsVisibility));
            OnPropertyChanged(nameof(CategoryDeletionSaveEnabled));
            OnPropertyChanged(nameof(CategoryDeletionCancelEnabled));
        }
    }

    public async Task ToggleFavoriteAsync(Guid soundId, CancellationToken cancellationToken)
    {
        var sound = Sounds.SingleOrDefault(item => item.Id == soundId);
        if (sound is null)
        {
            return;
        }

        await setSoundFavorite.ExecuteAsync(new SetSoundFavoriteRequest(soundId, !sound.IsFavorite, DateTimeOffset.UtcNow), cancellationToken);
        notifications?.Dismiss();
        ImportToast = null;
        await RefreshAsync(cancellationToken);
    }

    private async Task PreviewCategorySoundAsync(Guid soundId, CancellationToken cancellationToken)
    {
        if (playbackCoordinator is not null)
        {
            await playbackCoordinator.PlayAsync(soundId, cancellationToken);
            return;
        }

        var library = await queryLibrary.ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
        var sound = library.Sounds.SingleOrDefault(item => item.Id == soundId);
        if (sound is null)
        {
            return;
        }

        if (sound.IsMissingFile)
        {
            ShowPlaybackError(sound.Name, "The audio file could not be found. It may have been moved or deleted.");
            return;
        }

        try
        {
            if (playSound is null)
            {
                await playback.StopAllAsync(cancellationToken);
                await playback.PlayAsync(sound.FilePath, sound.Volume, cancellationToken);
            }
            else
            {
                await playSound.ExecuteAsync(new PlaySoundRequest(sound.Id, DateTimeOffset.UtcNow), cancellationToken);
            }

            playbackSoundId = soundId;
            PlaybackToast = null;
            ApplyPlaybackSnapshot(playback.GetSnapshot());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or InvalidOperationException or ArgumentException or UnauthorizedAccessException)
        {
            playbackSoundId = null;
            ApplyPlaybackSnapshot(SoundPlaybackSnapshot.Idle);
            ShowPlaybackError(sound.Name, "The audio file is missing, corrupted, unsupported, or no playback device is available.");
        }
    }

    public async Task StopPlaybackAsync(CancellationToken cancellationToken)
    {
        if (playbackCoordinator is not null)
        {
            await playbackCoordinator.StopAsync(cancellationToken);
        }
        else
        {
            await playback.StopAllAsync(cancellationToken);
        }
        playbackSoundId = null;
        ApplyPlaybackSnapshot(SoundPlaybackSnapshot.Idle);
    }

    public void RefreshPlaybackState()
    {
        ApplyPlaybackSnapshot(playback.GetSnapshot());
    }

    public async Task SaveSelectedSoundHotkeyAsync(CancellationToken cancellationToken)
    {
        if (SelectedSoundId is null)
        {
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Warning, "Select a sound", "Choose a sound before assigning a hotkey.");
            return;
        }

        try
        {
            var result = await assignSoundHotkey.ExecuteAsync(
                new AssignSoundHotkeyRequest(
                    SelectedSoundId.Value,
                    BuildSelectedModifiers(),
                    HotkeyPrimaryKey,
                    IsSelectedSoundHotkeyEnabled,
                    DateTimeOffset.UtcNow),
                cancellationToken);
            ImportToast = ToastForRegistration("Hotkey saved", result);
            await RefreshAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DuplicateHotkeyBindingException or DomainValidationException)
        {
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Error, "Hotkey not saved", exception.Message);
        }
    }

    public async Task RemoveSelectedSoundHotkeyAsync(CancellationToken cancellationToken)
    {
        if (SelectedSoundId is null || !hotkeyBySoundId.TryGetValue(SelectedSoundId.Value, out var binding))
        {
            ImportToast = new ToastPreviewModel(ToastNotificationKind.Info, "No hotkey assigned", "The selected sound has no hotkey to remove.");
            return;
        }

        await removeHotkeyBinding.ExecuteAsync(binding.Id, cancellationToken);
        ImportToast = new ToastPreviewModel(ToastNotificationKind.Success, "Hotkey removed", "The selected sound no longer has a hotkey.");
        await RefreshAsync(cancellationToken);
    }

    public async Task ToggleSelectedSoundHotkeyEnabledAsync(CancellationToken cancellationToken)
    {
        if (SelectedSoundId is null || !hotkeyBySoundId.TryGetValue(SelectedSoundId.Value, out var binding))
        {
            return;
        }

        var result = await setHotkeyBindingEnabled.ExecuteAsync(binding.Id, !binding.IsEnabled, DateTimeOffset.UtcNow, cancellationToken);
        ImportToast = ToastForRegistration(result.IsEnabled ? "Hotkey enabled" : "Hotkey disabled", result);
        await RefreshAsync(cancellationToken);
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            loadError = null;
            await RefreshHotkeysAsync(cancellationToken);
            var result = await queryLibrary.ExecuteAsync(
                new SoundLibraryFilter(SearchText, selectedCategoryId, IncludeUncategorizedOnly: false, FavoritesOnly: IsFavoritesOnly),
                cancellationToken);
            ReplaceSounds(result.Sounds);
            UpdateCategoryFilters(result.Categories, result.TotalSoundCount);
            UpdateManagedCategories(result.Categories);
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
        if (isFavoritesOnly)
        {
            isFavoritesOnly = false;
            OnPropertyChanged(nameof(IsFavoritesOnly));
        }
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(HasActiveFilters));
        await RefreshAsync(cancellationToken);
    }

    private async Task SelectCategoryAsync(CategoryPreviewModel? category)
    {
        if (category is null)
        {
            return;
        }

        selectedCategoryId = category.FilterKind == SoundLibraryCategoryFilterKinds.Category ? category.Id : null;
        var favoritesSelected = category.FilterKind == SoundLibraryCategoryFilterKinds.Favorites;
        if (isFavoritesOnly != favoritesSelected)
        {
            isFavoritesOnly = favoritesSelected;
            OnPropertyChanged(nameof(IsFavoritesOnly));
        }

        OnPropertyChanged(nameof(SelectedCategoryId));
        await RefreshAsync(CancellationToken.None);
    }

    private void SelectSound(Guid soundId)
    {
        SelectedSoundId = soundId;
        for (var index = 0; index < Sounds.Count; index++)
        {
            var item = Sounds[index];
            item.IsSelected = item.Id == soundId;
        }

        PopulateHotkeyEditor(soundId);
        NotifyHotkeyPropertiesChanged();
    }

    private async Task ActivateSoundAsync(Guid soundId, CancellationToken cancellationToken)
    {
        SelectSound(soundId);
        if (!soundById.TryGetValue(soundId, out var sound))
        {
            return;
        }

        if (sound.IsMissingFile)
        {
            await StopPlaybackAsync(cancellationToken);
            ShowPlaybackError(sound.Name, "The audio file could not be found. It may have been moved or deleted.");
            return;
        }

        try
        {
            var snapshot = playback.GetSnapshot();
            if (playbackSoundId == soundId && (snapshot.IsPlaying || snapshot.IsPaused))
            {
                await playback.TogglePauseAsync(cancellationToken);
                ApplyPlaybackSnapshot(playback.GetSnapshot());
                return;
            }

            if (playSound is null)
            {
                await playback.StopAllAsync(cancellationToken);
                await playback.PlayAsync(sound.FilePath, sound.Volume, cancellationToken);
            }
            else
            {
                await playSound.ExecuteAsync(new PlaySoundRequest(sound.Id, DateTimeOffset.UtcNow), cancellationToken);
            }

            playbackSoundId = soundId;
            PlaybackToast = null;
            ApplyPlaybackSnapshot(playback.GetSnapshot());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            playbackSoundId = null;
            ApplyPlaybackSnapshot(SoundPlaybackSnapshot.Idle);
            ShowPlaybackError(sound.Name, "The audio file is missing, corrupted, unsupported, or no playback device is available.");
        }
    }

    private void ApplyPlaybackSnapshot(SoundPlaybackSnapshot snapshot)
    {
        if (snapshot.IsPlaying || snapshot.IsPaused)
        {
            var snapshotSound = soundById.Values.FirstOrDefault(sound =>
                string.Equals(sound.FilePath, snapshot.FilePath, StringComparison.OrdinalIgnoreCase));
            playbackSoundId = snapshotSound?.Id;
        }

        if (playbackSoundId is not Guid activeSoundId)
        {
            UpdatePlaybackCards(null, SoundPlaybackSnapshot.Idle);
            return;
        }

        if (!snapshot.IsPlaying && !snapshot.IsPaused)
        {
            playbackSoundId = null;
            UpdatePlaybackCards(null, SoundPlaybackSnapshot.Idle);
            return;
        }

        UpdatePlaybackCards(activeSoundId, snapshot);
    }

    private void UpdatePlaybackCards(Guid? activeSoundId, SoundPlaybackSnapshot snapshot)
    {
        for (var index = 0; index < Sounds.Count; index++)
        {
            var item = Sounds[index];
            var isActive = item.Id == activeSoundId;
            item.IsPlaying = isActive && snapshot.IsPlaying;
            item.IsPaused = isActive && snapshot.IsPaused;
            item.StatusText = item.IsMissingFile ? "File missing" : string.Empty;
        }
    }

    private void ShowPlaybackError(string soundName, string message)
    {
        PlaybackToast = new ToastPreviewModel(ToastNotificationKind.Error, $"Could not play {soundName}", message);
        notifications?.Show(ToastNotificationKind.Error, $"Não foi possível reproduzir {soundName}", message);
    }

    private void ReplaceSounds(IReadOnlyList<SoundLibraryItemDto> sounds)
    {
        soundById.Clear();
        Sounds.Clear();
        foreach (var sound in sounds)
        {
            soundById[sound.Id] = sound;
            playbackCoordinator?.TrackSound(sound.Id, sound.FilePath);
            Sounds.Add(new SoundCardPreviewModel(
                sound.Name,
                BuildSoundSubtitle(sound),
                FormatDuration(sound.Duration),
                hotkeyBySoundId.TryGetValue(sound.Id, out var binding) ? binding.NormalizedKeyCombination : "No hotkey",
                IsSelected: sound.Id == SelectedSoundId,
                IsFavorite: sound.IsFavorite,
                IsEnabled: true,
                Id: sound.Id,
                IsMissingFile: sound.IsMissingFile,
                StatusText: sound.IsMissingFile ? "File missing" : string.Empty,
                SelectCommand: playbackCoordinator?.PlaySoundCommand ?? ActivateSoundCommand,
                FavoriteCommand: (System.Windows.Input.ICommand?)libraryInteractions?.ToggleFavoriteCommand
                    ?? new AsyncRelayCommand(_ => ToggleFavoriteAsync(sound.Id, CancellationToken.None)),
                FormatText: sound.Extension.TrimStart('.').ToUpperInvariant(),
                UsageText: $"{sound.PlayCount} {(sound.PlayCount == 1 ? "uso" : "usos")}",
                WaveformBars: ToWaveform(sound.WaveformPeaks),
                DetailsCommand: details?.OpenCommand,
                EditCommand: details?.OpenEditCommand,
                DeleteCommand: libraryInteractions?.DeleteSoundCommand));
        }

        if (SelectedSoundId is Guid selectedSoundId && !soundById.ContainsKey(selectedSoundId))
        {
            SelectedSoundId = null;
            OnPropertyChanged(nameof(SelectedSoundId));
            PopulateHotkeyEditor(Guid.Empty);
        }

        ApplyPlaybackSnapshot(playback.GetSnapshot());

        NotifyStatePropertiesChanged();
    }

    private void OnPlaybackSnapshotChanged(object? sender, PlaybackSnapshotChange e)
    {
        playbackSoundId = e.SoundId;
        ApplyPlaybackSnapshot(e.Snapshot);
    }

    private void OnPlaybackConfirmed(object? sender, Guid soundId)
    {
        if (!soundById.TryGetValue(soundId, out var sound))
        {
            return;
        }

        var updated = sound with { PlayCount = sound.PlayCount + 1 };
        soundById[soundId] = updated;
        for (var index = 0; index < Sounds.Count; index++)
        {
            if (Sounds[index].Id == soundId)
            {
                Sounds[index].UsageText = $"{updated.PlayCount} {(updated.PlayCount == 1 ? "uso" : "usos")}";
            }
        }
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

    private void DismissImportNotification()
    {
        IsImportNotificationVisible = false;
    }

    private void UpdateCategoryFilters(
        IReadOnlyList<SoundLibraryCategoryDto> categories,
        int totalSoundCount)
    {
        Categories.Clear();
        Categories.Add(new CategoryPreviewModel(
            "All sounds",
            FormatCount(totalSoundCount),
            Symbol.Library,
            IsSelected: selectedCategoryId is null && !IsFavoritesOnly,
            Id: null,
            FilterKind: SoundLibraryCategoryFilterKinds.All,
            SelectCommand: SelectCategoryCommand));
        Categories.Add(new CategoryPreviewModel(
            "Favorites",
            string.Empty,
            Symbol.Favorite,
            IsSelected: IsFavoritesOnly,
            Id: null,
            FilterKind: SoundLibraryCategoryFilterKinds.Favorites,
            SelectCommand: SelectCategoryCommand));

        foreach (var category in categories)
        {
            Categories.Add(new CategoryPreviewModel(
                category.Name,
                FormatCount(category.SoundCount),
                Symbol.Tag,
                IsSelected: !IsFavoritesOnly && selectedCategoryId == category.Id,
                Id: category.Id,
                FilterKind: SoundLibraryCategoryFilterKinds.Category,
                SelectCommand: SelectCategoryCommand));
        }

        NotifyStatePropertiesChanged();
    }

    private void UpdateManagedCategories(IReadOnlyList<SoundLibraryCategoryDto> categories)
    {
        ManagedCategories.Clear();
        foreach (var category in categories.OrderBy(category => category.SortOrder))
        {
            ManagedCategories.Add(new ManagedCategoryViewModel(
                category.Id,
                category.Name,
                FormatCount(category.SoundCount),
                EditManagedCategoryCommand,
                RequestCategoryDeletionCommand));
        }

        OnPropertyChanged(nameof(CategoryManagementItemsVisibility));
        OnPropertyChanged(nameof(CategoryManagementEmptyVisibility));
        OnPropertyChanged(nameof(CategoryDeletionTargetName));
    }

    private void UpdateManagedCategories(
        IReadOnlyList<CategoryDto> categories,
        IReadOnlyList<SoundDto> sounds)
    {
        var countsByCategoryId = sounds
            .SelectMany(sound => sound.CategoryIds.Distinct())
            .GroupBy(categoryId => categoryId)
            .ToDictionary(group => group.Key, group => group.Count());

        ManagedCategories.Clear();
        foreach (var category in categories.OrderBy(category => category.SortOrder))
        {
            ManagedCategories.Add(new ManagedCategoryViewModel(
                category.Id,
                category.Name,
                FormatCount(countsByCategoryId.GetValueOrDefault(category.Id)),
                EditManagedCategoryCommand,
                RequestCategoryDeletionCommand));
        }

        OnPropertyChanged(nameof(CategoryManagementItemsVisibility));
        OnPropertyChanged(nameof(CategoryManagementEmptyVisibility));
        OnPropertyChanged(nameof(CategoryDeletionTargetName));
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
        OnPropertyChanged(nameof(SoundHotkeyEditorVisibility));
        OnPropertyChanged(nameof(SelectedSoundHotkeyText));
        OnPropertyChanged(nameof(SelectedCategoryId));
        OnPropertyChanged(nameof(CategoryDeletionTargetName));
    }

    private async Task RefreshHotkeysAsync(CancellationToken cancellationToken)
    {
        var bindings = await listHotkeys.ExecuteAsync(cancellationToken);
        hotkeyBySoundId.Clear();
        foreach (var binding in bindings.Where(binding => binding.SoundId is not null))
        {
            hotkeyBySoundId[binding.SoundId!.Value] = binding;
        }
    }

    private void PopulateHotkeyEditor(Guid soundId)
    {
        if (!hotkeyBySoundId.TryGetValue(soundId, out var binding))
        {
            HotkeyCtrl = true;
            HotkeyAlt = false;
            HotkeyShift = false;
            HotkeyWin = false;
            HotkeyPrimaryKey = string.Empty;
            IsSelectedSoundHotkeyEnabled = true;
            return;
        }

        HotkeyCtrl = binding.Modifiers.HasFlag(HotkeyModifiers.Control);
        HotkeyAlt = binding.Modifiers.HasFlag(HotkeyModifiers.Alt);
        HotkeyShift = binding.Modifiers.HasFlag(HotkeyModifiers.Shift);
        HotkeyWin = binding.Modifiers.HasFlag(HotkeyModifiers.Windows);
        HotkeyPrimaryKey = binding.PrimaryKey;
        IsSelectedSoundHotkeyEnabled = binding.IsEnabled;
    }

    private HotkeyModifiers BuildSelectedModifiers()
    {
        var modifiers = HotkeyModifiers.None;
        if (HotkeyCtrl)
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (HotkeyAlt)
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (HotkeyShift)
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (HotkeyWin)
        {
            modifiers |= HotkeyModifiers.Windows;
        }

        return modifiers;
    }

    private void NotifyHotkeyPropertiesChanged()
    {
        OnPropertyChanged(nameof(SoundHotkeyEditorVisibility));
        OnPropertyChanged(nameof(SelectedSoundHotkeyText));
    }

    private static ToastPreviewModel ToastForRegistration(string title, HotkeyBindingDto binding)
    {
        var kind = binding.RegistrationState switch
        {
            HotkeyRegistrationState.Active => ToastNotificationKind.Success,
            HotkeyRegistrationState.Disabled => ToastNotificationKind.Info,
            HotkeyRegistrationState.Conflicting => ToastNotificationKind.Warning,
            _ => ToastNotificationKind.Error
        };

        return new ToastPreviewModel(kind, title, binding.RegistrationMessage);
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

    private async void OnDetailsSoundChanged(object? sender, EventArgs e)
    {
        await RefreshAsync(CancellationToken.None);
    }

    private async void OnLibraryChanged(object? sender, SoundLibraryChange e)
    {
        try
        {
            await RefreshAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            notifications?.Show(ToastNotificationKind.Error, "Biblioteca não atualizada", "Tente novamente.");
        }
    }

    private static WaveformBarViewModel[] ToWaveform(byte[] peaks) => peaks.Length == 32
        ? peaks.Select(peak => new WaveformBarViewModel(6 + peak / 255.0 * 28)).ToArray()
        : [];
}

public static class SoundLibraryCategoryFilterKinds
{
    public const string All = "All";
    public const string Favorites = "Favorites";
    public const string Category = "Category";
}

public sealed record SoundLibraryCategoryOptionViewModel(Guid? Id, string Name)
{
    public static SoundLibraryCategoryOptionViewModel Unassigned { get; } = new(null, "Unassigned");
}


public sealed record ImportFeedbackItemViewModel(string FileName, string Status, string Message);
