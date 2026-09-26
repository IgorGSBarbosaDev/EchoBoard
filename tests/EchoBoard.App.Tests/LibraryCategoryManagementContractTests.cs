using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class LibraryCategoryManagementContractTests
{
    [Fact]
    public void LibraryUsesOnePencilEntryPointAndFavoritesNavigationItem()
    {
        var library = File.ReadAllText(Path.Combine(AppPath(), "Views", "LibraryPage.xaml"));

        library.Should().Contain("OnManageCategoriesClicked");
        library.Should().Contain("Manage categories");
        library.Should().NotContain("Create category");
        library.Should().NotContain("Favorites only");
        library.Should().NotContain("Uncategorized");
    }

    [Fact]
    public void CategoryManagementDialogKeepsCreateEditDeleteAndSoundActionsInOneOverlay()
    {
        var dialog = File.ReadAllText(Path.Combine(AppPath(), "Dialogs", "CategoryManagementDialog.xaml"));

        dialog.Should().Contain("CreateManagedCategoryCommand");
        dialog.Should().Contain("EditCommand");
        dialog.Should().Contain("DeleteCommand");
        dialog.Should().Contain("CategoryEditorName");
        dialog.Should().Contain("CategoryDeletionConfirmationVisibility");
        dialog.Should().Contain("CategoryEditorSounds");
        dialog.Should().Contain("PreviewCommand");
        dialog.Should().NotContain("CategoryDeletionDialog");
        dialog.Should().NotContain("CategoryEditorDialog");
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
