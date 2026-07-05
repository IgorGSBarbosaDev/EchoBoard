using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class ThemeResourceContractTests
{
    [Fact]
    public void AppResourcesMergeDesignSystemDictionaries()
    {
        var appResources = XDocument.Load(ProjectPath("src/EchoBoard.App/App.xaml"));

        var mergedSources = appResources
            .Descendants()
            .Where(element => element.Name.LocalName == "ResourceDictionary")
            .Select(element => element.Attribute("Source")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        mergedSources.Should().ContainInOrder(
            "Themes/Colors.xaml",
            "Themes/Brushes.xaml",
            "Themes/Typography.xaml",
            "Themes/Spacing.xaml",
            "Themes/Radii.xaml",
            "Themes/ControlStyles.xaml");
    }

    [Fact]
    public void ViewsDoNotHardcodePrdThemeHexColors()
    {
        var viewFiles = Directory.EnumerateFiles(ProjectPath("src/EchoBoard.App"), "*.xaml", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Themes{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        var themeHexValues = new[]
        {
            "#080B12", "#111827", "#151C2B", "#1C2D4D", "#2F80FF", "#5A9BFF",
            "#F4F7FB", "#A9B3C6", "#263147", "#F5F7FB", "#FFFFFF", "#E7F0FF",
            "#146EF5", "#5B6475", "#D8E0ED"
        };

        var hardcodedMatches = viewFiles
            .SelectMany(path => themeHexValues
                .Where(hex => File.ReadAllText(path).Contains(hex, StringComparison.OrdinalIgnoreCase))
                .Select(hex => $"{Path.GetRelativePath(RepositoryRoot(), path)} contains {hex}"))
            .ToArray();

        hardcodedMatches.Should().BeEmpty();
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot(), relativePath);
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
