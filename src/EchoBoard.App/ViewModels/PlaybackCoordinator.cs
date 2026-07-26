using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.Application.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoBoard.App.ViewModels;

public sealed class PlaybackCoordinator : ObservableObject
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ISoundPlaybackEngine playback;
    private readonly TransientNotificationService notifications;
    private readonly ILogger<PlaybackCoordinator> logger;
    private readonly ConcurrentDictionary<Guid, string> pathBySoundId = new();
    private SoundPlaybackSnapshot snapshot = SoundPlaybackSnapshot.Idle;
    private Guid? currentSoundId;
    private Guid? currentSessionId;
    private Guid? publishedSoundId;

    public PlaybackCoordinator(
        IServiceScopeFactory scopeFactory,
        ISoundPlaybackEngine playback,
        TransientNotificationService notifications,
        ILogger<PlaybackCoordinator> logger)
    {
        this.scopeFactory = scopeFactory;
        this.playback = playback;
        this.notifications = notifications;
        this.logger = logger;
        PlaySoundCommand = new AsyncRelayCommand<Guid>(PlayAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        TogglePauseCommand = new AsyncRelayCommand(TogglePauseAsync);
    }

    public event EventHandler<PlaybackSnapshotChange>? SnapshotChanged;

    public event EventHandler<Guid>? PlaybackConfirmed;

    public IAsyncRelayCommand<Guid> PlaySoundCommand { get; }

    public IAsyncRelayCommand StopCommand { get; }

    public IAsyncRelayCommand TogglePauseCommand { get; }

    public Guid? CurrentSoundId
    {
        get => currentSoundId;
        private set => SetProperty(ref currentSoundId, value);
    }

    public SoundPlaybackSnapshot Snapshot
    {
        get => snapshot;
        private set => SetProperty(ref snapshot, value);
    }

    public Guid? CurrentSessionId
    {
        get => currentSessionId;
        private set => SetProperty(ref currentSessionId, value);
    }

    public void TrackSound(Guid soundId, string filePath)
    {
        if (soundId != Guid.Empty && !string.IsNullOrWhiteSpace(filePath))
        {
            pathBySoundId[soundId] = filePath;
        }
    }

    public async Task PlayAsync(Guid soundId, CancellationToken cancellationToken)
    {
        if (soundId == Guid.Empty)
        {
            return;
        }

        try
        {
            var engineSnapshot = playback.GetSnapshot();
            if (CurrentSoundId == soundId && engineSnapshot.State is SoundPlaybackState.Playing or SoundPlaybackState.Paused)
            {
                await playback.TogglePauseAsync(cancellationToken);
                Refresh();
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var started = await scope.ServiceProvider.GetRequiredService<PlaySoundUseCase>().ExecuteAsync(
                new PlaySoundRequest(soundId, DateTimeOffset.UtcNow),
                cancellationToken);
            CurrentSoundId = soundId;
            CurrentSessionId = started.SessionId;
            Apply(started.Snapshot with { Position = TimeSpan.Zero });
            PlaybackConfirmed?.Invoke(this, soundId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
                                           or InvalidDataException
                                           or InvalidOperationException
                                           or ArgumentException
                                           or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Playback request failed for sound {SoundId}.", soundId);
            Refresh();
            notifications.Show(
                EchoBoard.App.Controls.ToastNotificationKind.Error,
                "Não foi possível reproduzir",
                PlaybackErrorMessage(exception));
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await playback.StopAllAsync(cancellationToken);
        CurrentSoundId = null;
        CurrentSessionId = null;
        Apply(SoundPlaybackSnapshot.Idle);
    }

    public async Task StopSoundAsync(Guid soundId, string filePath, CancellationToken cancellationToken)
    {
        await playback.StopSoundAsync(filePath, cancellationToken);
        pathBySoundId.TryRemove(soundId, out _);
        if (CurrentSoundId == soundId)
        {
            CurrentSoundId = null;
            CurrentSessionId = null;
            Apply(SoundPlaybackSnapshot.Idle);
        }
        else
        {
            Refresh();
        }
    }

    public async Task SeekAsync(TimeSpan position, CancellationToken cancellationToken)
    {
        await playback.SeekAsync(position, cancellationToken);
        Refresh();
    }

    public async Task TogglePauseAsync(CancellationToken cancellationToken)
    {
        await playback.TogglePauseAsync(cancellationToken);
        Refresh();
    }

    public void Refresh()
    {
        var current = playback.GetSnapshot();
        if (current.State == SoundPlaybackState.Stopped)
        {
            CurrentSoundId = null;
            CurrentSessionId = null;
            if (Snapshot.State != SoundPlaybackState.Stopped &&
                Snapshot.Duration > TimeSpan.Zero &&
                Snapshot.Position >= Snapshot.Duration - TimeSpan.FromMilliseconds(250))
            {
                Apply(Snapshot with
                {
                    Position = Snapshot.Duration,
                    IsPlaying = false,
                    IsPaused = false
                });
                return;
            }
        }
        else if (!string.IsNullOrWhiteSpace(current.FilePath))
        {
            CurrentSoundId = pathBySoundId.FirstOrDefault(pair =>
                string.Equals(pair.Value, current.FilePath, StringComparison.OrdinalIgnoreCase)).Key;
            if (CurrentSoundId == Guid.Empty)
            {
                CurrentSoundId = null;
            }
        }

        Apply(current);
    }

    public void NotifyPlaybackConfirmed(Guid soundId, SoundPlaybackSnapshot value)
    {
        CurrentSoundId = soundId;
        Apply(value);
        PlaybackConfirmed?.Invoke(this, soundId);
    }

    public void NotifyPlaybackConfirmed(Guid soundId, PlaySoundResult result)
    {
        CurrentSessionId = result.SessionId;
        NotifyPlaybackConfirmed(soundId, result.Snapshot with { Position = TimeSpan.Zero });
    }

    private void Apply(SoundPlaybackSnapshot value)
    {
        if (Snapshot == value && publishedSoundId == CurrentSoundId)
        {
            return;
        }

        Snapshot = value;
        publishedSoundId = CurrentSoundId;
        SnapshotChanged?.Invoke(this, new PlaybackSnapshotChange(CurrentSoundId, value));
    }

    private static string PlaybackErrorMessage(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => "O arquivo de áudio não foi encontrado.",
            UnauthorizedAccessException => "O arquivo de áudio não pode ser lido.",
            InvalidDataException => "O formato ou codec do áudio não pôde ser decodificado.",
            _ when exception.Message.Contains("device", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("route", StringComparison.OrdinalIgnoreCase)
                => "Nenhum dispositivo de saída está disponível.",
            _ => "Verifique o arquivo e o dispositivo de saída."
        };
    }
}

public sealed record PlaybackSnapshotChange(
    Guid? SoundId,
    SoundPlaybackSnapshot Snapshot);
