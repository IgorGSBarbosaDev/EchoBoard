using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Controls;
using EchoBoard.App.Navigation;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Application.Library;
using EchoBoard.Domain.Enums;
using Microsoft.UI.Xaml;

namespace EchoBoard.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly QuerySoundLibraryUseCase queryLibrary;
    private readonly ImportSoundsUseCase importSounds;
    private readonly SetSoundFavoriteUseCase setSoundFavorite;
    private readonly GenerateSoundWaveformUseCase generateWaveform;
    private readonly ListHotkeyBindingsUseCase listHotkeys;
    private readonly GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot;
    private readonly PlaySoundUseCase playSound;
    private readonly SoundDetailsViewModel details;
    private readonly PlaybackCoordinator? playbackCoordinator;
    private readonly TransientNotificationService? notifications;
    private readonly GetAudioRoutingSnapshotUseCase? getAudioRoutingSnapshot;
    private readonly SoundLibraryInteractionCoordinator? libraryInteractions;
    private readonly Dictionary<Guid, SoundLibraryItemDto> soundById = [];
    private string microphoneNote = "Selecione uma entrada de áudio";
    private string feedbackMessage = string.Empty;
    private double microphoneLevel;
    private string microphoneLevelText = "Inativo";
    private double effectsLevel;
    private double virtualOutputLevel;
    private string effectsLevelText = "Indisponível";
    private string virtualOutputLevelText = "Indisponível";

    public DashboardViewModel(
        QuerySoundLibraryUseCase queryLibrary,
        ImportSoundsUseCase importSounds,
        SetSoundFavoriteUseCase setSoundFavorite,
        GenerateSoundWaveformUseCase generateWaveform,
        ListHotkeyBindingsUseCase listHotkeys,
        GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot,
        PlaySoundUseCase playSound,
        SoundDetailsViewModel details,
        INavigationService navigation,
        PlaybackCoordinator? playbackCoordinator = null,
        TransientNotificationService? notifications = null,
        GetAudioRoutingSnapshotUseCase? getAudioRoutingSnapshot = null,
        SoundLibraryInteractionCoordinator? libraryInteractions = null)
    {
        this.queryLibrary = queryLibrary;
        this.importSounds = importSounds;
        this.setSoundFavorite = setSoundFavorite;
        this.generateWaveform = generateWaveform;
        this.listHotkeys = listHotkeys;
        this.getMicrophoneSnapshot = getMicrophoneSnapshot;
        this.playSound = playSound;
        this.details = details;
        this.playbackCoordinator = playbackCoordinator;
        this.notifications = notifications;
        this.getAudioRoutingSnapshot = getAudioRoutingSnapshot;
        this.libraryInteractions = libraryInteractions;

        QuickSounds = [];
        OpenSettingsCommand = new RelayCommand(() => navigation.NavigateTo(ShellRoute.Settings));
        OpenLibraryCommand = new RelayCommand(() => navigation.NavigateTo(ShellRoute.Library));
        details.SoundChanged += OnSoundChanged;
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

    public string MicrophoneNote { get => microphoneNote; private set => SetProperty(ref microphoneNote, value); }

    public ObservableCollection<SoundCardPreviewModel> QuickSounds { get; }
    public IRelayCommand OpenSettingsCommand { get; }
    public IRelayCommand OpenLibraryCommand { get; }

    public Visibility QuickSoundsVisibility => QuickSounds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility QuickEmptyVisibility => QuickSounds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public double MicrophoneLevel
    {
        get => microphoneLevel;
        private set => SetProperty(ref microphoneLevel, Math.Clamp(value, 0, 1));
    }

    public string MicrophoneLevelText
    {
        get => microphoneLevelText;
        private set => SetProperty(ref microphoneLevelText, value);
    }

    public double EffectsLevel
    {
        get => effectsLevel;
        private set => SetProperty(ref effectsLevel, Math.Clamp(value, 0, 1));
    }

    public double VirtualOutputLevel
    {
        get => virtualOutputLevel;
        private set => SetProperty(ref virtualOutputLevel, Math.Clamp(value, 0, 1));
    }

    public string EffectsLevelText
    {
        get => effectsLevelText;
        private set => SetProperty(ref effectsLevelText, value);
    }

    public string VirtualOutputLevelText
    {
        get => virtualOutputLevelText;
        private set => SetProperty(ref virtualOutputLevelText, value);
    }

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

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var library = await queryLibrary.ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
        var waveformCandidates = library.Sounds
            .Where(sound => !sound.IsMissingFile && sound.WaveformPeaks.Length == 0)
            .OrderByDescending(sound => sound.IsFavorite)
            .ThenByDescending(sound => sound.PlayCount)
            .Take(4)
            .ToArray();
        foreach (var sound in waveformCandidates)
        {
            try
            {
                await generateWaveform.ExecuteAsync(sound.Id, cancellationToken);
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException or AudioFileUnreadableException or AudioFileMetadataException)
            {
                // The card keeps the explicit unavailable state when decoding fails.
            }
        }

        if (waveformCandidates.Length > 0)
        {
            library = await queryLibrary.ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
        }

        var hotkeys = await listHotkeys.ExecuteAsync(cancellationToken);
        var microphone = getMicrophoneSnapshot.Execute();

        ApplyMicrophone(microphone);
        ApplyRouting();
        ReplaceQuickSounds(library.Sounds, hotkeys);
    }

    public void RefreshLiveState()
    {
        ApplyMicrophone(getMicrophoneSnapshot.Execute());
        ApplyRouting();
    }

    public async Task ImportFilePathsAsync(IReadOnlyList<string> filePaths, CancellationToken cancellationToken)
    {
        if (filePaths.Count == 0)
        {
            return;
        }

        var result = await importSounds.ExecuteAsync(new ImportSoundsRequest(filePaths, DateTimeOffset.UtcNow), cancellationToken);
        var imported = result.Items.Count(item => item.Status == ImportSoundStatus.Imported);
        FeedbackMessage = imported == 0 ? "Nenhum som foi importado." : $"{imported} {(imported == 1 ? "som importado" : "sons importados")}.";
        await LoadAsync(cancellationToken);
    }

    private void ReplaceQuickSounds(IReadOnlyList<SoundLibraryItemDto> sounds, IReadOnlyList<HotkeyBindingDto> hotkeys)
    {
        soundById.Clear();
        foreach (var sound in sounds)
        {
            soundById[sound.Id] = sound;
            playbackCoordinator?.TrackSound(sound.Id, sound.FilePath);
        }

        var hotkeyBySoundId = hotkeys
            .Where(binding => binding.SoundId is not null)
            .ToDictionary(binding => binding.SoundId!.Value);
        QuickSounds.Clear();
        foreach (var sound in sounds
                     .OrderByDescending(sound => sound.IsFavorite)
                     .ThenByDescending(sound => sound.PlayCount)
                     .ThenBy(sound => sound.SortOrder)
                     .Take(4))
        {
            hotkeyBySoundId.TryGetValue(sound.Id, out var hotkey);
            QuickSounds.Add(ToCard(sound, hotkey));
        }

        OnPropertyChanged(nameof(QuickSoundsVisibility));
        OnPropertyChanged(nameof(QuickEmptyVisibility));
    }

    private SoundCardPreviewModel ToCard(SoundLibraryItemDto sound, HotkeyBindingDto? hotkey)
    {
        return new SoundCardPreviewModel(
            sound.Name,
            string.Empty,
            FormatDuration(sound.Duration),
            hotkey?.NormalizedKeyCombination ?? "Sem hotkey",
            sound.CategoryName ?? "Sem categoria",
            null,
            IsFavorite: sound.IsFavorite,
            Id: sound.Id,
            IsMissingFile: sound.IsMissingFile,
            StatusText: sound.IsMissingFile ? "Arquivo ausente" : string.Empty,
            SelectCommand: (System.Windows.Input.ICommand?)playbackCoordinator?.PlaySoundCommand
                ?? new AsyncRelayCommand(_ => PlayAsync(sound.Id, CancellationToken.None)),
            FavoriteCommand: (System.Windows.Input.ICommand?)libraryInteractions?.ToggleFavoriteCommand
                ?? new AsyncRelayCommand(_ => ToggleFavoriteAsync(sound.Id, sound.IsFavorite, CancellationToken.None)),
            FormatText: sound.Extension.TrimStart('.').ToUpperInvariant(),
            UsageText: $"{sound.PlayCount} {(sound.PlayCount == 1 ? "uso" : "usos")}",
            WaveformBars: ToWaveform(sound.WaveformPeaks),
            DetailsCommand: details.OpenCommand,
            EditCommand: details.OpenEditCommand,
            DeleteCommand: libraryInteractions?.DeleteSoundCommand);
    }

    private async Task PlayAsync(Guid soundId, CancellationToken cancellationToken)
    {
        try
        {
            await playSound.ExecuteAsync(new PlaySoundRequest(soundId, DateTimeOffset.UtcNow), cancellationToken);
            FeedbackMessage = string.Empty;
            await LoadAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            FeedbackMessage = exception.Message;
        }
    }

    private async Task ToggleFavoriteAsync(Guid soundId, bool isFavorite, CancellationToken cancellationToken)
    {
        await setSoundFavorite.ExecuteAsync(new SetSoundFavoriteRequest(soundId, !isFavorite, DateTimeOffset.UtcNow), cancellationToken);
        notifications?.Show(
            ToastNotificationKind.Success,
            !isFavorite ? "Adicionado aos favoritos" : "Removido dos favoritos",
            string.Empty);
        await LoadAsync(cancellationToken);
    }

    private void OnPlaybackSnapshotChanged(object? sender, PlaybackSnapshotChange e)
    {
        for (var index = 0; index < QuickSounds.Count; index++)
        {
            var card = QuickSounds[index];
            soundById.TryGetValue(card.Id, out var sound);
            var active = card.Id == e.SoundId && e.Snapshot.State != SoundPlaybackState.Stopped;
            card.IsPlaying = active && e.Snapshot.State == SoundPlaybackState.Playing;
            card.IsPaused = active && e.Snapshot.State == SoundPlaybackState.Paused;
            card.StatusText = card.IsMissingFile ? "Arquivo ausente" : string.Empty;
        }
    }

    private void OnPlaybackConfirmed(object? sender, Guid soundId)
    {
        if (!soundById.TryGetValue(soundId, out var sound))
        {
            return;
        }

        var updated = sound with { PlayCount = sound.PlayCount + 1 };
        soundById[soundId] = updated;
        for (var index = 0; index < QuickSounds.Count; index++)
        {
            if (QuickSounds[index].Id == soundId)
            {
                QuickSounds[index].UsageText = $"{updated.PlayCount} {(updated.PlayCount == 1 ? "uso" : "usos")}";
            }
        }
    }

    private async void OnLibraryChanged(object? sender, SoundLibraryChange e)
    {
        try
        {
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            notifications?.Show(ToastNotificationKind.Error, "Biblioteca não atualizada", "Tente novamente.");
        }
    }

    private void ApplyMicrophone(MicrophoneCaptureSnapshot snapshot)
    {
        MicrophoneNote = snapshot.SelectedDeviceName ?? "Selecione uma entrada de áudio";
        MicrophoneLevel = snapshot.State == MicrophoneCaptureState.Active && !snapshot.IsMuted ? snapshot.Level : 0;
        MicrophoneLevelText = snapshot.IsMuted ? "Mudo" : snapshot.State == MicrophoneCaptureState.Active ? $"{snapshot.Level:P0}" : "Inativo";
    }

    private void ApplyRouting()
    {
        if (getAudioRoutingSnapshot is null)
        {
            return;
        }

        var snapshot = getAudioRoutingSnapshot.Execute();
        EffectsLevel = snapshot.EffectsLevel;
        EffectsLevelText = snapshot.EffectsLevel > 0 ? $"{snapshot.EffectsLevel:P0}" : "Inativo";
        VirtualOutputLevel = snapshot.VirtualOutputLevel;
        VirtualOutputLevelText = snapshot.VirtualOutputState == AudioRouteState.Active
            ? snapshot.VirtualOutputLevel > 0 ? $"{snapshot.VirtualOutputLevel:P0}" : "Silenciosa"
            : "Indisponível";
    }

    private async void OnSoundChanged(object? sender, EventArgs e)
    {
        await LoadAsync(CancellationToken.None);
    }

    private static WaveformBarViewModel[] ToWaveform(byte[] peaks) => peaks.Length == 32
        ? peaks.Select(peak => new WaveformBarViewModel(6 + peak / 255.0 * 28)).ToArray()
        : [];

    private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1
        ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
        : $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
}
