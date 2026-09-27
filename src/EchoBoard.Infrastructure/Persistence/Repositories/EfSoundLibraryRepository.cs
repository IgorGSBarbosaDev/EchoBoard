using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence.Repositories;

public sealed class EfSoundLibraryRepository : ISoundLibraryRepository
{
    private readonly IDbContextFactory<EchoBoardDbContext> contextFactory;

    public EfSoundLibraryRepository(IDbContextFactory<EchoBoardDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Sound>> ListSoundsAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sounds
            .AsNoTracking()
            .Include(sound => sound.CategoryAssignments)
            .OrderBy(sound => sound.SortOrder)
            .ThenBy(sound => sound.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sound>> GetSoundsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return [];
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var soundIds = ids.Distinct().ToArray();
        return await context.Sounds
            .AsNoTracking()
            .Include(sound => sound.CategoryAssignments)
            .Where(sound => soundIds.Contains(sound.Id))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sound>> GetSoundsByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sounds
            .AsNoTracking()
            .Include(sound => sound.CategoryAssignments)
            .Where(sound => sound.CategoryAssignments.Any(assignment => assignment.CategoryId == categoryId))
            .OrderBy(sound => sound.SortOrder)
            .ThenBy(sound => sound.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Sound?> GetSoundAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Sounds
            .AsNoTracking()
            .Include(sound => sound.CategoryAssignments)
            .SingleOrDefaultAsync(sound => sound.Id == id, cancellationToken);
    }

    public async Task<bool> SoundFilePathExistsAsync(string filePath, Guid? excludingSoundId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedPath = PathNormalizer.NormalizeFilePath(filePath);

        return await context.Sounds
            .AsNoTracking()
            .AnyAsync(
                sound => sound.Id != excludingSoundId && sound.FilePath == normalizedPath,
                cancellationToken);
    }

    public async Task AddSoundAsync(Sound sound, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Sounds.Add(sound);
        await SaveChangesAsync(context, sound.FilePath, cancellationToken);
    }

    public async Task UpdateSoundAsync(Sound sound, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Sounds.Update(sound);
        await SaveChangesAsync(context, sound.FilePath, cancellationToken);
    }

    public async Task UpdateSoundsAsync(IReadOnlyList<Sound> sounds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sounds);
        if (sounds.Count == 0)
        {
            return;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var requestedById = sounds.ToDictionary(sound => sound.Id);
        var currentSounds = await context.Sounds
            .Include(sound => sound.CategoryAssignments)
            .Where(sound => requestedById.Keys.Contains(sound.Id))
            .ToArrayAsync(cancellationToken);
        var currentById = currentSounds.ToDictionary(sound => sound.Id);

        foreach (var requested in sounds)
        {
            if (!currentById.TryGetValue(requested.Id, out var current))
            {
                context.Sounds.Add(requested);
                continue;
            }

            context.Entry(current).CurrentValues.SetValues(requested);
            var requestedCategoryIds = requested.CategoryIds.ToHashSet();
            var currentAssignments = current.CategoryAssignments.ToDictionary(assignment => assignment.CategoryId);
            foreach (var assignment in currentAssignments.Values.Where(assignment => !requestedCategoryIds.Contains(assignment.CategoryId)))
            {
                current.RemoveFromCategory(assignment.CategoryId, requested.UpdatedAt);
            }

            foreach (var categoryId in requestedCategoryIds.Where(categoryId => !currentAssignments.ContainsKey(categoryId)))
            {
                current.AssignToCategory(categoryId, requested.UpdatedAt);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSoundAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var sound = await context.Sounds.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sound is null)
        {
            return;
        }

        context.Sounds.Remove(sound);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SaveChangesAsync(
        EchoBoardDbContext context,
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateSoundFilePathException(filePath);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException { SqliteErrorCode: 19 };
    }
}
