using EchoBoard.App.Hosting;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Library;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class HardwarePlaybackSmokeTests
{
    [Fact]
    public async Task PhysicalMixerStartsDecodedFileOnAnAvailableRoute()
    {
        var filePath = Environment.GetEnvironmentVariable("ECHOBOARD_SMOKE_AUDIO_FILE");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var host = AppHost.Create();
        try
        {
            await host.StartAsync(TestContext.Current.CancellationToken);
            await AppHost.InitializeDatabaseAsync(host.Services, TestContext.Current.CancellationToken);
            await AppHost.InitializeAudioEngineAsync(host.Services, TestContext.Current.CancellationToken);
            var engine = host.Services.GetRequiredService<ISoundPlaybackEngine>();
            var routing = host.Services.GetRequiredService<IAudioRoutingEngine>();

            var started = await engine.PlayAsync(
                filePath,
                0.5,
                TestContext.Current.CancellationToken);
            await Task.Delay(350, TestContext.Current.CancellationToken);

            engine.GetSnapshot().Position.Should().BeGreaterThan(TimeSpan.Zero);
            (started.IsMonitorActive || started.IsVirtualOutputActive).Should().BeTrue();
            var routingSnapshot = routing.GetRoutingSnapshot();
            routingSnapshot.EngineState.Should().Be(AudioRouteState.Active);
            routingSnapshot.MicrophoneState.Should().Be(AudioRouteState.Active);
            routingSnapshot.InputDeviceName.Should().NotBeNullOrWhiteSpace();
            await engine.StopAllAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            await host.StopAsync(CancellationToken.None);
            if (host is IAsyncDisposable asyncHost)
            {
                await asyncHost.DisposeAsync();
            }
            else
            {
                host.Dispose();
            }
        }
    }

    [Fact]
    public async Task ConfiguredVirtualEndpointStartsAndCanBeRestored()
    {
        var virtualOutputId = Environment.GetEnvironmentVariable("ECHOBOARD_SMOKE_VIRTUAL_OUTPUT_ID");
        if (string.IsNullOrWhiteSpace(virtualOutputId))
        {
            return;
        }

        var host = AppHost.Create();
        AudioRoutingSettingsDto? previous = null;
        try
        {
            await host.StartAsync(TestContext.Current.CancellationToken);
            await AppHost.InitializeDatabaseAsync(host.Services, TestContext.Current.CancellationToken);
            await using var scope = host.Services.CreateAsyncScope();
            var outputs = await scope.ServiceProvider
                .GetRequiredService<ListAudioOutputDevicesUseCase>()
                .ExecuteAsync(TestContext.Current.CancellationToken);
            var selected = outputs.Single(device =>
                string.Equals(device.Id, virtualOutputId, StringComparison.Ordinal));
            var load = scope.ServiceProvider.GetRequiredService<LoadAudioRoutingSettingsUseCase>();
            var save = scope.ServiceProvider.GetRequiredService<SaveAudioRoutingSettingsUseCase>();
            previous = await load.ExecuteAsync(TestContext.Current.CancellationToken);

            var snapshot = await save.ExecuteAsync(
                previous with
                {
                    VirtualOutputDeviceId = selected.Id,
                    VirtualOutputDeviceName = selected.Name
                },
                TestContext.Current.CancellationToken);

            snapshot.VirtualOutputState.Should().Be(AudioRouteState.Active);
            snapshot.VirtualOutputDeviceName.Should().Be(selected.Name);
            snapshot.VirtualOutputFormat.Should().NotBeNull();
        }
        finally
        {
            if (previous is not null)
            {
                await using var restoreScope = host.Services.CreateAsyncScope();
                await restoreScope.ServiceProvider
                    .GetRequiredService<SaveAudioRoutingSettingsUseCase>()
                    .ExecuteAsync(previous, CancellationToken.None);
            }

            await host.StopAsync(CancellationToken.None);
            if (host is IAsyncDisposable asyncHost)
            {
                await asyncHost.DisposeAsync();
            }
            else
            {
                host.Dispose();
            }
        }
    }

    [Fact]
    public async Task ConfiguredExternalAudioDecodesAndStartsAfterEngineRestart()
    {
        var filePath = Environment.GetEnvironmentVariable("ECHOBOARD_SMOKE_AUDIO");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var first = await PlayOnceAsync(filePath);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        var second = await PlayOnceAsync(filePath);

        first.Duration.Should().BeGreaterThan(TimeSpan.FromSeconds(8));
        second.Duration.Should().BeCloseTo(first.Duration, TimeSpan.FromMilliseconds(10));
        first.PlayCountAfter.Should().Be(first.PlayCountBefore + 1);
        second.PlayCountAfter.Should().Be(second.PlayCountBefore + 1);
    }

    private static async Task<SmokeResult> PlayOnceAsync(string filePath)
    {
        var host = AppHost.Create();
        try
        {
            await host.StartAsync(TestContext.Current.CancellationToken);
            await AppHost.InitializeDatabaseAsync(host.Services, TestContext.Current.CancellationToken);
            await AppHost.InitializeAudioEngineAsync(host.Services, TestContext.Current.CancellationToken);
            await using var scope = host.Services.CreateAsyncScope();
            var query = scope.ServiceProvider.GetRequiredService<QuerySoundLibraryUseCase>();
            var libraryBefore = await query.ExecuteAsync(SoundLibraryFilter.All, TestContext.Current.CancellationToken);
            var sound = libraryBefore.Sounds.Single(item =>
                string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            var result = await scope.ServiceProvider.GetRequiredService<PlaySoundUseCase>().ExecuteAsync(
                new PlaySoundRequest(sound.Id, DateTimeOffset.UtcNow),
                TestContext.Current.CancellationToken);
            await Task.Delay(350, TestContext.Current.CancellationToken);
            var snapshot = scope.ServiceProvider.GetRequiredService<ISoundPlaybackEngine>().GetSnapshot();
            snapshot.Position.Should().BeGreaterThan(TimeSpan.Zero);
            var libraryAfter = await query.ExecuteAsync(SoundLibraryFilter.All, TestContext.Current.CancellationToken);
            var updated = libraryAfter.Sounds.Single(item => item.Id == sound.Id);
            await scope.ServiceProvider.GetRequiredService<ISoundPlaybackEngine>()
                .StopAllAsync(TestContext.Current.CancellationToken);
            return new SmokeResult(result.Snapshot.Duration, sound.PlayCount, updated.PlayCount);
        }
        finally
        {
            await host.StopAsync(TestContext.Current.CancellationToken);
            if (host is IAsyncDisposable asyncHost)
            {
                await asyncHost.DisposeAsync();
            }
            else
            {
                host.Dispose();
            }
        }
    }

    private sealed record SmokeResult(TimeSpan Duration, int PlayCountBefore, int PlayCountAfter);
}
