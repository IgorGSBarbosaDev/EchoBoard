using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence.Repositories;

public sealed class EfRecentlyPlayedRepository : IRecentlyPlayedRepository
{
    private readonly EchoBoardDbContext context;

    public EfRecentlyPlayedRepository(EchoBoardDbContext context)
    {
        this.context = context;
    }

    public async Task AddAsync(RecentlyPlayed entry, CancellationToken cancellationToken)
    {
        context.RecentlyPlayed.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetPlayCountsAsync(CancellationToken cancellationToken)
    {
        return await context.RecentlyPlayed
            .AsNoTracking()
            .GroupBy(entry => entry.SoundId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);
    }

    public async Task<IReadOnlyList<RecentlyPlayed>> ListAsync(int limit, CancellationToken cancellationToken)
    {
        return await context.RecentlyPlayed
            .AsNoTracking()
            .OrderByDescending(entry => entry.PlayedAt)
            .Take(Math.Max(0, limit))
            .ToArrayAsync(cancellationToken);
    }
}
