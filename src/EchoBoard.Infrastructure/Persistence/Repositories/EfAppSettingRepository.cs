using EchoBoard.Application.Audio;
using EchoBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence.Repositories;

public sealed class EfAppSettingRepository : IAppSettingRepository
{
    private readonly IDbContextFactory<EchoBoardDbContext> contextFactory;

    public EfAppSettingRepository(IDbContextFactory<EchoBoardDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var setting = await context.AppSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Key == key, cancellationToken);

        return setting?.Value;
    }

    public async Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var setting = await context.AppSettings.SingleOrDefaultAsync(item => item.Key == key, cancellationToken);
        if (setting is null)
        {
            await context.AppSettings.AddAsync(AppSetting.Create(key, value, DateTimeOffset.UtcNow), cancellationToken);
        }
        else
        {
            setting.ChangeValue(value, DateTimeOffset.UtcNow);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
