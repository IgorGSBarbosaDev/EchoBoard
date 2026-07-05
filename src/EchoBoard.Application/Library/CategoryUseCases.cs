using EchoBoard.Domain.Entities;

namespace EchoBoard.Application.Library;

public sealed class CreateCategoryUseCase
{
    private readonly ICategoryRepository categories;

    public CreateCategoryUseCase(ICategoryRepository categories)
    {
        this.categories = categories;
    }

    public async Task<CategoryDto> ExecuteAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await categories.CategoryNameExistsAsync(request.Name, excludingCategoryId: null, cancellationToken))
        {
            throw new DuplicateCategoryNameException(request.Name.Trim());
        }

        var category = Category.Create(request.Name, request.SortOrder, request.CreatedAt);
        await categories.AddCategoryAsync(category, cancellationToken);

        return LibraryMapper.ToDto(category);
    }
}

public sealed class GetCategoryUseCase
{
    private readonly ICategoryRepository categories;

    public GetCategoryUseCase(ICategoryRepository categories)
    {
        this.categories = categories;
    }

    public async Task<CategoryDto> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await categories.GetCategoryAsync(id, cancellationToken);

        return category is null ? throw new CategoryNotFoundException(id) : LibraryMapper.ToDto(category);
    }
}

public sealed class ListCategoriesUseCase
{
    private readonly ICategoryRepository categories;

    public ListCategoriesUseCase(ICategoryRepository categories)
    {
        this.categories = categories;
    }

    public async Task<IReadOnlyList<CategoryDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var allCategories = await categories.ListCategoriesAsync(cancellationToken);

        return allCategories.Select(LibraryMapper.ToDto).ToArray();
    }
}

public sealed class UpdateCategoryUseCase
{
    private readonly ICategoryRepository categories;

    public UpdateCategoryUseCase(ICategoryRepository categories)
    {
        this.categories = categories;
    }

    public async Task<CategoryDto> ExecuteAsync(UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await categories.GetCategoryAsync(request.Id, cancellationToken);
        if (category is null)
        {
            throw new CategoryNotFoundException(request.Id);
        }

        if (await categories.CategoryNameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            throw new DuplicateCategoryNameException(request.Name.Trim());
        }

        category.Rename(request.Name);
        category.ChangeSortOrder(request.SortOrder);
        await categories.UpdateCategoryAsync(category, cancellationToken);

        return LibraryMapper.ToDto(category);
    }
}

public sealed class DeleteCategoryUseCase
{
    private readonly ICategoryRepository categories;

    public DeleteCategoryUseCase(ICategoryRepository categories)
    {
        this.categories = categories;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await categories.GetCategoryAsync(id, cancellationToken) is null)
        {
            throw new CategoryNotFoundException(id);
        }

        await categories.DeleteCategoryAsync(id, cancellationToken);
    }
}
