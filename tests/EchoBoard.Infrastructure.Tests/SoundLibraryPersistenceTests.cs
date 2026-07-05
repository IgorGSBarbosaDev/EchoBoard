using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using EchoBoard.Infrastructure.Persistence;
using EchoBoard.Infrastructure.Persistence.Repositories;
using EchoBoard.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EchoBoard.Infrastructure.Tests;

public sealed class SoundLibraryPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RepositoriesPersistAndReadSoundsAndCategories()
    {
        await using var database = await TestDatabase.CreateAsync();
        var categories = new EfCategoryRepository(database.Context);
        var sounds = new EfSoundLibraryRepository(database.Context);
        var category = Category.Create("Memes", 0, Now);
        await categories.AddCategoryAsync(category, CancellationToken.None);
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(3), 123, category.Id, 1, Now);

        await sounds.AddSoundAsync(sound, CancellationToken.None);

        var storedSound = await sounds.GetSoundAsync(sound.Id, CancellationToken.None);
        var storedCategories = await categories.ListCategoriesAsync(CancellationToken.None);

        storedSound.Should().NotBeNull();
        storedSound!.Name.Should().Be("Intro");
        storedSound.CategoryId.Should().Be(category.Id);
        storedSound.Duration.Should().Be(TimeSpan.FromSeconds(3));
        storedCategories.Should().ContainSingle(item => item.Id == category.Id && item.Name == "Memes");
    }

    [Fact]
    public async Task SoundFilePathUniqueConstraintIsCaseInsensitive()
    {
        await using var database = await TestDatabase.CreateAsync();
        var sounds = new EfSoundLibraryRepository(database.Context);
        await sounds.AddSoundAsync(Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, Now), CancellationToken.None);
        var duplicate = Sound.Create("Intro Copy", "c:\\audio\\INTRO.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 1, Now);

        var act = () => sounds.AddSoundAsync(duplicate, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateSoundFilePathException>();
    }

    [Fact]
    public async Task CategoryNameUniqueConstraintIsCaseInsensitive()
    {
        await using var database = await TestDatabase.CreateAsync();
        var categories = new EfCategoryRepository(database.Context);
        await categories.AddCategoryAsync(Category.Create("Memes", 0, Now), CancellationToken.None);

        var act = () => categories.AddCategoryAsync(Category.Create("memes", 1, Now), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateCategoryNameException>();
    }

    [Fact]
    public async Task DeletingCategoryKeepsSoundsAndClearsCategoryId()
    {
        await using var database = await TestDatabase.CreateAsync();
        var categories = new EfCategoryRepository(database.Context);
        var sounds = new EfSoundLibraryRepository(database.Context);
        var category = Category.Create("Memes", 0, Now);
        await categories.AddCategoryAsync(category, CancellationToken.None);
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, category.Id, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);

        await categories.DeleteCategoryAsync(category.Id, CancellationToken.None);
        database.Context.ChangeTracker.Clear();

        var storedSound = await sounds.GetSoundAsync(sound.Id, CancellationToken.None);

        storedSound.Should().NotBeNull();
        storedSound!.CategoryId.Should().BeNull();
    }

    [Fact]
    public async Task DatabaseInitializerAppliesMigrationsToEmptyDatabase()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"echoboard-{Guid.NewGuid():N}.db");
        try
        {
            var settings = new AppSettings
            {
                AppDataDirectory = Path.GetTempPath(),
                LogDirectory = Path.GetTempPath(),
                DatabasePath = databasePath
            };
            var options = new DbContextOptionsBuilder<EchoBoardDbContext>()
                .UseSqlite(settings.DatabaseConnectionString)
                .Options;
            await using var context = new EchoBoardDbContext(options);
            var initializer = new EfDatabaseInitializer(context);

            await initializer.InitializeAsync(TestContext.Current.CancellationToken);

            var tables = await context.Database.SqlQueryRaw<string>(
                "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name IN ('Sounds', 'Categories')")
                .ToListAsync(TestContext.Current.CancellationToken);

            tables.Should().BeEquivalentTo(["Sounds", "Categories"]);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(string path, EchoBoardDbContext context)
        {
            Path = path;
            Context = context;
        }

        public string Path { get; }

        public EchoBoardDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"echoboard-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EchoBoardDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            var context = new EchoBoardDbContext(options);
            await context.Database.MigrateAsync();

            return new TestDatabase(databasePath, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            SqliteConnection.ClearAllPools();
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
