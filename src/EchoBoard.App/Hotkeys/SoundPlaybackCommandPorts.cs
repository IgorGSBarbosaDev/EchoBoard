using System.Runtime.InteropServices;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Application.Library;
using EchoBoard.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EchoBoard.App.Hotkeys;

public sealed class SoundPlaybackCommandPorts : ISoundPlaybackCommandPort, IPlaybackControlCommandPort
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ISoundPlaybackEngine playback;
    private readonly PlaybackCoordinator playbackCoordinator;

    public SoundPlaybackCommandPorts(
        IServiceScopeFactory scopeFactory,
        ISoundPlaybackEngine playback,
        PlaybackCoordinator playbackCoordinator)
    {
        this.scopeFactory = scopeFactory;
        this.playback = playback;
        this.playbackCoordinator = playbackCoordinator;
    }

    public async Task<HotkeyCommandResult> PlaySoundAsync(Guid soundId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var playSound = scope.ServiceProvider.GetRequiredService<PlaySoundUseCase>();
            var result = await playSound.ExecuteAsync(new PlaySoundRequest(soundId, DateTimeOffset.UtcNow), cancellationToken);
            playbackCoordinator.NotifyPlaybackConfirmed(soundId, result);
            return HotkeyCommandResult.Success($"Playing {result.SoundName}.");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or COMException or NotSupportedException or ArgumentException or InvalidOperationException or SoundNotFoundException)
        {
            return HotkeyCommandResult.Failed("The audio file is corrupted, unsupported, or no playback device is available.");
        }
    }

    public async Task<HotkeyCommandResult> StopAllSoundsAsync(CancellationToken cancellationToken)
    {
        await playback.StopAllAsync(cancellationToken);
        playbackCoordinator.Refresh();
        return HotkeyCommandResult.Success("All sounds stopped.");
    }

    public async Task<HotkeyCommandResult> PauseResumePlaybackAsync(CancellationToken cancellationToken)
    {
        await playback.TogglePauseAsync(cancellationToken);
        playbackCoordinator.Refresh();
        return HotkeyCommandResult.Success("Playback state changed.");
    }
}
