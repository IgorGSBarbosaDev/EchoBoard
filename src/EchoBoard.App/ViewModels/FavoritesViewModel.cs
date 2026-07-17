using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Controls;
using EchoBoard.Application.Library;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using Microsoft.UI.Xaml;

namespace EchoBoard.App.ViewModels;

public sealed partial class FavoritesViewModel : ObservableObject
{
    private readonly QuerySoundLibraryUseCase queryLibrary;
    private readonly SetSoundFavoriteUseCase setSoundFavorite;
    private readonly ListHotkeyBindingsUseCase? listHotkeys;
    private readonly PlaySoundUseCase? playSound;
    private readonly SoundDetailsViewModel? details;
    private readonly Dictionary<Guid, HotkeyBindingDto> hotkeyBySoundId = [];
    private bool isBusy;
    private string searchText = string.Empty;
    private string? loadError;
    private ToastPreviewModel? feedbackToast;

    public FavoritesViewModel(
        QuerySoundLibraryUseCase queryLibrary,
        SetSoundFavoriteUseCase setSoundFavorite,
        ListHotkeyBindingsUseCase? listHotkeys = null,
        PlaySoundUseCase? playSound = null,
        SoundDetailsViewModel? details = null)
    {
        this.queryLibrary = queryLibrary;
        this.setSoundFavorite = setSoundFavorite;
        this.listHotkeys = listHotkeys;
        this.playSound = playSound;
        this.details = details;

        Sounds = [];
        ClearFiltersCommand = new AsyncRelayCommand(ct => ClearFiltersAsync(ct));
        if (details is not null)
        {
            details.SoundChanged += OnDetailsSoundChanged;
        }
    }

    public string Title => "Favorites";

    public string Subtitle => "Fast access area for frequently used sounds.";

    public string EmptyStateTitle
    {
        get
        {
            if (IsBusy)
            {
                return "Loading favorites";
            }

            if (loadError is not null)
            {
                return "Favorites unavailable";
            }

            return string.IsNullOrWhiteSpace(SearchText) ? "No favorites yet" : "No results";
        }
    }

    public string EmptyStateMessage
    {
        get
        {
            if (IsBusy)
            {
                return "Loading favorite sounds from the local library.";
            }

            if (loadError is not null)
            {
                return loadError;
            }

            return string.IsNullOrWhiteSpace(SearchText)
                ? "Sounds marked as favorites will be collected here."
                : "Try clearing the search filter.";
        }
    }

    public ObservableCollection<SoundCardPreviewModel> Sounds { get; }

    public ToastPreviewModel? FeedbackToast
    {
        get => feedbackToast;
        private set
        {
            if (SetProperty(ref feedbackToast, value))
            {
                OnPropertyChanged(nameof(FeedbackToastVisibility));
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

    public Visibility EmptyStateVisibility => IsBusy || loadError is not null || Sounds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SoundGridVisibility => !IsBusy && loadError is null && Sounds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ClearFiltersVisibility => string.IsNullOrWhiteSpace(SearchText) ? Visibility.Collapsed : Visibility.Visible;

    public Visibility FeedbackToastVisibility => FeedbackToast is null ? Visibility.Collapsed : Visibility.Visible;

    public IAsyncRelayCommand ClearFiltersCommand { get; }

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

    public async Task ToggleFavoriteAsync(Guid soundId, CancellationToken cancellationToken)
    {
        var sound = Sounds.SingleOrDefault(item => item.Id == soundId);
        if (sound is null)
        {
            return;
        }

        await setSoundFavorite.ExecuteAsync(new SetSoundFavoriteRequest(soundId, false, DateTimeOffset.UtcNow), cancellationToken);
        FeedbackToast = new ToastPreviewModel(ToastNotificationKind.Success, "Removed from favorites", sound.Title);
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
                new SoundLibraryFilter(SearchText, CategoryId: null, IncludeUncategorizedOnly: false, FavoritesOnly: true),
                cancellationToken);
            ReplaceSounds(result.Sounds);
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
        OnPropertyChanged(nameof(SearchText));
        await RefreshAsync(cancellationToken);
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
                hotkeyBySoundId.TryGetValue(sound.Id, out var binding) ? binding.NormalizedKeyCombination : "No hotkey",
                sound.CategoryName ?? "Uncategorized",
                null,
                IsFavorite: sound.IsFavorite,
                Id: sound.Id,
                IsMissingFile: sound.IsMissingFile,
                StatusText: sound.IsMissingFile ? "File missing" : string.Empty,
                SelectCommand: playSound is null ? null : new AsyncRelayCommand(_ => PlayAsync(sound.Id, CancellationToken.None)),
                FavoriteCommand: new AsyncRelayCommand(_ => ToggleFavoriteAsync(sound.Id, CancellationToken.None)),
                FormatText: sound.Extension.TrimStart('.').ToUpperInvariant(),
                UsageText: $"{sound.PlayCount} {(sound.PlayCount == 1 ? "uso" : "usos")}",
                WaveformBars: ToWaveform(sound.WaveformPeaks),
                DetailsCommand: details?.OpenCommand,
                EditCommand: details?.OpenEditCommand));
        }

        NotifyStatePropertiesChanged();
    }

    private void NotifyStatePropertiesChanged()
    {
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateMessage));
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(SoundGridVisibility));
        OnPropertyChanged(nameof(ClearFiltersVisibility));
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

    private async Task PlayAsync(Guid soundId, CancellationToken cancellationToken)
    {
        if (playSound is null)
        {
            return;
        }

        try
        {
            await playSound.ExecuteAsync(new PlaySoundRequest(soundId, DateTimeOffset.UtcNow), cancellationToken);
            FeedbackToast = new ToastPreviewModel(ToastNotificationKind.Success, "Playback started", "The selected sound is playing.");
            await RefreshAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            FeedbackToast = new ToastPreviewModel(ToastNotificationKind.Error, "Playback failed", exception.Message);
        }
    }

    private async Task RefreshHotkeysAsync(CancellationToken cancellationToken)
    {
        hotkeyBySoundId.Clear();
        if (listHotkeys is null)
        {
            return;
        }

        var bindings = await listHotkeys.ExecuteAsync(cancellationToken);
        foreach (var binding in bindings.Where(binding => binding.SoundId is not null))
        {
            hotkeyBySoundId[binding.SoundId!.Value] = binding;
        }
    }

    private async void OnDetailsSoundChanged(object? sender, EventArgs e)
    {
        await RefreshAsync(CancellationToken.None);
    }

    private static WaveformBarViewModel[] ToWaveform(byte[] peaks) => peaks.Length == 32
        ? peaks.Select(peak => new WaveformBarViewModel(6 + peak / 255.0 * 28)).ToArray()
        : [];
}
