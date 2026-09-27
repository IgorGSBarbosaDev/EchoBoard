using EchoBoard.Application.Hotkeys;
using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence.Repositories;

public sealed class EfHotkeyBindingRepository : IHotkeyBindingRepository
{
    private readonly IDbContextFactory<EchoBoardDbContext> contextFactory;

    public EfHotkeyBindingRepository(IDbContextFactory<EchoBoardDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<HotkeyBinding>> ListAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotkeyBindings
            .AsNoTracking()
            .Where(binding => binding.TargetKind == HotkeyBindingTargetKind.Sound && binding.SoundId != null)
            .OrderBy(binding => binding.NormalizedKeyCombination)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<HotkeyBinding?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotkeyBindings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                binding => binding.Id == id && binding.TargetKind == HotkeyBindingTargetKind.Sound && binding.SoundId != null,
                cancellationToken);
    }

    public async Task<HotkeyBinding?> GetForSoundAsync(Guid soundId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotkeyBindings
            .AsNoTracking()
            .SingleOrDefaultAsync(binding => binding.SoundId == soundId, cancellationToken);
    }

    public async Task<bool> CombinationExistsAsync(string normalizedKeyCombination, Guid? excludingBindingId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotkeyBindings
            .AsNoTracking()
            .AnyAsync(
                binding => binding.Id != excludingBindingId && binding.TargetKind == HotkeyBindingTargetKind.Sound && binding.SoundId != null && binding.NormalizedKeyCombination == normalizedKeyCombination,
                cancellationToken);
    }

    public async Task AddAsync(HotkeyBinding binding, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.HotkeyBindings.Add(binding);
        await SaveChangesAsync(context, binding.NormalizedKeyCombination, cancellationToken);
    }

    public async Task UpdateAsync(HotkeyBinding binding, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.HotkeyBindings.Update(binding);
        await SaveChangesAsync(context, binding.NormalizedKeyCombination, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var binding = await context.HotkeyBindings.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (binding is null)
        {
            return;
        }

        context.HotkeyBindings.Remove(binding);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SaveChangesAsync(
        EchoBoardDbContext context,
        string normalizedKeyCombination,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateHotkeyBindingException(normalizedKeyCombination);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException { SqliteErrorCode: 19 };
    }
}
