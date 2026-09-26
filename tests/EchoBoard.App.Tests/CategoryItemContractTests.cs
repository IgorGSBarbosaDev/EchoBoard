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
