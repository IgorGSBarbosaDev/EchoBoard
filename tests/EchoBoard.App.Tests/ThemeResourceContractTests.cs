using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class ThemeResourceContractTests
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

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

        var themeHexValues = ReadColorResources("src/EchoBoard.App/Themes/Colors.xaml")
            .Select(resource => resource.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var hardcodedMatches = viewFiles
            .SelectMany(path => themeHexValues
                .Where(hex => File.ReadAllText(path).Contains(hex, StringComparison.OrdinalIgnoreCase))
                .Select(hex => $"{Path.GetRelativePath(RepositoryRoot(), path)} contains {hex}"))
            .ToArray();

        hardcodedMatches.Should().BeEmpty();
    }

    [Fact]
    public void DarkThemeUsesOnlyGrayscaleColors()
    {
        var darkThemeColors = ReadThemeDictionaryColors("Dark")
            .Concat(ReadThemeDictionaryColors("Default"));

        var nonGrayscaleColors = darkThemeColors
            .Where(resource => !IsGrayscale(resource.Value))
            .Select(resource => $"{resource.Key}={resource.Value}")
            .ToArray();

        nonGrayscaleColors.Should().BeEmpty();
    }

    [Fact]
    public void AppUsesOnlyTheDarkThemeAndHasNoAppearancePickers()
    {
        var colors = XDocument.Load(ProjectPath("src/EchoBoard.App/Themes/Colors.xaml"));
        var shellPage = XDocument.Load(ProjectPath("src/EchoBoard.App/Views/MainShellPage.xaml"));
        var shellRoot = shellPage.Descendants().Single(element =>
            (string?)element.Attribute(XamlNamespace + "Name") == "ShellRoot");

        colors.Descendants().Should().NotContain(element =>
            element.Name.LocalName == "ResourceDictionary"
            && (string?)element.Attribute(XamlNamespace + "Key") == "Light");
        shellRoot.Attribute("RequestedTheme")?.Value.Should().Be("Dark");
        File.ReadAllText(ProjectPath("src/EchoBoard.App/App.xaml"))
            .Should().NotContain("Palettes.xaml");
        shellPage.ToString().Should().NotContain("Switch theme").And.NotContain("Choose accent palette");

        var designReference = File.ReadAllText(ProjectPath("docs/design-reference/echoboard-design-reference.html"));
        designReference.Should().NotContain("themeToggle").And.NotContain("data-theme=\"light\"");
    }

    private static KeyValuePair<string, string>[] ReadColorResources(string relativePath)
    {
        return XDocument.Load(ProjectPath(relativePath))
            .Descendants()
            .Where(element => element.Name.LocalName == "Color")
            .Select(element => new KeyValuePair<string, string>(
                element.Attribute(XamlNamespace + "Key")?.Value ?? string.Empty,
                element.Value.Trim()))
            .ToArray();
    }

    private static KeyValuePair<string, string>[] ReadThemeDictionaryColors(string dictionaryKey)
    {
        var dictionary = XDocument.Load(ProjectPath("src/EchoBoard.App/Themes/Colors.xaml"))
            .Descendants()
            .Single(element => element.Name.LocalName == "ResourceDictionary"
                && element.Attribute(XamlNamespace + "Key")?.Value == dictionaryKey);

        return dictionary.Elements()
            .Where(element => element.Name.LocalName == "Color")
            .Select(element => new KeyValuePair<string, string>(
                element.Attribute(XamlNamespace + "Key")?.Value ?? string.Empty,
                element.Value.Trim()))
            .ToArray();
    }

    private static bool IsGrayscale(string color)
    {
        var hex = color.Trim().TrimStart('#');
        if (hex.Length == 8)
        {
            hex = hex[2..];
        }

        return hex.Length == 6
            && string.Equals(hex[..2], hex[2..4], StringComparison.OrdinalIgnoreCase)
            && string.Equals(hex[2..4], hex[4..6], StringComparison.OrdinalIgnoreCase);
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
