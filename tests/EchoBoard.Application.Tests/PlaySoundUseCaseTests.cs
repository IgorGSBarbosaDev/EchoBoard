using EchoBoard.Application.Audio;
using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Application.Tests;

public sealed class PlaySoundUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 17, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsyncAppliesDefaultsAndRecordsOnlySuccessfulPlayback()
    {
        var sound = CreateSound();
        var history = new FakeRecentlyPlayedRepository();
        var playback = new FakePlaybackEngine();
        var useCase = CreateUseCase(sound, history, playback);

        await useCase.ExecuteAsync(new PlaySoundRequest(sound.Id, Now.AddSeconds(1)), TestContext.Current.CancellationToken);

        playback.Operations.Should().Equal("stop-all", "play");
        playback.LastOptions.Should().Be(SoundPlaybackOptions.Default);
        history.Entries.Should().ContainSingle(entry => entry.SoundId == sound.Id);
    }

    [Fact]
    public async Task ExecuteAsyncStopsOnlyDuplicateWhenOverlapIsDisabled()
    {
        var sound = CreateSound();
        sound.ConfigurePlayback(isLoopEnabled: true, stopPreviousSound: false, allowOverlap: false, Now.AddSeconds(1));
        var history = new FakeRecentlyPlayedRepository();
        var playback = new FakePlaybackEngine();
        var useCase = CreateUseCase(sound, history, playback);

        await useCase.ExecuteAsync(new PlaySoundRequest(sound.Id, Now.AddSeconds(2)), TestContext.Current.CancellationToken);

        playback.Operations.Should().Equal("stop-sound", "play");
        playback.LastOptions.Should().Be(new SoundPlaybackOptions(IsLoopEnabled: true));
    }

    [Fact]
    public async Task ExecuteAsyncDoesNotRecordFailedPlayback()
    {
        var sound = CreateSound();
        var history = new FakeRecentlyPlayedRepository();
        var playback = new FakePlaybackEngine { PlayException = new IOException("decode failed") };
        var useCase = CreateUseCase(sound, history, playback);

        var act = () => useCase.ExecuteAsync(new PlaySoundRequest(sound.Id, Now.AddSeconds(1)), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<IOException>();
        history.Entries.Should().BeEmpty();
    }

    private static PlaySoundUseCase CreateUseCase(Sound sound, FakeRecentlyPlayedRepository history, FakePlaybackEngine playback) =>
        new(new FakeSoundRepository(sound), new FakeFileReader(), history, playback);

    private static Sound CreateSound() =>
        Sound.Create("Intro", "C:\\Audio\\intro.wav", ".wav", TimeSpan.FromSeconds(1), 1, null, 0, Now);

    private sealed class FakeSoundRepository(Sound sound) : ISoundLibraryRepository
    {
        public Task<IReadOnlyList<Sound>> ListSoundsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Sound>>([sound]);
        public Task<Sound?> GetSoundAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Sound?>(id == sound.Id ? sound : null);
        public Task<bool> SoundFilePathExistsAsync(string filePath, Guid? excludingSoundId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task AddSoundAsync(Sound item, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateSoundAsync(Sound item, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteSoundAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeFileReader : ISoundFileAvailabilityReader
    {
        public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeRecentlyPlayedRepository : IRecentlyPlayedRepository
    {
        public List<RecentlyPlayed> Entries { get; } = [];
        public Task AddAsync(RecentlyPlayed entry, CancellationToken cancellationToken) { Entries.Add(entry); return Task.CompletedTask; }
        public Task<IReadOnlyDictionary<Guid, int>> GetPlayCountsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());
        public Task<IReadOnlyList<RecentlyPlayed>> ListAsync(int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RecentlyPlayed>>(Entries.Take(limit).ToArray());
    }

    private sealed class FakePlaybackEngine : ISoundPlaybackEngine
    {
        public List<string> Operations { get; } = [];
        public SoundPlaybackOptions? LastOptions { get; private set; }
        public Exception? PlayException { get; init; }
        public Task PlayAsync(string filePath, double volume, CancellationToken cancellationToken) => PlayAsync(filePath, volume, SoundPlaybackOptions.Default, cancellationToken);
        public Task PlayAsync(string filePath, double volume, SoundPlaybackOptions options, CancellationToken cancellationToken)
        {
            if (PlayException is not null) throw PlayException;
            Operations.Add("play");
            LastOptions = options;
            return Task.CompletedTask;
        }
        public Task StopAllAsync(CancellationToken cancellationToken) { Operations.Add("stop-all"); return Task.CompletedTask; }
        public Task StopSoundAsync(string filePath, CancellationToken cancellationToken) { Operations.Add("stop-sound"); return Task.CompletedTask; }
        public Task TogglePauseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetVolumeAsync(double volume, CancellationToken cancellationToken) => Task.CompletedTask;
        public SoundPlaybackSnapshot GetSnapshot() => SoundPlaybackSnapshot.Idle;
    }
}
