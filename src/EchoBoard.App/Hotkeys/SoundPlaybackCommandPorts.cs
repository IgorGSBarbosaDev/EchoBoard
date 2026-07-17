using System.Runtime.InteropServices;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Application.Library;
using Microsoft.Extensions.DependencyInjection;

namespace EchoBoard.App.Hotkeys;

public sealed class SoundPlaybackCommandPorts : ISoundPlaybackCommandPort, IPlaybackControlCommandPort
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ISoundPlaybackEngine playback;

    public SoundPlaybackCommandPorts(IServiceScopeFactory scopeFactory, ISoundPlaybackEngine playback)
    {
        this.scopeFactory = scopeFactory;
        this.playback = playback;
    }

    public async Task<HotkeyCommandResult> PlaySoundAsync(Guid soundId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sounds = scope.ServiceProvider.GetRequiredService<ISoundLibraryRepository>();
        var sound = await sounds.GetSoundAsync(soundId, cancellationToken);
        if (sound is null)
        {
            return HotkeyCommandResult.Failed("The selected sound no longer exists.");
        }

        if (!File.Exists(sound.FilePath))
        {
            return HotkeyCommandResult.Failed("The audio file could not be found.");
        }

        try
        {
            await playback.PlayAsync(sound.FilePath, sound.Volume, cancellationToken);
            return HotkeyCommandResult.Success($"Playing {sound.Name}.");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or COMException or NotSupportedException or ArgumentException or InvalidOperationException)
        {
            return HotkeyCommandResult.Failed("The audio file is corrupted, unsupported, or no playback device is available.");
        }
    }

    public async Task<HotkeyCommandResult> StopAllSoundsAsync(CancellationToken cancellationToken)
    {
        await playback.StopAllAsync(cancellationToken);
        return HotkeyCommandResult.Success("All sounds stopped.");
    }

    public async Task<HotkeyCommandResult> PauseResumePlaybackAsync(CancellationToken cancellationToken)
    {
        await playback.TogglePauseAsync(cancellationToken);
        return HotkeyCommandResult.Success("Playback state changed.");
    }
}
