using EchoBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Tests;

internal sealed class TestEchoBoardDbContextFactory(DbContextOptions<EchoBoardDbContext> options)
    : IDbContextFactory<EchoBoardDbContext>
{
    public EchoBoardDbContext CreateDbContext() => new(options);

    public Task<EchoBoardDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateDbContext());
    }
}
