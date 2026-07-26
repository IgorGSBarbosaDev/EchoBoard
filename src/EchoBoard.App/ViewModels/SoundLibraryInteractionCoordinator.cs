using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Controls;
using EchoBoard.Application.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoBoard.App.ViewModels;

public sealed class SoundLibraryInteractionCoordinator
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly PlaybackCoordinator playback;
    private readonly TransientNotificationService notifications;
    private readonly ILogger<SoundLibraryInteractionCoordinator> logger;

    public SoundLibraryInteractionCoordinator(
        IServiceScopeFactory scopeFactory,
        PlaybackCoordinator playback,
        TransientNotificationService notifications,
        ILogger<SoundLibraryInteractionCoordinator> logger)
    {
        this.scopeFactory = scopeFactory;
        this.playback = playback;
        this.notifications = notifications;
        this.logger = logger;
        ToggleFavoriteCommand = new AsyncRelayCommand<Guid>(ToggleFavoriteAsync);
        DeleteSoundCommand = new AsyncRelayCommand<Guid>(DeleteAsync);
    }

    public event EventHandler<SoundLibraryChange>? LibraryChanged;

    public IAsyncRelayCommand<Guid> ToggleFavoriteCommand { get; }

    public IAsyncRelayCommand<Guid> DeleteSoundCommand { get; }

    private async Task ToggleFavoriteAsync(Guid soundId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var library = await scope.ServiceProvider
                .GetRequiredService<QuerySoundLibraryUseCase>()
                .ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
            var sound = library.Sounds.SingleOrDefault(item => item.Id == soundId);
            if (sound is null)
            {
                return;
            }

            var updated = await scope.ServiceProvider
                .GetRequiredService<SetSoundFavoriteUseCase>()
                .ExecuteAsync(new SetSoundFavoriteRequest(soundId, !sound.IsFavorite, DateTimeOffset.UtcNow), cancellationToken);
            notifications.Show(
                ToastNotificationKind.Success,
                updated.IsFavorite ? "Adicionado aos favoritos" : "Removido dos favoritos",
                string.Empty);
            LibraryChanged?.Invoke(this, new SoundLibraryChange(soundId, SoundLibraryChangeKind.Favorite));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Favorite state could not be changed for sound {SoundId}.", soundId);
            notifications.Show(ToastNotificationKind.Error, "Favorito não atualizado", "Tente novamente.");
        }
    }

    private async Task DeleteAsync(Guid soundId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var library = await scope.ServiceProvider
                .GetRequiredService<QuerySoundLibraryUseCase>()
                .ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
            var sound = library.Sounds.SingleOrDefault(item => item.Id == soundId);
            if (sound is null)
            {
                return;
            }

            await scope.ServiceProvider
                .GetRequiredService<DeleteSoundUseCase>()
                .ExecuteAsync(soundId, cancellationToken);
            try
            {
                await playback.StopSoundAsync(soundId, sound.FilePath, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Deleted sound {SoundId} could not be stopped cleanly.", soundId);
            }

            LibraryChanged?.Invoke(this, new SoundLibraryChange(soundId, SoundLibraryChangeKind.Deleted));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Sound {SoundId} could not be deleted.", soundId);
            notifications.Show(ToastNotificationKind.Error, "Áudio não excluído", "Tente novamente.");
        }
    }
}

public sealed record SoundLibraryChange(Guid SoundId, SoundLibraryChangeKind Kind);

public enum SoundLibraryChangeKind
{
    Favorite,
    Deleted,
    Updated
}
