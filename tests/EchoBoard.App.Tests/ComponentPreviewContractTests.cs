using EchoBoard.App.Controls;
using EchoBoard.App.ViewModels;
using FluentAssertions;
using Xunit;

namespace EchoBoard.App.Tests;

public sealed class ComponentPreviewContractTests
{
    [Fact]
    public void ComponentStateEnumsExposeAllPlannedValues()
    {
        Enum.GetNames<DeviceStatusKind>().Should().Equal(
            "Connected",
            "Disconnected",
            "Unavailable",
            "Warning",
            "Loading",
            "Unknown");

        Enum.GetNames<AudioLevelMeterVariant>().Should().Equal(
            "Microphone",
            "Effects",
            "Monitor",
            "VirtualOutput");

        Enum.GetNames<ToastNotificationKind>().Should().Equal(
            "Success",
            "Info",
            "Warning",
            "Error");
    }

    [Fact]
    public void PreviewDataCoversSoundCardStatesAndCategoryItems()
    {
        var viewModel = new LibraryViewModel();

        viewModel.PreviewCategories.Should().Contain(category => category.IsSelected);
        viewModel.PreviewCategories.Should().OnlyContain(category => !string.IsNullOrWhiteSpace(category.Name));

        viewModel.PreviewSounds.Should().Contain(sound => sound.IsSelected);
        viewModel.PreviewSounds.Should().Contain(sound => sound.IsPlaying);
        viewModel.PreviewSounds.Should().Contain(sound => sound.IsFavorite);
        viewModel.PreviewSounds.Should().Contain(sound => sound.IsCompact);
        viewModel.PreviewSounds.Should().OnlyContain(sound => !string.IsNullOrWhiteSpace(sound.Title));
    }

    [Fact]
    public void DiagnosticsPreviewDataCoversDeviceStatusesAndMeterVariants()
    {
        var viewModel = new AudioDiagnosticsViewModel();

        viewModel.PreviewDevices.Select(device => device.Status).Should().Contain([
            DeviceStatusKind.Connected,
            DeviceStatusKind.Warning,
            DeviceStatusKind.Unavailable,
            DeviceStatusKind.Loading]);

        viewModel.PreviewMeters.Select(meter => meter.Variant).Should().Equal(
            AudioLevelMeterVariant.Microphone,
            AudioLevelMeterVariant.Effects,
            AudioLevelMeterVariant.Monitor,
            AudioLevelMeterVariant.VirtualOutput);

        viewModel.PreviewMeters.Should().OnlyContain(meter => meter.Level >= 0 && meter.Level <= 1);
    }

    [Fact]
    public void DashboardPreviewIncludesToastAndEmptyStateActions()
    {
        var viewModel = new DashboardViewModel();

        viewModel.PreviewToast.Kind.Should().Be(ToastNotificationKind.Info);
        viewModel.PreviewToast.Title.Should().NotBeNullOrWhiteSpace();
        viewModel.PreviewEmptyState.Title.Should().NotBeNullOrWhiteSpace();
        viewModel.PreviewEmptyState.PrimaryActionText.Should().NotBeNullOrWhiteSpace();
        viewModel.PreviewSounds.Should().NotBeEmpty();
    }

    [Fact]
    public void MeterPreviewModelClampsNormalizedLevel()
    {
        new AudioMeterPreviewModel("Quiet", 0.25, AudioLevelMeterVariant.Microphone).Level.Should().Be(0.25);
        new AudioMeterPreviewModel("Too low", -0.5, AudioLevelMeterVariant.Effects).Level.Should().Be(0);
        new AudioMeterPreviewModel("Too high", 1.5, AudioLevelMeterVariant.VirtualOutput).Level.Should().Be(1);
    }
}
