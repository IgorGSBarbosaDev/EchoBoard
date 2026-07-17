using EchoBoard.Domain.Exceptions;

namespace EchoBoard.Domain.Entities;

public sealed class RecentlyPlayed
{
    private RecentlyPlayed()
    {
    }

    private RecentlyPlayed(Guid id, Guid soundId, DateTimeOffset playedAt)
    {
        Id = id;
        SoundId = soundId;
        PlayedAt = playedAt;
    }

    public Guid Id { get; private set; }

    public Guid SoundId { get; private set; }

    public DateTimeOffset PlayedAt { get; private set; }

    public static RecentlyPlayed Create(Guid soundId, DateTimeOffset playedAt)
    {
        if (soundId == Guid.Empty)
        {
            throw new DomainValidationException("SoundId is required.");
        }

        if (playedAt.Offset != TimeSpan.Zero)
        {
            throw new DomainValidationException("PlayedAt must be a UTC timestamp.");
        }

        return new RecentlyPlayed(Guid.NewGuid(), soundId, playedAt);
    }
}
