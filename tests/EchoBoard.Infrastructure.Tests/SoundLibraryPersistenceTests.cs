using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using EchoBoard.Infrastructure.Files;
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
        var categories = new EfCategoryRepository(database.ContextFactory);
        var sounds = new EfSoundLibraryRepository(database.ContextFactory);
        var category = Category.Create("Memes", 0, Now);
        await categories.AddCategoryAsync(category, CancellationToken.None);
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(3), 123, category.Id, 1, Now);

        await sounds.AddSoundAsync(sound, CancellationToken.None);

        var storedSound = await sounds.GetSoundAsync(sound.Id, CancellationToken.None);
        var storedCategories = await categories.ListCategoriesAsync(CancellationToken.None);

        storedSound.Should().NotBeNull();
        storedSound!.Name.Should().Be("Intro");
        storedSound.CategoryIds.Should().ContainSingle().Which.Should().Be(category.Id);
        storedSound.Duration.Should().Be(TimeSpan.FromSeconds(3));
        storedCategories.Should().ContainSingle(item => item.Id == category.Id && item.Name == "Memes");
    }

    [Fact]
    public async Task SoundFilePathUniqueConstraintIsCaseInsensitive()
    {
        await using var database = await TestDatabase.CreateAsync();
        var sounds = new EfSoundLibraryRepository(database.ContextFactory);
        await sounds.AddSoundAsync(Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, Now), CancellationToken.None);
        var duplicate = Sound.Create("Intro Copy", "c:\\audio\\INTRO.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 1, Now);

        var act = () => sounds.AddSoundAsync(duplicate, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateSoundFilePathException>();
    }

    [Fact]
    public async Task RepeatedUpdatesDoNotReuseTrackedEntityInstances()
    {
        await using var database = await TestDatabase.CreateAsync();
        var sounds = new EfSoundLibraryRepository(database.ContextFactory);
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);

        var firstUpdate = await sounds.GetSoundAsync(sound.Id, CancellationToken.None);
        firstUpdate!.Rename("Intro v2", Now.AddSeconds(1));
        await sounds.UpdateSoundAsync(firstUpdate, CancellationToken.None);

        var secondUpdate = await sounds.GetSoundAsync(sound.Id, CancellationToken.None);
        secondUpdate!.Rename("Intro v3", Now.AddSeconds(2));
        await sounds.UpdateSoundAsync(secondUpdate, CancellationToken.None);

        (await sounds.GetSoundAsync(sound.Id, CancellationToken.None))!.Name.Should().Be("Intro v3");
    }

    [Fact]
    public async Task CategoryNameUniqueConstraintIsCaseInsensitive()
    {
        await using var database = await TestDatabase.CreateAsync();
        var categories = new EfCategoryRepository(database.ContextFactory);
        await categories.AddCategoryAsync(Category.Create("Memes", 0, Now), CancellationToken.None);

        var act = () => categories.AddCategoryAsync(Category.Create("memes", 1, Now), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateCategoryNameException>();
    }

    [Fact]
    public async Task DeletingCategoryKeepsSoundsAndOtherCategoryMemberships()
    {
        await using var database = await TestDatabase.CreateAsync();
        var categories = new EfCategoryRepository(database.ContextFactory);
        var sounds = new EfSoundLibraryRepository(database.ContextFactory);
        var category = Category.Create("Memes", 0, Now);
        var otherCategory = Category.Create("Games", 1, Now);
        await categories.AddCategoryAsync(category, CancellationToken.None);
        await categories.AddCategoryAsync(otherCategory, CancellationToken.None);
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, category.Id, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        var categorizedSound = await sounds.GetSoundAsync(sound.Id, CancellationToken.None);
        categorizedSound!.AssignToCategory(otherCategory.Id, Now.AddMinutes(1));
        await sounds.UpdateSoundsAsync([categorizedSound], CancellationToken.None);

        (await sounds.GetSoundAsync(sound.Id, CancellationToken.None))!.CategoryIds
            .Should().BeEquivalentTo([category.Id, otherCategory.Id]);

        await categories.DeleteCategoryAsync(category.Id, CancellationToken.None);
        database.Context.ChangeTracker.Clear();

        var storedSound = await sounds.GetSoundAsync(sound.Id, CancellationToken.None);

        storedSound.Should().NotBeNull();
        storedSound!.CategoryIds.Should().ContainSingle().Which.Should().Be(otherCategory.Id);
        (await categories.GetCategoryAsync(category.Id, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task HistoryPersistsPlaybackCountsAndCascadesWhenSoundIsDeleted()
    {
        await using var database = await TestDatabase.CreateAsync();
        var sounds = new EfSoundLibraryRepository(database.ContextFactory);
        var history = new EfRecentlyPlayedRepository(database.ContextFactory);
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, Now);
        sound.ConfigurePlayback(isLoopEnabled: true, stopPreviousSound: false, allowOverlap: true, Now.AddSeconds(1));
        sound.SetWaveformPeaks(Enumerable.Repeat((byte)50, 32).ToArray(), Now.AddSeconds(1));
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        await history.AddAsync(RecentlyPlayed.Create(sound.Id, Now.AddSeconds(2)), CancellationToken.None);
        await history.AddAsync(RecentlyPlayed.Create(sound.Id, Now.AddSeconds(3)), CancellationToken.None);

        database.Context.ChangeTracker.Clear();
        var stored = await sounds.GetSoundAsync(sound.Id, CancellationToken.None);
        var counts = await history.GetPlayCountsAsync(CancellationToken.None);

        stored!.IsLoopEnabled.Should().BeTrue();
        stored.StopPreviousSound.Should().BeFalse();
        stored.AllowOverlap.Should().BeTrue();
        stored.WaveformPeaks.Should().OnlyContain(peak => peak == 50);
        counts[sound.Id].Should().Be(2);

        await sounds.DeleteSoundAsync(sound.Id, CancellationToken.None);
        database.Context.ChangeTracker.Clear();

        (await history.ListAsync(10, CancellationToken.None)).Should().BeEmpty();
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
                "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name IN ('Sounds', 'Categories', 'SoundCategories', 'RecentlyPlayed')")
                .ToListAsync(TestContext.Current.CancellationToken);

            tables.Should().BeEquivalentTo(["Sounds", "Categories", "SoundCategories", "RecentlyPlayed"]);
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

    [Fact]
    public async Task ManyToManyMigrationPreservesExistingSoundCategoryLinks()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"echoboard-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<EchoBoardDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using var context = new EchoBoardDbContext(options);
            var cancellationToken = TestContext.Current.CancellationToken;
            await context.Database.MigrateAsync("20260717014813_DashboardRedesign", cancellationToken);
            await context.Database.OpenConnectionAsync(cancellationToken);

            var category = Category.Create("Memes", 0, Now);
            var soundId = Guid.NewGuid();
            await using (var command = ((SqliteConnection)context.Database.GetDbConnection()).CreateCommand())
            {
                command.CommandText = """
                    INSERT INTO Categories (Id, Name, SortOrder, CreatedAt)
                    VALUES ($categoryId, $categoryName, 0, $createdAt);
                    INSERT INTO Sounds (
                        Id, Name, FilePath, Extension, Duration, FileSize, Volume, IsFavorite,
                        CategoryId, SortOrder, CreatedAt, UpdatedAt, IsLoopEnabled,
                        StopPreviousSound, AllowOverlap, WaveformPeaks)
                    VALUES (
                        $soundId, 'Intro', 'C:\\Audio\\intro.mp3', '.mp3', $duration, 123, 1.0, 0,
                        $categoryId, 0, $createdAt, $createdAt, 0, 1, 0, $waveform);
                    """;
                command.Parameters.AddWithValue("$categoryId", category.Id);
                command.Parameters.AddWithValue("$categoryName", category.Name);
                command.Parameters.AddWithValue("$soundId", soundId);
                command.Parameters.AddWithValue("$createdAt", Now.UtcDateTime);
                command.Parameters.AddWithValue("$duration", TimeSpan.FromSeconds(3).Ticks);
                command.Parameters.AddWithValue("$waveform", Array.Empty<byte>());
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var verifyCommand = ((SqliteConnection)context.Database.GetDbConnection()).CreateCommand())
            {
                verifyCommand.CommandText = "SELECT COUNT(*) FROM Sounds WHERE Id = $soundId;";
                verifyCommand.Parameters.AddWithValue("$soundId", soundId);
                var soundCount = (long)(await verifyCommand.ExecuteScalarAsync(cancellationToken))!;
                soundCount.Should().Be(1);
            }

            await context.Database.MigrateAsync(cancellationToken);

            var repository = new EfSoundLibraryRepository(new TestEchoBoardDbContextFactory(options));
            var migratedSound = await repository.GetSoundAsync(soundId, cancellationToken);

            migratedSound.Should().NotBeNull();
            migratedSound!.CategoryIds.Should().ContainSingle().Which.Should().Be(category.Id);
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

    [Fact]
    public async Task SoundFileAvailabilityReaderReportsExistingAndMissingFiles()
    {
        var existingPath = Path.Combine(Path.GetTempPath(), $"echoboard-{Guid.NewGuid():N}.wav");
        var missingPath = Path.Combine(Path.GetTempPath(), $"echoboard-{Guid.NewGuid():N}.wav");
        await File.WriteAllTextAsync(existingPath, "not audio", TestContext.Current.CancellationToken);
        try
        {
            var reader = new SoundFileAvailabilityReader();

            var existing = await reader.ExistsAsync(existingPath, TestContext.Current.CancellationToken);
            var missing = await reader.ExistsAsync(missingPath, TestContext.Current.CancellationToken);

            existing.Should().BeTrue();
            missing.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(existingPath))
            {
                File.Delete(existingPath);
            }
        }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(string path, EchoBoardDbContext context, IDbContextFactory<EchoBoardDbContext> contextFactory)
        {
            Path = path;
            Context = context;
            ContextFactory = contextFactory;
        }

        public string Path { get; }

        public EchoBoardDbContext Context { get; }

        public IDbContextFactory<EchoBoardDbContext> ContextFactory { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"echoboard-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EchoBoardDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            var context = new EchoBoardDbContext(options);
            await context.Database.MigrateAsync();

            return new TestDatabase(databasePath, context, new TestEchoBoardDbContextFactory(options));
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
