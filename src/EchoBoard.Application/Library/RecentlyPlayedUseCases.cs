namespace EchoBoard.Application.Library;

public sealed record RecentlyPlayedSoundDto(SoundDto Sound, DateTimeOffset PlayedAt);

public sealed class ListRecentlyPlayedUseCase
{
    private readonly IRecentlyPlayedRepository history;
    private readonly ISoundLibraryRepository sounds;

    public ListRecentlyPlayedUseCase(IRecentlyPlayedRepository history, ISoundLibraryRepository sounds)
    {
        this.history = history;
        this.sounds = sounds;
    }

    public async Task<IReadOnlyList<RecentlyPlayedSoundDto>> ExecuteAsync(int limit, CancellationToken cancellationToken)
    {
        var entries = await history.ListAsync(Math.Max(limit * 4, limit), cancellationToken);
        var soundById = (await sounds.ListSoundsAsync(cancellationToken)).ToDictionary(sound => sound.Id);
        var seen = new HashSet<Guid>();
        var result = new List<RecentlyPlayedSoundDto>();

        foreach (var entry in entries)
        {
            if (!seen.Add(entry.SoundId) || !soundById.TryGetValue(entry.SoundId, out var sound))
            {
                continue;
            }

            result.Add(new RecentlyPlayedSoundDto(LibraryMapper.ToDto(sound), entry.PlayedAt));
            if (result.Count == limit)
            {
                break;
            }
        }

        return result;
    }
}
