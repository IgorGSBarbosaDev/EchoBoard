namespace EchoBoard.Application.Library;

public sealed class DuplicateSoundFilePathException : Exception
{
    public DuplicateSoundFilePathException(string filePath)
        : base($"A sound already exists for file path '{filePath}'.")
    {
    }
}

public sealed class DuplicateCategoryNameException : Exception
{
    public DuplicateCategoryNameException(string name)
        : base($"A category already exists with name '{name}'.")
    {
    }
}

public sealed class SoundNotFoundException : Exception
{
    public SoundNotFoundException(Guid soundId)
        : base($"Sound '{soundId}' was not found.")
    {
    }
}

public sealed class CategoryNotFoundException : Exception
{
    public CategoryNotFoundException(Guid categoryId)
        : base($"Category '{categoryId}' was not found.")
    {
    }
}
