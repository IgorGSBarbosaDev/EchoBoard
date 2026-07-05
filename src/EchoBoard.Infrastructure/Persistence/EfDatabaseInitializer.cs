using EchoBoard.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence;

public sealed class EfDatabaseInitializer : IDatabaseInitializer
{
    private readonly EchoBoardDbContext context;

    public EfDatabaseInitializer(EchoBoardDbContext context)
    {
        this.context = context;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await context.Database.MigrateAsync(cancellationToken);
    }
}
