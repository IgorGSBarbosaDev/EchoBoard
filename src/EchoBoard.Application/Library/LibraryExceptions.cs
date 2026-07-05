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

public sealed class AudioFileUnreadableException : Exception
{
    public AudioFileUnreadableException(string filePath, string message)
        : base(message)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }
}

public sealed class AudioFileMetadataException : Exception
{
    public AudioFileMetadataException(string filePath, string message)
        : base(message)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }
}
