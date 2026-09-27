using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence.Repositories;

public sealed class EfCategoryRepository : ICategoryRepository
{
    private readonly IDbContextFactory<EchoBoardDbContext> contextFactory;

    public EfCategoryRepository(IDbContextFactory<EchoBoardDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Category?> GetCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    public async Task<bool> CategoryNameExistsAsync(string name, Guid? excludingCategoryId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var trimmedName = name.Trim();

        return await context.Categories
            .AsNoTracking()
            .AnyAsync(
                category => category.Id != excludingCategoryId && category.Name == trimmedName,
                cancellationToken);
    }

    public async Task AddCategoryAsync(Category category, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Categories.Add(category);
        await SaveChangesAsync(context, category.Name, cancellationToken);
    }

    public async Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Categories.Update(category);
        await SaveChangesAsync(context, category.Name, cancellationToken);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await context.Categories.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (category is null)
        {
            return;
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SaveChangesAsync(
        EchoBoardDbContext context,
        string categoryName,
        CancellationToken cancellationToken)
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
