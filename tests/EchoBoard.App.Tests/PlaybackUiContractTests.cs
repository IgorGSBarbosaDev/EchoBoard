using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class PlaybackUiContractTests
{
    [Fact]
    public void NormalPlaybackSuccessMessagesAreAbsentFromApplicationUi()
    {
        var files = Directory.EnumerateFiles(AppPath(), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase));

        var contents = string.Join(Environment.NewLine, files.Select(File.ReadAllText));

        contents.Should().NotContain("Playback started");
        contents.Should().NotContain("The selected sound is playing.");
        contents.Should().NotContain("Reprodução iniciada");
    }

    [Fact]
    public void SoundCardUsesDedicatedDpiSafeActionButtons()
    {
        var card = File.ReadAllText(Path.Combine(AppPath(), "Controls", "SoundCard.xaml"));
        var styles = File.ReadAllText(Path.Combine(AppPath(), "Themes", "ControlStyles.xaml"));

        card.Should().Contain("EchoBoardCardActionButtonStyle");
        card.Should().NotContain("Width=\"30\"");
        card.Should().NotContain("Height=\"30\"");
        styles.Should().Contain("<Setter Property=\"MinWidth\" Value=\"32\" />");
        styles.Should().Contain("<Setter Property=\"MinHeight\" Value=\"32\" />");
        styles.Should().Contain("<Setter Property=\"Padding\" Value=\"4\" />");
    }

    [Fact]
    public void PlaybackContinuesWhenLibraryPageUnloads()
    {
        var codeBehind = File.ReadAllText(Path.Combine(AppPath(), "Views", "LibraryPage.xaml.cs"));

        codeBehind.Should().NotContain("StopPlaybackAsync");
        codeBehind.Should().NotContain("StopAllAsync");
    }

    [Fact]
    public void ShellOwnsTheOnlyPlaybackTimer()
    {
        var appPath = AppPath();
        var shell = File.ReadAllText(Path.Combine(appPath, "Views", "MainShellPage.xaml.cs"));
        var library = File.ReadAllText(Path.Combine(appPath, "Views", "LibraryPage.xaml.cs"));

        shell.Should().Contain("DispatcherTimer playbackTimer");
        library.Should().NotContain("DispatcherTimer");
        library.Should().NotContain("RefreshPlaybackState");
    }

    [Fact]
    public void PlayerDoesNotExposeTheRedundantStopAllAction()
    {
        var appPath = AppPath();
        var player = File.ReadAllText(Path.Combine(appPath, "ViewModels", "PlaybackBarViewModel.cs"));
        var shell = File.ReadAllText(Path.Combine(appPath, "Views", "MainShellPage.xaml"));
        var styles = File.ReadAllText(Path.Combine(appPath, "Themes", "ControlStyles.xaml"));

        player.Should().NotContain("StopAllCommand");
        shell.Should().NotContain("Parar tudo");
        styles.Should().NotContain("EchoBoardStopAllButtonStyle");
    }

    [Fact]
    public void SoundCardKeepsOnlyPlayAndMenuActions()
    {
        var card = File.ReadAllText(Path.Combine(AppPath(), "Controls", "SoundCard.xaml"));

        card.Should().Contain("PlayPauseGlyph");
        card.Should().Contain("FavoriteLabel");
        card.Should().Contain("Excluir áudio");
        card.Should().Contain("OnDeleteClicked");
        card.Should().NotContain("FavoriteGlyph");
        card.Should().NotContain(">Stopped<");
    }

    [Theory]
    [InlineData("DashboardViewModel.cs")]
    [InlineData("FavoritesViewModel.cs")]
    [InlineData("LibraryViewModel.cs")]
    [InlineData("RecentViewModel.cs")]
    public void SoundSurfacesUseTheSharedPlaybackCommand(string fileName)
    {
        var source = File.ReadAllText(Path.Combine(AppPath(), "ViewModels", fileName));

        source.Should().Contain("playbackCoordinator?.PlaySoundCommand");
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
