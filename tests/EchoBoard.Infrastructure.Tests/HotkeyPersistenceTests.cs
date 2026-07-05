using EchoBoard.Application.Hotkeys;
using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;
using EchoBoard.Domain.ValueObjects;
using EchoBoard.Infrastructure.Persistence;
using EchoBoard.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EchoBoard.Infrastructure.Tests;

public sealed class HotkeyPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RepositoryPersistsAndReadsHotkeyBindings()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfHotkeyBindingRepository(database.Context);
        var binding = HotkeyBinding.CreateForGlobalCommand(
            GlobalHotkeyCommand.ShowHideMainWindow,
            HotkeyCombination.Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, "F10"),
            isEnabled: true,
            Now);

        await repository.AddAsync(binding, CancellationToken.None);
        database.Context.ChangeTracker.Clear();

        var stored = await repository.GetForGlobalCommandAsync(GlobalHotkeyCommand.ShowHideMainWindow, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.NormalizedKeyCombination.Should().Be("Ctrl+Alt+F10");
        stored.PrimaryKey.Should().Be("F10");
        stored.Modifiers.Should().Be(HotkeyModifiers.Control | HotkeyModifiers.Alt);
    }

    [Fact]
    public async Task HotkeyCombinationUniqueConstraintIsCaseInsensitive()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfHotkeyBindingRepository(database.Context);
        await repository.AddAsync(
            HotkeyBinding.CreateForGlobalCommand(
                GlobalHotkeyCommand.StopAllSounds,
                HotkeyCombination.Create(HotkeyModifiers.Control, "F8"),
                isEnabled: true,
                Now),
            CancellationToken.None);

        var duplicate = HotkeyBinding.CreateForGlobalCommand(
            GlobalHotkeyCommand.PauseResumePlayback,
            HotkeyCombination.Create(HotkeyModifiers.Control, "F8"),
            isEnabled: true,
            Now);

        var act = () => repository.AddAsync(duplicate, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateHotkeyBindingException>();
    }

    [Fact]
    public async Task DatabaseInitializerAppliesHotkeyMigration()
    {
        await using var database = await TestDatabase.CreateAsync();

        var tables = await database.Context.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name = 'HotkeyBindings'")
            .ToListAsync(TestContext.Current.CancellationToken);

        tables.Should().ContainSingle().Which.Should().Be("HotkeyBindings");
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
            var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"echoboard-hotkeys-{Guid.NewGuid():N}.db");
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
