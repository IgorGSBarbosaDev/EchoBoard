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
    public async Task ImportSoundsCreatesValidMp3AndWavSounds()
    {
        var sounds = new FakeSoundLibraryRepository();
        var metadata = new FakeAudioFileMetadataReader();
        metadata.Add("C:\\Audio\\intro.mp3", "Intro", ".mp3", TimeSpan.FromSeconds(3), 123);
        metadata.Add("C:\\Audio\\alert.wav", "Alert", ".wav", TimeSpan.FromSeconds(1), 456);
        var useCase = new ImportSoundsUseCase(sounds, metadata);

        var result = await useCase.ExecuteAsync(
            new ImportSoundsRequest(["C:\\Audio\\intro.mp3", "C:\\Audio\\alert.wav"], Now),
            CancellationToken.None);

        result.Items.Select(item => item.Status).Should().Equal(ImportSoundStatus.Imported, ImportSoundStatus.Imported);
        sounds.Items.Should().HaveCount(2);
        sounds.Items.Select(sound => sound.SortOrder).Should().Equal(0, 1);
        sounds.Items.Select(sound => sound.Extension).Should().Equal(".mp3", ".wav");
    }

    [Fact]
    public async Task ImportSoundsRejectsUnsupportedExtensionBeforeReadingMetadata()
    {
        var sounds = new FakeSoundLibraryRepository();
        var metadata = new FakeAudioFileMetadataReader();
        var useCase = new ImportSoundsUseCase(sounds, metadata);

        var result = await useCase.ExecuteAsync(
            new ImportSoundsRequest(["C:\\Audio\\clip.flac"], Now),
            CancellationToken.None);

        result.Items.Should().ContainSingle()
            .Which.Status.Should().Be(ImportSoundStatus.InvalidExtension);
        metadata.ReadCount.Should().Be(0);
        sounds.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportSoundsSkipsDuplicateFilePath()
    {
        var sounds = new FakeSoundLibraryRepository();
        await sounds.AddSoundAsync(CreateSound("C:\\Audio\\intro.mp3"), CancellationToken.None);
        var metadata = new FakeAudioFileMetadataReader();
        metadata.Add("C:\\Audio\\INTRO.mp3", "Intro", ".mp3", TimeSpan.FromSeconds(1), 100);
        var useCase = new ImportSoundsUseCase(sounds, metadata);

        var result = await useCase.ExecuteAsync(
            new ImportSoundsRequest([" c:\\audio\\INTRO.mp3 "], Now),
            CancellationToken.None);

        result.Items.Should().ContainSingle()
            .Which.Status.Should().Be(ImportSoundStatus.SkippedDuplicate);
        metadata.ReadCount.Should().Be(0);
        sounds.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ImportSoundsReportsUnreadableFile()
    {
        var sounds = new FakeSoundLibraryRepository();
        var metadata = new FakeAudioFileMetadataReader();
        metadata.AddUnreadable("C:\\Audio\\missing.wav", "The file does not exist or cannot be read.");
        var useCase = new ImportSoundsUseCase(sounds, metadata);

        var result = await useCase.ExecuteAsync(
            new ImportSoundsRequest(["C:\\Audio\\missing.wav"], Now),
            CancellationToken.None);

        result.Items.Should().ContainSingle()
            .Which.Status.Should().Be(ImportSoundStatus.Unreadable);
        sounds.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportSoundsReportsMetadataFailure()
    {
        var sounds = new FakeSoundLibraryRepository();
        var metadata = new FakeAudioFileMetadataReader();
        metadata.AddMetadataFailure("C:\\Audio\\broken.mp3", "Audio metadata could not be read.");
        var useCase = new ImportSoundsUseCase(sounds, metadata);

        var result = await useCase.ExecuteAsync(
            new ImportSoundsRequest(["C:\\Audio\\broken.mp3"], Now),
            CancellationToken.None);

        result.Items.Should().ContainSingle()
            .Which.Status.Should().Be(ImportSoundStatus.MetadataFailed);
        sounds.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportSoundsAllowsPartialSuccess()
    {
        var sounds = new FakeSoundLibraryRepository();
        await sounds.AddSoundAsync(CreateSound("C:\\Audio\\existing.mp3"), CancellationToken.None);
        var metadata = new FakeAudioFileMetadataReader();
        metadata.Add("C:\\Audio\\valid.wav", "Valid", ".wav", TimeSpan.FromSeconds(5), 500);
        metadata.AddMetadataFailure("C:\\Audio\\broken.mp3", "Audio metadata could not be read.");
        var useCase = new ImportSoundsUseCase(sounds, metadata);

        var result = await useCase.ExecuteAsync(
            new ImportSoundsRequest(
                ["C:\\Audio\\valid.wav", "C:\\Audio\\existing.mp3", "C:\\Audio\\notes.txt", "C:\\Audio\\broken.mp3"],
                Now),
            CancellationToken.None);

        result.Items.Select(item => item.Status).Should().Equal(
            ImportSoundStatus.Imported,
            ImportSoundStatus.SkippedDuplicate,
            ImportSoundStatus.InvalidExtension,
            ImportSoundStatus.MetadataFailed);
        sounds.Items.Should().HaveCount(2);
        sounds.Items.Should().Contain(sound => sound.FilePath.EndsWith("valid.wav", StringComparison.OrdinalIgnoreCase));
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

        public IReadOnlyList<Sound> Items => sounds;

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

    private sealed class FakeAudioFileMetadataReader : IAudioFileMetadataReader
    {
        private readonly Dictionary<string, AudioFileMetadata> metadataByPath = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Exception> failuresByPath = new(StringComparer.OrdinalIgnoreCase);

        public int ReadCount { get; private set; }

        public void Add(string filePath, string displayName, string extension, TimeSpan duration, long fileSize)
        {
            var normalized = PathNormalizer.NormalizeFilePath(filePath);
            metadataByPath[normalized] = new AudioFileMetadata(displayName, normalized, extension, duration, fileSize);
        }

        public void AddUnreadable(string filePath, string message)
        {
            failuresByPath[PathNormalizer.NormalizeFilePath(filePath)] = new AudioFileUnreadableException(filePath, message);
        }

        public void AddMetadataFailure(string filePath, string message)
        {
            failuresByPath[PathNormalizer.NormalizeFilePath(filePath)] = new AudioFileMetadataException(filePath, message);
        }

        public Task<AudioFileMetadata> ReadAsync(string filePath, CancellationToken cancellationToken)
        {
            ReadCount++;
            var normalized = PathNormalizer.NormalizeFilePath(filePath);
            if (failuresByPath.TryGetValue(normalized, out var failure))
            {
                throw failure;
            }

            if (metadataByPath.TryGetValue(normalized, out var metadata))
            {
                return Task.FromResult(metadata);
            }

            throw new AudioFileMetadataException(filePath, "Audio metadata could not be read.");
        }
    }
}
