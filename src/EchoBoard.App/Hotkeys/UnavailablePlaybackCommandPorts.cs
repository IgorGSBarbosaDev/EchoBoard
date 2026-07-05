using EchoBoard.Application.Hotkeys;

namespace EchoBoard.App.Hotkeys;

public sealed class UnavailablePlaybackCommandPorts : ISoundPlaybackCommandPort, IPlaybackControlCommandPort
{
    public Task<HotkeyCommandResult> PlaySoundAsync(Guid soundId, CancellationToken cancellationToken)
    {
        return Task.FromResult(HotkeyCommandResult.Unavailable("Playback is not available in this build step yet."));
    }

    public Task<HotkeyCommandResult> StopAllSoundsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(HotkeyCommandResult.Unavailable("Stop all sounds will activate when playback is implemented."));
    }

    public Task<HotkeyCommandResult> PauseResumePlaybackAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(HotkeyCommandResult.Unavailable("Pause/resume will activate when playback is implemented."));
    }
}
