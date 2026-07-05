using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence.Repositories;

public sealed class EfSoundLibraryRepository : ISoundLibraryRepository
{
    private readonly EchoBoardDbContext context;

    public EfSoundLibraryRepository(EchoBoardDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<Sound>> ListSoundsAsync(CancellationToken cancellationToken)
    {
        return await context.Sounds
            .AsNoTracking()
            .OrderBy(sound => sound.SortOrder)
            .ThenBy(sound => sound.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Sound?> GetSoundAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Sounds
            .AsNoTracking()
            .SingleOrDefaultAsync(sound => sound.Id == id, cancellationToken);
    }

    public async Task<bool> SoundFilePathExistsAsync(string filePath, Guid? excludingSoundId, CancellationToken cancellationToken)
    {
        var normalizedPath = PathNormalizer.NormalizeFilePath(filePath);

        return await context.Sounds
            .AsNoTracking()
            .AnyAsync(
                sound => sound.Id != excludingSoundId && sound.FilePath == normalizedPath,
                cancellationToken);
    }

    public async Task AddSoundAsync(Sound sound, CancellationToken cancellationToken)
    {
        context.Sounds.Add(sound);
        await SaveChangesAsync(sound.FilePath, cancellationToken);
    }

    public async Task UpdateSoundAsync(Sound sound, CancellationToken cancellationToken)
    {
        context.Sounds.Update(sound);
        await SaveChangesAsync(sound.FilePath, cancellationToken);
    }

    public async Task DeleteSoundAsync(Guid id, CancellationToken cancellationToken)
    {
        var sound = await context.Sounds.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sound is null)
        {
            return;
        }

        context.Sounds.Remove(sound);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveChangesAsync(string filePath, CancellationToken cancellationToken)
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
