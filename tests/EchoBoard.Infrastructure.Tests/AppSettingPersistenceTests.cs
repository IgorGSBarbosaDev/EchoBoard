using EchoBoard.Application.Audio;
using EchoBoard.Domain.Entities;
using EchoBoard.Infrastructure.Persistence;
using EchoBoard.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EchoBoard.Infrastructure.Tests;

public sealed class AppSettingPersistenceTests
{
    [Fact]
    public async Task RepositoryUpsertsAndReadsSettingValue()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfAppSettingRepository(database.Context);

        await repository.UpsertValueAsync(MicrophoneSettingKeys.SelectedDeviceId, "mic-1", CancellationToken.None);
        await repository.UpsertValueAsync(MicrophoneSettingKeys.SelectedDeviceId, "mic-2", CancellationToken.None);
        database.Context.ChangeTracker.Clear();

        var value = await repository.GetValueAsync(MicrophoneSettingKeys.SelectedDeviceId, CancellationToken.None);

        value.Should().Be("mic-2");
    }

    [Fact]
    public async Task RepositoryReturnsNullForMissingSetting()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfAppSettingRepository(database.Context);

        var value = await repository.GetValueAsync("missing", CancellationToken.None);

        value.Should().BeNull();
    }

    [Fact]
    public async Task MigrationCreatesAppSettingsTable()
    {
        await using var database = await TestDatabase.CreateAsync();

        var tables = await database.Context.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name = 'AppSettings'")
            .ToListAsync(TestContext.Current.CancellationToken);

        tables.Should().ContainSingle().Which.Should().Be("AppSettings");
    }

    [Fact]
    public void AppSettingRejectsBlankKeys()
    {
        var act = () => AppSetting.Create(" ", "value", DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>().WithParameterName("key");
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
            var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"echoboard-settings-{Guid.NewGuid():N}.db");
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
