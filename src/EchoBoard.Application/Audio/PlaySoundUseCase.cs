using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;

namespace EchoBoard.Application.Audio;

public sealed record PlaySoundRequest(Guid SoundId, DateTimeOffset PlayedAt);

public sealed record PlaySoundResult(string SoundName, SoundPlaybackSnapshot Snapshot);

public sealed class PlaySoundUseCase
{
    private readonly ISoundLibraryRepository sounds;
    private readonly ISoundFileAvailabilityReader files;
    private readonly IRecentlyPlayedRepository recentlyPlayed;
    private readonly ISoundPlaybackEngine playback;

    public PlaySoundUseCase(
        ISoundLibraryRepository sounds,
        ISoundFileAvailabilityReader files,
        IRecentlyPlayedRepository recentlyPlayed,
        ISoundPlaybackEngine playback)
    {
        this.sounds = sounds;
        this.files = files;
        this.recentlyPlayed = recentlyPlayed;
        this.playback = playback;
    }

    public async Task<PlaySoundResult> ExecuteAsync(PlaySoundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.PlayedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("PlayedAt must be a UTC timestamp.", nameof(request));
        }

        var sound = await sounds.GetSoundAsync(request.SoundId, cancellationToken)
            ?? throw new SoundNotFoundException(request.SoundId);
        if (!await files.ExistsAsync(sound.FilePath, cancellationToken))
        {
            throw new FileNotFoundException("The audio file could not be found.", sound.FilePath);
        }

        if (sound.StopPreviousSound)
        {
            await playback.StopAllAsync(cancellationToken);
        }
        else if (!sound.AllowOverlap)
        {
            await playback.StopSoundAsync(sound.FilePath, cancellationToken);
        }

        await playback.PlayAsync(
            sound.FilePath,
            sound.Volume,
            new SoundPlaybackOptions(sound.IsLoopEnabled),
            cancellationToken);
        await recentlyPlayed.AddAsync(RecentlyPlayed.Create(sound.Id, request.PlayedAt), cancellationToken);

        return new PlaySoundResult(sound.Name, playback.GetSnapshot());
    }
}
