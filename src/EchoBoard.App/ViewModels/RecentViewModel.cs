using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Library;
using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace EchoBoard.App.ViewModels;

public sealed class RecentViewModel : ObservableObject
{
    private readonly ListRecentlyPlayedUseCase listRecent;
    private readonly PlaySoundUseCase playSound;
    private readonly SoundDetailsViewModel details;
    private readonly PlaybackCoordinator? playbackCoordinator;
    private readonly SoundLibraryInteractionCoordinator? libraryInteractions;

    public RecentViewModel(
        ListRecentlyPlayedUseCase listRecent,
        PlaySoundUseCase playSound,
        SoundDetailsViewModel details,
        PlaybackCoordinator? playbackCoordinator = null,
        SoundLibraryInteractionCoordinator? libraryInteractions = null)
    {
        this.listRecent = listRecent;
        this.playSound = playSound;
        this.details = details;
        this.playbackCoordinator = playbackCoordinator;
        this.libraryInteractions = libraryInteractions;
        Sounds = [];
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

    public string Title => "Recentes";
    public string Subtitle => "Sons reproduzidos recentemente neste dispositivo.";
    public ObservableCollection<RecentSoundViewModel> Sounds { get; }
    public Visibility EmptyVisibility => Sounds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ListVisibility => Sounds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var items = await listRecent.ExecuteAsync(50, cancellationToken);
        Sounds.Clear();
        foreach (var item in items)
        {
            playbackCoordinator?.TrackSound(item.Sound.Id, item.Sound.FilePath);
            Sounds.Add(new RecentSoundViewModel(
                item.Sound.Id,
                item.Sound.Name,
                item.Sound.Extension.TrimStart('.').ToUpperInvariant(),
                item.PlayedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                (ICommand?)playbackCoordinator?.PlaySoundCommand
                    ?? new AsyncRelayCommand(_ => PlayAsync(item.Sound.Id, CancellationToken.None)),
                details.OpenCommand));
        }

        OnPropertyChanged(nameof(EmptyVisibility));
        OnPropertyChanged(nameof(ListVisibility));
    }

    private async Task PlayAsync(Guid soundId, CancellationToken cancellationToken)
    {
        await playSound.ExecuteAsync(new PlaySoundRequest(soundId, DateTimeOffset.UtcNow), cancellationToken);
        await LoadAsync(cancellationToken);
    }

    private async void OnSoundChanged(object? sender, EventArgs e)
    {
        await LoadAsync(CancellationToken.None);
    }

    private void OnPlaybackSnapshotChanged(object? sender, PlaybackSnapshotChange e)
    {
        for (var index = 0; index < Sounds.Count; index++)
        {
            var item = Sounds[index];
            var active = item.Id == e.SoundId && e.Snapshot.State != SoundPlaybackState.Stopped;
            Sounds[index] = item with
            {
                IsPlaying = active && e.Snapshot.State == SoundPlaybackState.Playing,
                IsPaused = active && e.Snapshot.State == SoundPlaybackState.Paused
            };
        }
    }

    private async void OnPlaybackConfirmed(object? sender, Guid soundId)
    {
        await LoadAsync(CancellationToken.None);
    }

    private async void OnLibraryChanged(object? sender, SoundLibraryChange e)
    {
        try
        {
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // The persisted action already completed; the next page load will refresh the list.
        }
    }
}

public sealed record RecentSoundViewModel(
    Guid Id,
    string Title,
    string Format,
    string PlayedAt,
    ICommand PlayCommand,
    IAsyncRelayCommand<Guid> DetailsCommand,
    bool IsPlaying = false,
    bool IsPaused = false)
{
    public string PlayGlyph => IsPlaying ? "\uE769" : "\uE768";
    public string PlaybackLabel => IsPlaying ? "Pausar" : IsPaused ? "Continuar" : "Reproduzir";
}
