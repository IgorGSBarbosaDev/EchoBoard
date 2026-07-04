using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Domain.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void DomainProjectHasNoProjectReferences()
    {
        var references = ProjectFile.ProjectReferences("src/EchoBoard.Domain/EchoBoard.Domain.csproj");

        references.Should().BeEmpty();
    }

    [Fact]
    public void AppProjectReferencesApplicationAudioAndInfrastructure()
    {
        var references = ProjectFile.ProjectReferences("src/EchoBoard.App/EchoBoard.App.csproj");

        references.Should().BeEquivalentTo(
            [
                "../EchoBoard.Application/EchoBoard.Application.csproj",
                "../EchoBoard.Audio/EchoBoard.Audio.csproj",
                "../EchoBoard.Infrastructure/EchoBoard.Infrastructure.csproj"
            ]);
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
