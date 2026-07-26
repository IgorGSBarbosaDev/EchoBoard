using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class SoundDetailsOverlayContractTests
{
    [Fact]
    public void SoundDetailsOverlayUsesAStableBackdropBehindTheDrawer()
    {
        var appPath = AppPath();
        var shell = XDocument.Load(Path.Combine(appPath, "Views", "MainShellPage.xaml"));
        var codeBehind = File.ReadAllText(Path.Combine(appPath, "Views", "MainShellPage.xaml.cs"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var overlay = shell.Descendants().Single(element => (string?)element.Attribute(x + "Name") == "SoundDetailsOverlay");
        var backdrop = shell.Descendants().Single(element => (string?)element.Attribute(x + "Name") == "SoundDetailsBackdrop");
        var drawer = shell.Descendants().Single(element => (string?)element.Attribute(x + "Name") == "SoundDetailsDrawer");

        overlay.Attribute("Visibility")?.Value.Should().Be("Collapsed");
        backdrop.Name.LocalName.Should().Be("Border");
        backdrop.Attribute("Background")?.Value.Should().Be("{ThemeResource EchoBoardOverlayBrush}");
        backdrop.Attribute("PointerPressed")?.Value.Should().Be("OnSoundDetailsBackdropPressed");
        backdrop.Attribute("IsHitTestVisible")?.Value.Should().Be("True");
        backdrop.Attribute("Canvas.ZIndex")?.Value.Should().Be("0");
        backdrop.Attribute("Opacity").Should().BeNull();
        drawer.Attribute("Canvas.ZIndex")?.Value.Should().Be("1");

        codeBehind.Should().Contain("OnSoundDetailsBackdropPressed");
        codeBehind.Should().NotContain("SoundDetailsBackdrop.Opacity");
        codeBehind.Should().NotContain("nameof(UIElement.Opacity)");
    }

    private static string AppPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EchoBoard.sln")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root."),
            "src",
            "EchoBoard.App");
    }
}
