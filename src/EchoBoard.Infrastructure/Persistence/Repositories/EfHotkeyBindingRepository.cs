using EchoBoard.Application.Hotkeys;
using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence.Repositories;

public sealed class EfHotkeyBindingRepository : IHotkeyBindingRepository
{
    private readonly EchoBoardDbContext context;

    public EfHotkeyBindingRepository(EchoBoardDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<HotkeyBinding>> ListAsync(CancellationToken cancellationToken)
    {
        return await context.HotkeyBindings
            .AsNoTracking()
            .OrderBy(binding => binding.TargetKind)
            .ThenBy(binding => binding.NormalizedKeyCombination)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<HotkeyBinding?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.HotkeyBindings
            .AsNoTracking()
            .SingleOrDefaultAsync(binding => binding.Id == id, cancellationToken);
    }

    public async Task<HotkeyBinding?> GetForSoundAsync(Guid soundId, CancellationToken cancellationToken)
    {
        return await context.HotkeyBindings
            .AsNoTracking()
            .SingleOrDefaultAsync(binding => binding.SoundId == soundId, cancellationToken);
    }

    public async Task<HotkeyBinding?> GetForGlobalCommandAsync(GlobalHotkeyCommand command, CancellationToken cancellationToken)
    {
        return await context.HotkeyBindings
            .AsNoTracking()
            .SingleOrDefaultAsync(binding => binding.GlobalCommand == command, cancellationToken);
    }

    public async Task<bool> CombinationExistsAsync(string normalizedKeyCombination, Guid? excludingBindingId, CancellationToken cancellationToken)
    {
        return await context.HotkeyBindings
            .AsNoTracking()
            .AnyAsync(
                binding => binding.Id != excludingBindingId && binding.NormalizedKeyCombination == normalizedKeyCombination,
                cancellationToken);
    }

    public async Task AddAsync(HotkeyBinding binding, CancellationToken cancellationToken)
    {
        context.HotkeyBindings.Add(binding);
        await SaveChangesAsync(binding.NormalizedKeyCombination, cancellationToken);
    }

    public async Task UpdateAsync(HotkeyBinding binding, CancellationToken cancellationToken)
    {
        context.HotkeyBindings.Update(binding);
        await SaveChangesAsync(binding.NormalizedKeyCombination, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var binding = await context.HotkeyBindings.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (binding is null)
        {
            return;
        }

        context.HotkeyBindings.Remove(binding);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveChangesAsync(string normalizedKeyCombination, CancellationToken cancellationToken)
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
