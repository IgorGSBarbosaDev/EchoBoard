using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence.Repositories;

public sealed class EfCategoryRepository : ICategoryRepository
{
    private readonly EchoBoardDbContext context;

    public EfCategoryRepository(EchoBoardDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Category?> GetCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    public async Task<bool> CategoryNameExistsAsync(string name, Guid? excludingCategoryId, CancellationToken cancellationToken)
    {
        var trimmedName = name.Trim();

        return await context.Categories
            .AsNoTracking()
            .AnyAsync(
                category => category.Id != excludingCategoryId && category.Name == trimmedName,
                cancellationToken);
    }

    public async Task AddCategoryAsync(Category category, CancellationToken cancellationToken)
    {
        context.Categories.Add(category);
        await SaveChangesAsync(category.Name, cancellationToken);
    }

    public async Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken)
    {
        context.Categories.Update(category);
        await SaveChangesAsync(category.Name, cancellationToken);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await context.Categories.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (category is null)
        {
            return;
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveChangesAsync(string categoryName, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateCategoryNameException(categoryName);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException { SqliteErrorCode: 19 };
    }
}
