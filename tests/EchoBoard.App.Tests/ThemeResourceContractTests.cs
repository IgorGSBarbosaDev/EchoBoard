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
            "Themes/Palettes.xaml",
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
            .Concat(ReadColorResources("src/EchoBoard.App/Themes/Palettes.xaml"))
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
    public void DarkThemeAndAccentPalettesUseOnlyGrayscaleColors()
    {
        var darkThemeColors = ReadThemeDictionaryColors("Dark")
            .Concat(ReadThemeDictionaryColors("Default"));
        var darkPaletteColors = ReadColorResources("src/EchoBoard.App/Themes/Palettes.xaml")
            .Where(resource => resource.Key.Contains("Dark", StringComparison.Ordinal));

        var nonGrayscaleColors = darkThemeColors
            .Concat(darkPaletteColors)
            .Where(resource => !IsGrayscale(resource.Value))
            .Select(resource => $"{resource.Key}={resource.Value}")
            .ToArray();

        nonGrayscaleColors.Should().BeEmpty();
    }

    [Fact]
    public void LightThemeRetainsItsExistingSurfaceActionAndStatusColors()
    {
        var lightColors = ReadThemeDictionaryColors("Light")
            .ToDictionary(resource => resource.Key, resource => resource.Value, StringComparer.Ordinal);
        var lightPalettes = ReadColorResources("src/EchoBoard.App/Themes/Palettes.xaml")
            .Where(resource => resource.Key.Contains("Light", StringComparison.Ordinal))
            .ToDictionary(resource => resource.Key, resource => resource.Value, StringComparer.Ordinal);

        lightColors["EchoBoardBackgroundPrimaryColor"].Should().Be("#F5F7FB");
        lightColors["EchoBoardBackgroundSurfaceColor"].Should().Be("#FFFFFF");
        lightColors["EchoBoardActionColor"].Should().Be("#146EF5");
        lightColors["EchoBoardSuccessColor"].Should().Be("#168A60");
        lightColors["EchoBoardWarningColor"].Should().Be("#B77900");
        lightColors["EchoBoardErrorColor"].Should().Be("#C53030");
        lightPalettes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["EchoBoardPaletteBlueLightActionColor"] = "#146EF5",
            ["EchoBoardPaletteBlueLightHoverColor"] = "#0E5CD1",
            ["EchoBoardPaletteBlueLightPressedColor"] = "#0B49A6",
            ["EchoBoardPaletteBlueLightTintColor"] = "#E7F0FF",
            ["EchoBoardPaletteCyanLightActionColor"] = "#087E8B",
            ["EchoBoardPaletteCyanLightHoverColor"] = "#066A75",
            ["EchoBoardPaletteCyanLightPressedColor"] = "#05545D",
            ["EchoBoardPaletteCyanLightTintColor"] = "#DDF5F7",
            ["EchoBoardPaletteVioletLightActionColor"] = "#6D4DE3",
            ["EchoBoardPaletteVioletLightHoverColor"] = "#583AC7",
            ["EchoBoardPaletteVioletLightPressedColor"] = "#452D9E",
            ["EchoBoardPaletteVioletLightTintColor"] = "#EEE9FF",
            ["EchoBoardPaletteEmeraldLightActionColor"] = "#168A60",
            ["EchoBoardPaletteEmeraldLightHoverColor"] = "#11734F",
            ["EchoBoardPaletteEmeraldLightPressedColor"] = "#0D593D",
            ["EchoBoardPaletteEmeraldLightTintColor"] = "#DEF5EB",
            ["EchoBoardPaletteRoseLightActionColor"] = "#C93663",
            ["EchoBoardPaletteRoseLightHoverColor"] = "#AA294F",
            ["EchoBoardPaletteRoseLightPressedColor"] = "#861F3E",
            ["EchoBoardPaletteRoseLightTintColor"] = "#FCE5EC"
        });
    }

    [Fact]
    public void AccentPalettePickerUsesThemeSpecificVisibility()
    {
        var shellPage = File.ReadAllText(ProjectPath("src/EchoBoard.App/Views/MainShellPage.xaml"));

        shellPage.Should().Contain("Visibility=\"{Binding AccentPalettePickerVisibility}\"");
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
