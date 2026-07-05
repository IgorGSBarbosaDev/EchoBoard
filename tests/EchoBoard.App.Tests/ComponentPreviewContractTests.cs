using EchoBoard.App.Controls;
using EchoBoard.App.ViewModels;
using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
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
    public async Task LibraryViewModelLoadsPersistedSounds()
    {
        var sounds = new FakeSoundLibraryRepository();
        await sounds.AddSoundAsync(
            Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(3), 123, null, 0, Now),
            CancellationToken.None);
        var viewModel = CreateLibraryViewModel(sounds);

        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.Categories.Should().Contain(category => category.IsSelected);
        viewModel.Sounds.Should().ContainSingle(sound =>
            sound.Title == "Intro" &&
            sound.DurationText == "0:03" &&
            sound.CategoryLabel == "Uncategorized");
    }

    [Fact]
    public async Task LibraryViewModelImportsFilesAndRefreshesSounds()
    {
        var sounds = new FakeSoundLibraryRepository();
        var metadata = new FakeAudioFileMetadataReader();
        metadata.Add("C:\\Audio\\intro.wav", "Intro", ".wav", TimeSpan.FromSeconds(2), 96000);
        var viewModel = CreateLibraryViewModel(sounds, metadata);

        await viewModel.ImportFilePathsAsync(["C:\\Audio\\intro.wav"], CancellationToken.None);

        viewModel.Sounds.Should().ContainSingle(sound => sound.Title == "Intro");
        viewModel.ImportToast.Should().NotBeNull();
        viewModel.ImportToast!.Kind.Should().Be(ToastNotificationKind.Success);
        viewModel.ImportFeedbackItems.Should().BeEmpty();
    }

    [Fact]
    public async Task LibraryViewModelReportsCancelledImportWithoutMutation()
    {
        var sounds = new FakeSoundLibraryRepository();
        var viewModel = CreateLibraryViewModel(sounds);

        await viewModel.ImportFilePathsAsync([], CancellationToken.None);

        sounds.Items.Should().BeEmpty();
        viewModel.ImportToast.Should().NotBeNull();
        viewModel.ImportToast!.Kind.Should().Be(ToastNotificationKind.Info);
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

    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    private static LibraryViewModel CreateLibraryViewModel(
        FakeSoundLibraryRepository? sounds = null,
        FakeAudioFileMetadataReader? metadata = null)
    {
        sounds ??= new FakeSoundLibraryRepository();
        metadata ??= new FakeAudioFileMetadataReader();

        return new LibraryViewModel(
            new ListSoundsUseCase(sounds),
            new ImportSoundsUseCase(sounds, metadata));
    }

    private sealed class FakeSoundLibraryRepository : ISoundLibraryRepository
    {
        private readonly List<Sound> sounds = [];

        public IReadOnlyList<Sound> Items => sounds;

        public Task<IReadOnlyList<Sound>> ListSoundsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Sound>>(sounds.OrderBy(sound => sound.SortOrder).ToArray());
        }

        public Task<Sound?> GetSoundAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(sounds.SingleOrDefault(sound => sound.Id == id));
        }

        public Task<bool> SoundFilePathExistsAsync(string filePath, Guid? excludingSoundId, CancellationToken cancellationToken)
        {
            var normalized = PathNormalizer.NormalizeFilePath(filePath);
            var exists = sounds.Any(sound =>
                sound.Id != excludingSoundId &&
                string.Equals(PathNormalizer.NormalizeFilePath(sound.FilePath), normalized, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public Task AddSoundAsync(Sound sound, CancellationToken cancellationToken)
        {
            sounds.Add(sound);
            return Task.CompletedTask;
        }

        public Task UpdateSoundAsync(Sound sound, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteSoundAsync(Guid id, CancellationToken cancellationToken)
        {
            sounds.RemoveAll(sound => sound.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAudioFileMetadataReader : IAudioFileMetadataReader
    {
        private readonly Dictionary<string, AudioFileMetadata> metadataByPath = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string filePath, string displayName, string extension, TimeSpan duration, long fileSize)
        {
            var normalized = PathNormalizer.NormalizeFilePath(filePath);
            metadataByPath[normalized] = new AudioFileMetadata(displayName, normalized, extension, duration, fileSize);
        }

        public Task<AudioFileMetadata> ReadAsync(string filePath, CancellationToken cancellationToken)
        {
            var normalized = PathNormalizer.NormalizeFilePath(filePath);
            if (metadataByPath.TryGetValue(normalized, out var metadata))
            {
                return Task.FromResult(metadata);
            }

            throw new AudioFileMetadataException(filePath, "Audio metadata could not be read.");
        }
    }
}
