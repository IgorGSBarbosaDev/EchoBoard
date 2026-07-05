using EchoBoard.Domain.Exceptions;

namespace EchoBoard.Domain.Entities;

public sealed class Category
{
    public const int NameMaxLength = 80;

    private Category()
    {
        Name = string.Empty;
    }

    private Category(Guid id, string name, int sortOrder, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Category Create(string name, int sortOrder, DateTimeOffset createdAt)
    {
        return new Category(Guid.NewGuid(), ValidateName(name), ValidateSortOrder(sortOrder), ValidateUtc(createdAt, nameof(createdAt)));
    }

    public void Rename(string name)
    {
        Name = ValidateName(name);
    }

    public void ChangeSortOrder(int sortOrder)
    {
        SortOrder = ValidateSortOrder(sortOrder);
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainValidationException($"Name must be {NameMaxLength} characters or less.");
        }

        return trimmed;
    }

    private static int ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new DomainValidationException("SortOrder must be zero or greater.");
        }

        return sortOrder;
    }

    private static DateTimeOffset ValidateUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainValidationException($"{parameterName} must be a UTC timestamp.");
        }

        return value;
    }
}
