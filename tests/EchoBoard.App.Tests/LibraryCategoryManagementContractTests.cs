using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class LibraryCategoryManagementContractTests
{
    [Fact]
    public void LibraryKeepsSeparateCreateAndManageEntryPoints()
    {
        var library = File.ReadAllText(Path.Combine(AppPath(), "Views", "LibraryPage.xaml"));

        library.Should().Contain("OnManageCategoriesClicked");
        library.Should().Contain("Manage categories");
        library.Should().Contain("Content=\"Create category\"");
        library.Should().Contain("OnCreateCategoryClicked");
        library.Should().NotContain("Favorites only");
        library.Should().NotContain("Uncategorized");
    }

    [Fact]
    public void CategoryManagementDialogEditsAndDeletesExistingCategoriesInOneOverlay()
    {
        var dialog = File.ReadAllText(Path.Combine(AppPath(), "Dialogs", "CategoryManagementDialog.xaml"));

        dialog.Should().Contain("EditCommand");
        dialog.Should().Contain("DeleteCommand");
        dialog.Should().Contain("CategoryEditorName");
        dialog.Should().Contain("CategoryDeletionConfirmationVisibility");
        dialog.Should().NotContain("Move its sounds to");
        dialog.Should().Contain("CategoryEditorSounds");
        dialog.Should().Contain("PreviewCommand");
        dialog.Should().NotContain("Create category");
        dialog.Should().NotContain("Padding=\"{StaticResource EchoBoardSpace16}\"");
        dialog.Should().NotContain("CategoryDeletionDialog");
        dialog.Should().NotContain("CategoryEditorDialog");
    }

    [Fact]
    public void CategoryManagementDialogUsesTheDefaultCenteredHostLayout()
    {
        var dialogPath = Path.Combine(AppPath(), "Dialogs", "CategoryManagementDialog.xaml");
        var dialog = XDocument.Load(dialogPath);

        dialog.Root!.Attribute("MaxWidth").Should().BeNull();
    }

    [Fact]
    public void CreateCategoryDialogKeepsTheScrollableAudioPicker()
    {
        var dialog = File.ReadAllText(Path.Combine(AppPath(), "Dialogs", "CategoryEditorDialog.xaml"));

        dialog.Should().Contain("Title=\"{Binding CategoryEditorTitle}\"");
        dialog.Should().Contain("CategoryEditorSounds");
        dialog.Should().Contain("PreviewCommand");
        dialog.Should().Contain("ScrollViewer.VerticalScrollMode=\"Enabled\"");
    }

    [Fact]
    public void SoundCardAndDetailsDoNotShowCategoryMembership()
    {
        var appPath = AppPath();
        var card = File.ReadAllText(Path.Combine(appPath, "Controls", "SoundCard.xaml"));
        var details = File.ReadAllText(Path.Combine(appPath, "Controls", "SoundDetailsDrawer.xaml"));

        card.Should().NotContain("CategoryLabel");
        card.Should().NotContain("DisplayCategoryBrush");
        details.Should().NotContain("CategoryText");
        details.Should().NotContain("SelectedCategory");
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
