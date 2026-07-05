using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Application.Tests;

public sealed class SoundLibraryUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateSoundRejectsDuplicateFilePath()
    {
        var sounds = new FakeSoundLibraryRepository();
        var categories = new FakeCategoryRepository();
        await sounds.AddSoundAsync(CreateSound("C:\\Audio\\intro.mp3"), CancellationToken.None);
        var useCase = new CreateSoundUseCase(sounds, categories);

        var request = new CreateSoundRequest("Intro", " c:\\audio\\INTRO.mp3 ", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, Now);

        var act = () => useCase.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateSoundFilePathException>();
    }

    [Fact]
    public async Task UpdateSoundRejectsDuplicateFilePath()
    {
        var sounds = new FakeSoundLibraryRepository();
        var categories = new FakeCategoryRepository();
        var first = CreateSound("C:\\Audio\\first.mp3");
        var second = CreateSound("C:\\Audio\\second.mp3");
        await sounds.AddSoundAsync(first, CancellationToken.None);
        await sounds.AddSoundAsync(second, CancellationToken.None);
        var useCase = new UpdateSoundUseCase(sounds, categories);

        var request = new UpdateSoundRequest(
            second.Id,
            "Second",
            "C:\\Audio\\FIRST.mp3",
            ".mp3",
            TimeSpan.FromSeconds(2),
            2,
            0.8,
            false,
            null,
            1,
            Now.AddMinutes(1));

        var act = () => useCase.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateSoundFilePathException>();
    }

    [Fact]
    public async Task CreateSoundRejectsMissingCategory()
    {
        var sounds = new FakeSoundLibraryRepository();
        var categories = new FakeCategoryRepository();
        var useCase = new CreateSoundUseCase(sounds, categories);

        var request = new CreateSoundRequest("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, Guid.NewGuid(), 0, Now);

        var act = () => useCase.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<CategoryNotFoundException>();
    }

    [Fact]
    public async Task CreateCategoryRejectsDuplicateName()
    {
        var categories = new FakeCategoryRepository();
        await categories.AddCategoryAsync(Category.Create("Memes", 0, Now), CancellationToken.None);
        var useCase = new CreateCategoryUseCase(categories);

        var act = () => useCase.ExecuteAsync(new CreateCategoryRequest(" memes ", 1, Now), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateCategoryNameException>();
    }

    [Fact]
    public async Task UpdateCategoryRejectsDuplicateName()
    {
        var categories = new FakeCategoryRepository();
        var memes = Category.Create("Memes", 0, Now);
        var games = Category.Create("Games", 1, Now);
        await categories.AddCategoryAsync(memes, CancellationToken.None);
        await categories.AddCategoryAsync(games, CancellationToken.None);
        var useCase = new UpdateCategoryUseCase(categories);

        var act = () => useCase.ExecuteAsync(new UpdateCategoryRequest(games.Id, "memes", 1), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateCategoryNameException>();
    }

    [Fact]
    public async Task DeleteSoundRejectsMissingSound()
    {
        var useCase = new DeleteSoundUseCase(new FakeSoundLibraryRepository());

        var act = () => useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<SoundNotFoundException>();
    }

    private static Sound CreateSound(string filePath)
    {
        return Sound.Create("Intro", filePath, ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, Now);
    }

    private sealed class FakeSoundLibraryRepository : ISoundLibraryRepository
    {
        private readonly List<Sound> sounds = [];

        public Task<IReadOnlyList<Sound>> ListSoundsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Sound>>(sounds.OrderBy(sound => sound.SortOrder).ToArray());
        }

        public Task<Sound?> GetSoundAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(sounds.SingleOrDefault(sound => sound.Id == id));
        }

        public Task<bool> SoundFilePathExistsAsync(string filePath, Guid? excludingSoundId, CancellationToken cancellationToken)
        {
            var normalized = PathNormalizer.NormalizeFilePath(filePath);
            var exists = sounds.Any(sound =>
                sound.Id != excludingSoundId &&
                string.Equals(PathNormalizer.NormalizeFilePath(sound.FilePath), normalized, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public Task AddSoundAsync(Sound sound, CancellationToken cancellationToken)
        {
            sounds.Add(sound);
            return Task.CompletedTask;
        }

        public Task UpdateSoundAsync(Sound sound, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteSoundAsync(Guid id, CancellationToken cancellationToken)
        {
            sounds.RemoveAll(sound => sound.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> categories = [];

        public Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Category>>(categories.OrderBy(category => category.SortOrder).ToArray());
        }

        public Task<Category?> GetCategoryAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(categories.SingleOrDefault(category => category.Id == id));
        }

        public Task<bool> CategoryNameExistsAsync(string name, Guid? excludingCategoryId, CancellationToken cancellationToken)
        {
            var exists = categories.Any(category =>
                category.Id != excludingCategoryId &&
                string.Equals(category.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public Task AddCategoryAsync(Category category, CancellationToken cancellationToken)
        {
            categories.Add(category);
            return Task.CompletedTask;
        }

        public Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
        {
            categories.RemoveAll(category => category.Id == id);
            return Task.CompletedTask;
        }
    }
}
