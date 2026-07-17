namespace EchoBoard.Application.Library;

public sealed class QuerySoundLibraryUseCase
{
    private readonly ISoundLibraryRepository sounds;
    private readonly ICategoryRepository categories;
    private readonly ISoundFileAvailabilityReader fileAvailability;
    private readonly IRecentlyPlayedRepository? recentlyPlayed;

    public QuerySoundLibraryUseCase(
        ISoundLibraryRepository sounds,
        ICategoryRepository categories,
        ISoundFileAvailabilityReader fileAvailability,
        IRecentlyPlayedRepository? recentlyPlayed = null)
    {
        this.sounds = sounds;
        this.categories = categories;
        this.fileAvailability = fileAvailability;
        this.recentlyPlayed = recentlyPlayed;
    }

    public async Task<SoundLibraryResult> ExecuteAsync(SoundLibraryFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var allSounds = await sounds.ListSoundsAsync(cancellationToken);
        var allCategories = await categories.ListCategoriesAsync(cancellationToken);
        var playCounts = recentlyPlayed is null
            ? new Dictionary<Guid, int>()
            : await recentlyPlayed.GetPlayCountsAsync(cancellationToken);
        var categoryById = allCategories.ToDictionary(category => category.Id);
        var countSource = filter.FavoritesOnly
            ? allSounds.Where(sound => sound.IsFavorite).ToArray()
            : allSounds;

        var categoryCounts = countSource
            .Where(sound => sound.CategoryId is not null)
            .GroupBy(sound => sound.CategoryId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());
        var uncategorizedCount = countSource.Count(sound => sound.CategoryId is null);

        var filtered = countSource.AsEnumerable();
        if (filter.CategoryId is not null)
        {
            filtered = filtered.Where(sound => sound.CategoryId == filter.CategoryId);
        }
        else if (filter.IncludeUncategorizedOnly)
        {
            filtered = filtered.Where(sound => sound.CategoryId is null);
        }

        var searchText = filter.SearchText?.Trim();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(sound => sound.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        var filteredSounds = filtered.ToArray();
        var items = new List<SoundLibraryItemDto>(filteredSounds.Length);
        foreach (var sound in filteredSounds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileExists = await fileAvailability.ExistsAsync(sound.FilePath, cancellationToken);
            categoryById.TryGetValue(sound.CategoryId ?? Guid.Empty, out var category);

            items.Add(new SoundLibraryItemDto(
                sound.Id,
                sound.Name,
                sound.FilePath,
                sound.Extension,
                sound.Duration,
                sound.FileSize,
                sound.Volume,
                sound.IsFavorite,
                sound.CategoryId,
                category?.Name,
                category?.SortOrder,
                sound.SortOrder,
                IsMissingFile: !fileExists,
                sound.CreatedAt,
                sound.UpdatedAt,
                sound.IsLoopEnabled,
                sound.StopPreviousSound,
                sound.AllowOverlap,
                [.. sound.WaveformPeaks],
                playCounts.GetValueOrDefault(sound.Id)));
        }

        var categoryDtos = allCategories
            .Select(category => new SoundLibraryCategoryDto(
                category.Id,
                category.Name,
                category.SortOrder,
                categoryCounts.GetValueOrDefault(category.Id),
                category.CreatedAt))
            .ToArray();

        return new SoundLibraryResult(
            items,
            categoryDtos,
            countSource.Count,
            uncategorizedCount,
            BuildActiveFilterSummary(filter, categoryById, searchText));
    }

    private static string BuildActiveFilterSummary(
        SoundLibraryFilter filter,
        Dictionary<Guid, Domain.Entities.Category> categoryById,
        string? searchText)
    {
        var parts = new List<string>();
        if (filter.FavoritesOnly)
        {
            parts.Add("Favorites");
        }

        if (filter.CategoryId is not null && categoryById.TryGetValue(filter.CategoryId.Value, out var category))
        {
            parts.Add(category.Name);
        }
        else if (filter.IncludeUncategorizedOnly)
        {
            parts.Add("Uncategorized");
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            parts.Add($"\"{searchText}\"");
        }

        return parts.Count == 0 ? "All sounds" : string.Join(", ", parts);
    }
}
