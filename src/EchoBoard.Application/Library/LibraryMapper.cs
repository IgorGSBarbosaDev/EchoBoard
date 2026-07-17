using EchoBoard.Domain.Entities;

namespace EchoBoard.Application.Library;

internal static class LibraryMapper
{
    public static SoundDto ToDto(Sound sound)
    {
        return new SoundDto(
            sound.Id,
            sound.Name,
            sound.FilePath,
            sound.Extension,
            sound.Duration,
            sound.FileSize,
            sound.Volume,
            sound.IsFavorite,
            sound.CategoryId,
            sound.SortOrder,
            sound.CreatedAt,
            sound.UpdatedAt,
            sound.IsLoopEnabled,
            sound.StopPreviousSound,
            sound.AllowOverlap,
            [.. sound.WaveformPeaks]);
    }

    public static CategoryDto ToDto(Category category)
    {
        return new CategoryDto(category.Id, category.Name, category.SortOrder, category.CreatedAt);
    }
}
