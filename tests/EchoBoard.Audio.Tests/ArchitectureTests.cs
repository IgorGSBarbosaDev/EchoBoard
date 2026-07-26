using System.Xml.Linq;
using EchoBoard.Audio;
using EchoBoard.Application.Audio;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EchoBoard.Audio.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void AudioProjectReferencesApplicationAndDomain()
    {
        var references = ProjectFile.ProjectReferences("src/EchoBoard.Audio/EchoBoard.Audio.csproj");

        references.Should().BeEquivalentTo(
            [
                "../EchoBoard.Application/EchoBoard.Application.csproj",
                "../EchoBoard.Domain/EchoBoard.Domain.csproj"
            ]);
    }

    [Fact]
    public void AudioServicesCanBeRegistered()
    {
        var services = new ServiceCollection();

        var result = services.AddAudio();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public async Task PlaybackAndRoutingResolveTheSameMixerInstance()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddAudio();
        await using var provider = services.BuildServiceProvider();

        var playback = provider.GetRequiredService<ISoundPlaybackEngine>();
        var routing = provider.GetRequiredService<IAudioRoutingEngine>();

        playback.Should().BeSameAs(routing);
    }
}

file static class ProjectFile
{
    public static IReadOnlyCollection<string> ProjectReferences(string relativePath)
    {
        var document = XDocument.Load(Path.Combine(RepositoryRoot(), relativePath));

        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value.Replace('\\', '/'))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EchoBoard.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
