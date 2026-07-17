using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Application.Library;
using EchoBoard.Domain.Enums;
using Microsoft.UI.Xaml;

namespace EchoBoard.App.ViewModels;

public sealed class SoundDetailsViewModel : ObservableObject
{
    private readonly QuerySoundLibraryUseCase queryLibrary;
    private readonly UpdateSoundUseCase updateSound;
    private readonly DeleteSoundUseCase deleteSound;
    private readonly ListHotkeyBindingsUseCase listHotkeys;
    private readonly AssignSoundHotkeyUseCase assignSoundHotkey;
    private readonly RemoveHotkeyBindingUseCase removeHotkey;
    private readonly PlaySoundUseCase playSound;
    private SoundLibraryItemDto? selectedSound;
    private HotkeyBindingDto? selectedHotkey;
    private bool isOpen;
    private bool isEditing;
    private string name = string.Empty;
    private double volumePercent = 100;
    private bool isLoopEnabled;
    private bool stopPreviousSound = true;
    private bool allowOverlap;
    private bool isFavorite;
    private SoundLibraryCategoryOptionViewModel? selectedCategory;
    private string hotkeyPrimaryKey = string.Empty;
    private bool hotkeyCtrl = true;
    private bool hotkeyAlt;
    private bool hotkeyShift;
    private bool hotkeyWin;
    private string feedbackMessage = string.Empty;

    public SoundDetailsViewModel(
        QuerySoundLibraryUseCase queryLibrary,
        UpdateSoundUseCase updateSound,
        DeleteSoundUseCase deleteSound,
        ListHotkeyBindingsUseCase listHotkeys,
        AssignSoundHotkeyUseCase assignSoundHotkey,
        RemoveHotkeyBindingUseCase removeHotkey,
        PlaySoundUseCase playSound)
    {
        this.queryLibrary = queryLibrary;
        this.updateSound = updateSound;
        this.deleteSound = deleteSound;
        this.listHotkeys = listHotkeys;
        this.assignSoundHotkey = assignSoundHotkey;
        this.removeHotkey = removeHotkey;
        this.playSound = playSound;

        Categories = [];
        OpenCommand = new AsyncRelayCommand<Guid>(id => OpenAsync(id, edit: false, CancellationToken.None));
        OpenEditCommand = new AsyncRelayCommand<Guid>(id => OpenAsync(id, edit: true, CancellationToken.None));
        CloseCommand = new RelayCommand(Close);
        BeginEditCommand = new RelayCommand(() => IsEditing = true);
        CancelEditCommand = new RelayCommand(CancelEdit);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        PlayCommand = new AsyncRelayCommand(PlayAsync, () => selectedSound is { IsMissingFile: false });
    }

    public event EventHandler? SoundChanged;

    public ObservableCollection<SoundLibraryCategoryOptionViewModel> Categories { get; }

    public IAsyncRelayCommand<Guid> OpenCommand { get; }
    public IAsyncRelayCommand<Guid> OpenEditCommand { get; }
    public IRelayCommand CloseCommand { get; }
    public IRelayCommand BeginEditCommand { get; }
    public IRelayCommand CancelEditCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand PlayCommand { get; }

    public bool IsOpen
    {
        get => isOpen;
        private set
        {
            if (SetProperty(ref isOpen, value))
            {
                OnPropertyChanged(nameof(DrawerVisibility));
            }
        }
    }

    public Visibility DrawerVisibility => IsOpen ? Visibility.Visible : Visibility.Collapsed;

    public bool HasSelection => selectedSound is not null;

    public bool IsEditing
    {
        get => isEditing;
        private set
        {
            if (SetProperty(ref isEditing, value))
            {
                OnPropertyChanged(nameof(ReadOnlyVisibility));
                OnPropertyChanged(nameof(EditVisibility));
            }
        }
    }

    public Visibility ReadOnlyVisibility => IsEditing ? Visibility.Collapsed : Visibility.Visible;
    public Visibility EditVisibility => IsEditing ? Visibility.Visible : Visibility.Collapsed;

    public string Title => selectedSound?.Name ?? "Nenhum som selecionado";
    public string Metadata => selectedSound is null ? string.Empty : $"{CategoryText} · {FormatText} · {DurationText}";
    public string CategoryText => selectedSound?.CategoryName ?? "Sem categoria";
    public string FormatText => selectedSound?.Extension.TrimStart('.').ToUpperInvariant() ?? string.Empty;
    public string DurationText => selectedSound is null ? string.Empty : FormatDuration(selectedSound.Duration);
    public string FilePath => selectedSound?.FilePath ?? string.Empty;
    public string FileSizeText => selectedSound is null ? string.Empty : FormatFileSize(selectedSound.FileSize);
    public string AvailabilityText => selectedSound is null ? string.Empty : selectedSound.IsMissingFile ? "Arquivo ausente" : "Disponível";
    public string HotkeyText => selectedHotkey?.NormalizedKeyCombination ?? "Não configurada";
    public string HotkeyStatusText => selectedHotkey?.RegistrationState.ToString() ?? "Sem registro";
    public string VolumeText => $"{VolumePercent:0}%";
    public string LoopText => IsLoopEnabled ? "Ativado" : "Desativado";
    public string StopPreviousText => StopPreviousSound ? "Ativado" : "Desativado";
    public string AllowOverlapText => AllowOverlap ? "Ativada" : "Desativada";
    public string FavoriteText => IsFavorite ? "Sim" : "Não";

    public string Name { get => name; set => SetProperty(ref name, value); }
    public double VolumePercent { get => volumePercent; set { if (SetProperty(ref volumePercent, Math.Clamp(value, 0, 100))) OnPropertyChanged(nameof(VolumeText)); } }
    public bool IsLoopEnabled { get => isLoopEnabled; set { if (SetProperty(ref isLoopEnabled, value)) OnPropertyChanged(nameof(LoopText)); } }
    public bool StopPreviousSound { get => stopPreviousSound; set { if (SetProperty(ref stopPreviousSound, value)) OnPropertyChanged(nameof(StopPreviousText)); } }
    public bool AllowOverlap { get => allowOverlap; set { if (SetProperty(ref allowOverlap, value)) OnPropertyChanged(nameof(AllowOverlapText)); } }
    public bool IsFavorite { get => isFavorite; set { if (SetProperty(ref isFavorite, value)) OnPropertyChanged(nameof(FavoriteText)); } }
    public SoundLibraryCategoryOptionViewModel? SelectedCategory { get => selectedCategory; set => SetProperty(ref selectedCategory, value); }
    public string HotkeyPrimaryKey { get => hotkeyPrimaryKey; set => SetProperty(ref hotkeyPrimaryKey, value); }
    public bool HotkeyCtrl { get => hotkeyCtrl; set => SetProperty(ref hotkeyCtrl, value); }
    public bool HotkeyAlt { get => hotkeyAlt; set => SetProperty(ref hotkeyAlt, value); }
    public bool HotkeyShift { get => hotkeyShift; set => SetProperty(ref hotkeyShift, value); }
    public bool HotkeyWin { get => hotkeyWin; set => SetProperty(ref hotkeyWin, value); }

    public string FeedbackMessage
    {
        get => feedbackMessage;
        private set
        {
            if (SetProperty(ref feedbackMessage, value))
            {
                OnPropertyChanged(nameof(FeedbackVisibility));
            }
        }
    }

    public Visibility FeedbackVisibility => string.IsNullOrWhiteSpace(FeedbackMessage) ? Visibility.Collapsed : Visibility.Visible;

    public async Task OpenAsync(Guid soundId, bool edit, CancellationToken cancellationToken)
    {
        var library = await queryLibrary.ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
        selectedSound = library.Sounds.SingleOrDefault(sound => sound.Id == soundId);
        if (selectedSound is null)
        {
            return;
        }

        var bindings = await listHotkeys.ExecuteAsync(cancellationToken);
        selectedHotkey = bindings.SingleOrDefault(binding => binding.SoundId == soundId);
        Categories.Clear();
        Categories.Add(SoundLibraryCategoryOptionViewModel.Unassigned);
        foreach (var category in library.Categories.OrderBy(category => category.SortOrder))
        {
            Categories.Add(new SoundLibraryCategoryOptionViewModel(category.Id, category.Name));
        }

        PopulateEditor();
        FeedbackMessage = string.Empty;
        IsEditing = edit;
        IsOpen = true;
        NotifySelectionChanged();
        PlayCommand.NotifyCanExecuteChanged();
    }

    public void Toggle()
    {
        if (!HasSelection)
        {
            return;
        }

        IsOpen = !IsOpen;
    }

    public void Close()
    {
        IsOpen = false;
        IsEditing = false;
        FeedbackMessage = string.Empty;
    }

    public async Task DeleteSelectedAsync(CancellationToken cancellationToken)
    {
        if (selectedSound is null)
        {
            return;
        }

        if (selectedHotkey is not null)
        {
            await removeHotkey.ExecuteAsync(selectedHotkey.Id, cancellationToken);
        }

        await deleteSound.ExecuteAsync(selectedSound.Id, cancellationToken);
        selectedSound = null;
        selectedHotkey = null;
        Close();
        NotifySelectionChanged();
        SoundChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (selectedSound is null)
        {
            return;
        }

        try
        {
            await updateSound.ExecuteAsync(new UpdateSoundRequest(
                selectedSound.Id,
                Name,
                selectedSound.FilePath,
                selectedSound.Extension,
                selectedSound.Duration,
                selectedSound.FileSize,
                VolumePercent / 100.0,
                IsFavorite,
                SelectedCategory?.Id,
                selectedSound.SortOrder,
                DateTimeOffset.UtcNow,
                IsLoopEnabled,
                StopPreviousSound,
                AllowOverlap,
                selectedSound.WaveformPeaks), cancellationToken);

            if (string.IsNullOrWhiteSpace(HotkeyPrimaryKey))
            {
                if (selectedHotkey is not null)
                {
                    await removeHotkey.ExecuteAsync(selectedHotkey.Id, cancellationToken);
                }
            }
            else
            {
                await assignSoundHotkey.ExecuteAsync(new AssignSoundHotkeyRequest(
                    selectedSound.Id,
                    BuildModifiers(),
                    HotkeyPrimaryKey,
                    IsEnabled: true,
                    DateTimeOffset.UtcNow), cancellationToken);
            }

            var id = selectedSound.Id;
            await OpenAsync(id, edit: false, cancellationToken);
            FeedbackMessage = "Alterações salvas.";
            SoundChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            FeedbackMessage = exception.Message;
        }
    }

    private async Task PlayAsync(CancellationToken cancellationToken)
    {
        if (selectedSound is null)
        {
            return;
        }

        try
        {
            await playSound.ExecuteAsync(new PlaySoundRequest(selectedSound.Id, DateTimeOffset.UtcNow), cancellationToken);
            FeedbackMessage = "Reprodução iniciada.";
            SoundChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            FeedbackMessage = exception.Message;
        }
    }

    private void CancelEdit()
    {
        PopulateEditor();
        IsEditing = false;
        FeedbackMessage = string.Empty;
    }

    private void PopulateEditor()
    {
        if (selectedSound is null)
        {
            return;
        }

        Name = selectedSound.Name;
        VolumePercent = selectedSound.Volume * 100.0;
        IsLoopEnabled = selectedSound.IsLoopEnabled;
        StopPreviousSound = selectedSound.StopPreviousSound;
        AllowOverlap = selectedSound.AllowOverlap;
        IsFavorite = selectedSound.IsFavorite;
        SelectedCategory = Categories.SingleOrDefault(category => category.Id == selectedSound.CategoryId) ?? Categories.FirstOrDefault();
        HotkeyPrimaryKey = selectedHotkey?.PrimaryKey ?? string.Empty;
        HotkeyCtrl = selectedHotkey?.Modifiers.HasFlag(HotkeyModifiers.Control) ?? true;
        HotkeyAlt = selectedHotkey?.Modifiers.HasFlag(HotkeyModifiers.Alt) ?? false;
        HotkeyShift = selectedHotkey?.Modifiers.HasFlag(HotkeyModifiers.Shift) ?? false;
        HotkeyWin = selectedHotkey?.Modifiers.HasFlag(HotkeyModifiers.Windows) ?? false;
    }

    private HotkeyModifiers BuildModifiers()
    {
        var value = HotkeyModifiers.None;
        if (HotkeyCtrl) value |= HotkeyModifiers.Control;
        if (HotkeyAlt) value |= HotkeyModifiers.Alt;
        if (HotkeyShift) value |= HotkeyModifiers.Shift;
        if (HotkeyWin) value |= HotkeyModifiers.Windows;
        return value;
    }

    private void NotifySelectionChanged()
    {
        foreach (var property in new[]
        {
            nameof(HasSelection), nameof(Title), nameof(Metadata), nameof(CategoryText), nameof(FormatText),
            nameof(DurationText), nameof(FilePath), nameof(FileSizeText), nameof(AvailabilityText),
            nameof(HotkeyText), nameof(HotkeyStatusText)
        })
        {
            OnPropertyChanged(property);
        }
    }

    private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1
        ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
        : $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";

    private static string FormatFileSize(long size)
    {
        const double kb = 1024;
        const double mb = kb * 1024;
        return size >= mb ? $"{size / mb:0.#} MB" : $"{Math.Max(size / kb, 0.1):0.#} KB";
    }
}
