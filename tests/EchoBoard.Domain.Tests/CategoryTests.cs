using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Domain.Tests;

public sealed class CategoryTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateTrimsNameAndStoresUtcTimestamp()
    {
        var category = Category.Create("  Memes  ", 3, CreatedAt);

        category.Name.Should().Be("Memes");
        category.SortOrder.Should().Be(3);
        category.CreatedAt.Should().Be(CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRejectsBlankName(string name)
    {
        var act = () => Category.Create(name, 0, CreatedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*Name*");
    }

    [Fact]
    public void CreateRejectsNegativeSortOrder()
    {
        var act = () => Category.Create("Memes", -1, CreatedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*SortOrder*");
    }

    [Fact]
    public void CreateRejectsNonUtcTimestamp()
    {
        var localTime = new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.FromHours(-3));

        var act = () => Category.Create("Memes", 0, localTime);

        act.Should().Throw<DomainValidationException>().WithMessage("*UTC*");
    }

    [Fact]
    public void MutationsUpdateExpectedFields()
    {
        var category = Category.Create("Memes", 0, CreatedAt);

        category.Rename("Games");
        category.ChangeSortOrder(5);

        category.Name.Should().Be("Games");
        category.SortOrder.Should().Be(5);
    }
}
