using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class CategoryItemContractTests
{
    [Fact]
    public void CategorySymbolHasNoDecorativeCircleBehindIt()
    {
        var categoryItem = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "EchoBoard.App",
            "Controls",
            "CategoryItem.xaml"));

        categoryItem.Descendants().Should().ContainSingle(element => element.Name.LocalName == "SymbolIcon");
        categoryItem.Descendants().Should().NotContain(element => element.Name.LocalName == "Ellipse");
    }

    [Fact]
    public void CategoryButtonTracksItsCommandAndParameterAfterBinding()
    {
        var categoryItem = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "EchoBoard.App",
            "Controls",
            "CategoryItem.xaml"));
        var button = categoryItem.Descendants().Single(element => element.Name.LocalName == "Button");

        button.Attribute("Command")?.Value.Should().Be("{x:Bind Command, Mode=OneWay}");
        button.Attribute("CommandParameter")?.Value.Should().Be("{x:Bind CommandParameter, Mode=OneWay}");
        button.Attribute("IsEnabled")?.Value.Should().Be("{x:Bind IsEnabled, Mode=OneWay}");
    }

    [Fact]
    public void MainShellTopbarDoesNotRenderTheGlobalSoundSearch()
    {
        var shellPage = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "EchoBoard.App",
            "Views",
            "MainShellPage.xaml"));
        var shellViewModel = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "EchoBoard.App",
            "ViewModels",
            "MainShellViewModel.cs"));
        var controlStyles = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "EchoBoard.App",
            "Themes",
            "ControlStyles.xaml"));
        var libraryPage = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "EchoBoard.App",
            "Views",
            "LibraryPage.xaml"));

        shellPage.Should().NotContain("Search sounds placeholder");
        shellPage.Should().NotContain("EchoBoardTopbarSearchTextBoxStyle");
        shellViewModel.Should().NotContain("SearchPlaceholder");
        controlStyles.Should().NotContain("EchoBoardTopbarSearchTextBoxStyle");
        controlStyles.Should().Contain("x:Key=\"EchoBoardSearchTextBoxStyle\"");
        libraryPage.Should().Contain("Find sounds by name");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EchoBoard.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
