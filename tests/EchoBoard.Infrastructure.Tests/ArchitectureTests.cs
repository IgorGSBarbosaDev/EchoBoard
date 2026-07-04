using System.Xml.Linq;
using EchoBoard.Infrastructure;
using EchoBoard.Infrastructure.Persistence;
using EchoBoard.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EchoBoard.Infrastructure.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void InfrastructureProjectReferencesApplicationAndDomain()
    {
        var references = ProjectFile.ProjectReferences("src/EchoBoard.Infrastructure/EchoBoard.Infrastructure.csproj");

        references.Should().BeEquivalentTo(
            [
                "../EchoBoard.Application/EchoBoard.Application.csproj",
                "../EchoBoard.Domain/EchoBoard.Domain.csproj"
            ]);
    }

    [Fact]
    public void InfrastructureServicesCanBeRegisteredWithoutOpeningDatabase()
    {
        var settings = new AppSettings
        {
            AppDataDirectory = Path.GetTempPath(),
            LogDirectory = Path.GetTempPath(),
            DatabasePath = Path.Combine(Path.GetTempPath(), "echoboard-foundation-test.db")
        };
        var services = new ServiceCollection();

        services.AddInfrastructure(settings);

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(EchoBoardDbContext));
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
