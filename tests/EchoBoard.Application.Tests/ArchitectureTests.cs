using System.Xml.Linq;
using EchoBoard.Application;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EchoBoard.Application.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ApplicationProjectReferencesOnlyDomain()
    {
        var references = ProjectFile.ProjectReferences("src/EchoBoard.Application/EchoBoard.Application.csproj");

        references.Should().BeEquivalentTo(["../EchoBoard.Domain/EchoBoard.Domain.csproj"]);
    }

    [Fact]
    public void ApplicationServicesCanBeRegistered()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        result.Should().BeSameAs(services);
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
