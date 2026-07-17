using EchoBoard.App.Controls;
using EchoBoard.App.ViewModels;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Application.Library;
using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;
using EchoBoard.Domain.ValueObjects;
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
        var categories = new FakeCategoryRepository();
        var category = Category.Create("Memes", 0, Now);
        await categories.AddCategoryAsync(category, CancellationToken.None);
        await sounds.AddSoundAsync(
            Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(3), 123, category.Id, 0, Now),
            CancellationToken.None);
        var files = new FakeSoundFileAvailabilityReader(DefaultExists: true);
        var viewModel = CreateLibraryViewModel(sounds, categories: categories, files: files);

        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.Categories.Should().Contain(item => item.Name == "All sounds" && item.CountText == "1" && item.IsSelected);
        viewModel.Categories.Should().Contain(item => item.Name == "Memes" && item.CountText == "1");
        viewModel.Sounds.Should().ContainSingle(sound =>
            sound.Title == "Intro" &&
            sound.DurationText == "0:03" &&
            sound.CategoryLabel == "Memes" &&
            sound.HotkeyText == "No hotkey" &&
            !sound.IsMissingFile);
    }

    [Fact]
    public async Task LibraryViewModelDisplaysSoundHotkeyBindings()
    {
        var sounds = new FakeSoundLibraryRepository();
        var hotkeys = new FakeHotkeyBindingRepository();
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(3), 123, null, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        await hotkeys.AddAsync(
            HotkeyBinding.CreateForSound(
                sound.Id,
                HotkeyCombination.Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, "F8"),
                isEnabled: true,
                Now),
            CancellationToken.None);
        var viewModel = CreateLibraryViewModel(sounds, hotkeys: hotkeys, files: new FakeSoundFileAvailabilityReader(DefaultExists: true));

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectSoundCommand.Execute(sound.Id);

        viewModel.Sounds.Should().ContainSingle(item => item.HotkeyText == "Ctrl+Alt+F8");
        viewModel.HotkeyPrimaryKey.Should().Be("F8");
        viewModel.SaveSoundHotkeyCommand.Should().NotBeNull();
        viewModel.RemoveSoundHotkeyCommand.Should().NotBeNull();
    }

    [Fact]
    public void SettingsViewModelExposesRequiredGlobalHotkeyRows()
    {
        var hotkeys = new FakeHotkeyBindingRepository();
        var viewModel = CreateSettingsViewModel(hotkeys);

        viewModel.GlobalHotkeys.Select(item => item.Command).Should().Equal(
            GlobalHotkeyCommand.StopAllSounds,
            GlobalHotkeyCommand.PauseResumePlayback,
            GlobalHotkeyCommand.ShowHideMainWindow);
    }

    [Fact]
    public async Task LibraryViewModelSeparatesEmptyLibraryFromFilteredNoResults()
    {
        var sounds = new FakeSoundLibraryRepository();
        await sounds.AddSoundAsync(
            Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(3), 123, null, 0, Now),
            CancellationToken.None);
        var viewModel = CreateLibraryViewModel(sounds, files: new FakeSoundFileAvailabilityReader(DefaultExists: true));

        await viewModel.LoadAsync(CancellationToken.None);
        await viewModel.UpdateSearchTextAsync("missing", CancellationToken.None);

        viewModel.EmptyStateTitle.Should().Be("No results");
        viewModel.EmptyStateVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Visible);
        viewModel.SoundGridVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Collapsed);
        viewModel.ClearFiltersVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Visible);
    }

    [Fact]
    public async Task LibraryViewModelTogglesFavoriteAndRefreshesSound()
    {
        var sounds = new FakeSoundLibraryRepository();
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(3), 123, null, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        var viewModel = CreateLibraryViewModel(sounds, files: new FakeSoundFileAvailabilityReader(DefaultExists: true));
        await viewModel.LoadAsync(CancellationToken.None);

        await viewModel.ToggleFavoriteAsync(sound.Id, CancellationToken.None);

        sounds.Items.Single().IsFavorite.Should().BeTrue();
        viewModel.Sounds.Should().ContainSingle(item => item.IsFavorite);
    }

    [Fact]
    public async Task LibraryViewModelPlaysSelectedSoundAndUpdatesProgress()
    {
        var sounds = new FakeSoundLibraryRepository();
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(30), 123, null, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        var playback = new FakeSoundPlaybackEngine();
        var viewModel = CreateLibraryViewModel(sounds, files: new FakeSoundFileAvailabilityReader(DefaultExists: true), playback: playback);
        await viewModel.LoadAsync(CancellationToken.None);

        await viewModel.ActivateSoundCommand.ExecuteAsync(sound.Id);
        playback.AdvanceTo(TimeSpan.FromSeconds(4));
        viewModel.RefreshPlaybackState();

        playback.PlayedPaths.Should().Equal(sound.FilePath);
        viewModel.SelectedSoundId.Should().Be(sound.Id);
        viewModel.Sounds.Should().ContainSingle(item => item.IsPlaying && !item.IsPaused && item.StatusText == "Playing" && item.DurationText == "0:04 / 0:30");
    }

    [Fact]
    public async Task LibraryViewModelPausesAndResumesSameSound()
    {
        var sounds = new FakeSoundLibraryRepository();
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(3), 123, null, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        var playback = new FakeSoundPlaybackEngine();
        var viewModel = CreateLibraryViewModel(sounds, files: new FakeSoundFileAvailabilityReader(DefaultExists: true), playback: playback);
        await viewModel.LoadAsync(CancellationToken.None);

        await viewModel.ActivateSoundCommand.ExecuteAsync(sound.Id);
        await viewModel.ActivateSoundCommand.ExecuteAsync(sound.Id);

        playback.ToggleCount.Should().Be(1);
        viewModel.Sounds.Should().ContainSingle(item => item.IsPaused && !item.IsPlaying && item.StatusText == "Paused");

        await viewModel.ActivateSoundCommand.ExecuteAsync(sound.Id);
        viewModel.Sounds.Should().ContainSingle(item => item.IsPlaying && !item.IsPaused);
    }

    [Fact]
    public async Task LibraryViewModelStopsCurrentSoundBeforePlayingAnother()
    {
        var sounds = new FakeSoundLibraryRepository();
        var first = Sound.Create("First", "C:\\Audio\\first.wav", ".wav", TimeSpan.FromSeconds(3), 123, null, 0, Now);
        var second = Sound.Create("Second", "C:\\Audio\\second.wav", ".wav", TimeSpan.FromSeconds(5), 456, null, 1, Now);
        await sounds.AddSoundAsync(first, CancellationToken.None);
        await sounds.AddSoundAsync(second, CancellationToken.None);
        var playback = new FakeSoundPlaybackEngine();
        var viewModel = CreateLibraryViewModel(sounds, files: new FakeSoundFileAvailabilityReader(DefaultExists: true), playback: playback);
        await viewModel.LoadAsync(CancellationToken.None);

        await viewModel.ActivateSoundCommand.ExecuteAsync(first.Id);
        await viewModel.ActivateSoundCommand.ExecuteAsync(second.Id);

        playback.Operations.TakeLast(2).Should().Equal("stop", $"play:{second.FilePath}");
        viewModel.Sounds.Single(item => item.Id == first.Id).StatusText.Should().Be("Stopped");
        viewModel.Sounds.Single(item => item.Id == second.Id).IsPlaying.Should().BeTrue();
    }

    [Fact]
    public async Task LibraryViewModelResetsCardWhenPlaybackFinishes()
    {
        var sounds = new FakeSoundLibraryRepository();
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(3), 123, null, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        var playback = new FakeSoundPlaybackEngine();
        var viewModel = CreateLibraryViewModel(sounds, files: new FakeSoundFileAvailabilityReader(DefaultExists: true), playback: playback);
        await viewModel.LoadAsync(CancellationToken.None);
        await viewModel.ActivateSoundCommand.ExecuteAsync(sound.Id);

        playback.Complete();
        viewModel.RefreshPlaybackState();

        viewModel.Sounds.Should().ContainSingle(item => !item.IsPlaying && !item.IsPaused && item.StatusText == "Stopped" && item.DurationText == "0:03");
    }

    [Fact]
    public async Task LibraryViewModelReportsPlaybackOpenErrorWithoutThrowing()
    {
        var sounds = new FakeSoundLibraryRepository();
        var sound = Sound.Create("Broken", "C:\\Audio\\broken.flac", ".flac", TimeSpan.FromSeconds(3), 123, null, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        var playback = new FakeSoundPlaybackEngine { PlayException = new InvalidDataException("Invalid audio.") };
        var viewModel = CreateLibraryViewModel(sounds, files: new FakeSoundFileAvailabilityReader(DefaultExists: true), playback: playback);
        await viewModel.LoadAsync(CancellationToken.None);

        var action = async () => await viewModel.ActivateSoundCommand.ExecuteAsync(sound.Id);

        await action.Should().NotThrowAsync();
        viewModel.PlaybackToast.Should().NotBeNull();
        viewModel.PlaybackToast!.Description.Should().Contain("corrupted");
        viewModel.Sounds.Should().ContainSingle(item => item.StatusText == "Stopped" && !item.IsPlaying);
    }

    [Fact]
    public async Task PlaybackBarTracksPlaybackPauseAndTimeline()
    {
        var sounds = new FakeSoundLibraryRepository();
        var sound = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(30), 123, null, 0, Now);
        await sounds.AddSoundAsync(sound, CancellationToken.None);
        var playback = new FakeSoundPlaybackEngine();
        var viewModel = CreatePlaybackBarViewModel(sounds, playback);
        await viewModel.LoadAsync(CancellationToken.None);

        await playback.PlayAsync(sound.FilePath, sound.Volume, CancellationToken.None);
        playback.AdvanceTo(TimeSpan.FromSeconds(12), sound.Duration);
        viewModel.Refresh();
        await viewModel.PlayPauseCommand.ExecuteAsync(null);

        viewModel.Title.Should().Be("Intro");
        viewModel.ProgressPercent.Should().BeApproximately(40, 0.01);
        viewModel.ElapsedText.Should().Be("0:12");
        viewModel.DurationText.Should().Be("0:30");
        viewModel.IsPaused.Should().BeTrue();
    }

    [Fact]
    public async Task PlaybackBarUpdatesMixersRepeatAndStopsAll()
    {
        var playback = new FakeSoundPlaybackEngine();
        var viewModel = CreatePlaybackBarViewModel(new FakeSoundLibraryRepository(), playback);
        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.EffectsPercent = 67;
        viewModel.MonitorPercent = 42;
        viewModel.MicrophonePercent = 74;
        viewModel.ToggleRepeatCommand.Execute(null);
        await viewModel.StopAllCommand.ExecuteAsync(null);

        playback.LastVolume.Should().BeApproximately(0.67, 0.001);
        viewModel.MonitorPercentText.Should().Be("42%");
        viewModel.MicrophonePercentText.Should().Be("74%");
        viewModel.IsRepeatEnabled.Should().BeTrue();
        playback.Operations.Should().Contain("stop");
    }

    [Fact]
    public async Task FavoritesViewModelLoadsOnlyFavoriteSounds()
    {
        var sounds = new FakeSoundLibraryRepository();
        var favorite = Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(3), 123, null, 0, Now);
        favorite.SetFavorite(true, Now.AddMinutes(1));
        var regular = Sound.Create("Alert", "C:\\Audio\\alert.wav", ".wav", TimeSpan.FromSeconds(2), 456, null, 1, Now);
        await sounds.AddSoundAsync(favorite, CancellationToken.None);
        await sounds.AddSoundAsync(regular, CancellationToken.None);
        var categories = new FakeCategoryRepository();
        var files = new FakeSoundFileAvailabilityReader(DefaultExists: true);
        var viewModel = new FavoritesViewModel(
            new QuerySoundLibraryUseCase(sounds, categories, files),
            new SetSoundFavoriteUseCase(sounds));

        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.Sounds.Should().ContainSingle(item => item.Id == favorite.Id);
        viewModel.EmptyStateVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Collapsed);
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
        var viewModel = CreateAudioDiagnosticsViewModel();

        viewModel.PreviewDevices.Select(device => device.Status).Should().Contain(DeviceStatusKind.Unavailable);

        viewModel.PreviewMeters.Select(meter => meter.Variant).Should().Equal(AudioLevelMeterVariant.Microphone);

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
        FakeAudioFileMetadataReader? metadata = null,
        FakeCategoryRepository? categories = null,
        FakeHotkeyBindingRepository? hotkeys = null,
        FakeSoundFileAvailabilityReader? files = null,
        FakeSoundPlaybackEngine? playback = null)
    {
        sounds ??= new FakeSoundLibraryRepository();
        metadata ??= new FakeAudioFileMetadataReader();
        categories ??= new FakeCategoryRepository();
        hotkeys ??= new FakeHotkeyBindingRepository();
        files ??= new FakeSoundFileAvailabilityReader();
        playback ??= new FakeSoundPlaybackEngine();
        var runtime = new FakeHotkeyRuntime();

        return new LibraryViewModel(
            new QuerySoundLibraryUseCase(sounds, categories, files),
            new ImportSoundsUseCase(sounds, metadata),
            new CreateCategoryUseCase(categories),
            new UpdateCategoryUseCase(categories),
            new DeleteCategoryUseCase(categories),
            new SetSoundFavoriteUseCase(sounds),
            new AssignSoundCategoryUseCase(sounds, categories),
            new ListHotkeyBindingsUseCase(hotkeys, runtime),
            new AssignSoundHotkeyUseCase(hotkeys, sounds, runtime),
            new RemoveHotkeyBindingUseCase(hotkeys, runtime),
            new SetHotkeyBindingEnabledUseCase(hotkeys, runtime),
            playback);
    }

    private static SettingsViewModel CreateSettingsViewModel(FakeHotkeyBindingRepository? hotkeys = null)
    {
        hotkeys ??= new FakeHotkeyBindingRepository();
        var runtime = new FakeHotkeyRuntime();
        var settings = new FakeAppSettingRepository();
        var controller = new FakeMicrophoneCaptureController();

        return new SettingsViewModel(
            new ListHotkeyBindingsUseCase(hotkeys, runtime),
            new AssignGlobalHotkeyUseCase(hotkeys, runtime),
            new RemoveHotkeyBindingUseCase(hotkeys, runtime),
            new SetHotkeyBindingEnabledUseCase(hotkeys, runtime),
            new ListMicrophoneDevicesUseCase(controller),
            new LoadMicrophoneSettingsUseCase(settings, controller),
            new SelectMicrophoneDeviceUseCase(settings, controller),
            new SetMicrophoneGainUseCase(settings, controller),
            new SetMicrophoneMuteUseCase(settings, controller),
            new StartMicrophoneCaptureUseCase(controller),
            new StopMicrophoneCaptureUseCase(controller),
            new GetMicrophoneCaptureSnapshotUseCase(controller));
    }

    private static AudioDiagnosticsViewModel CreateAudioDiagnosticsViewModel()
    {
        return new AudioDiagnosticsViewModel(new GetMicrophoneCaptureSnapshotUseCase(new FakeMicrophoneCaptureController()));
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

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> categories = [];

        public Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Category>>(categories.OrderBy(category => category.SortOrder).ToArray());
        }

        public Task<Category?> GetCategoryAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(categories.SingleOrDefault(category => category.Id == id));
        }

        public Task<bool> CategoryNameExistsAsync(string name, Guid? excludingCategoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(categories.Any(category =>
                category.Id != excludingCategoryId &&
                string.Equals(category.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddCategoryAsync(Category category, CancellationToken cancellationToken)
        {
            categories.Add(category);
            return Task.CompletedTask;
        }

        public Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken)
        {
            var index = categories.FindIndex(item => item.Id == category.Id);
            if (index >= 0)
            {
                categories[index] = category;
            }

            return Task.CompletedTask;
        }

        public Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
        {
            categories.RemoveAll(category => category.Id == id);
            foreach (var category in categories)
            {
                _ = category;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeSoundFileAvailabilityReader : ISoundFileAvailabilityReader
    {
        public FakeSoundFileAvailabilityReader(bool DefaultExists = false)
        {
            this.DefaultExists = DefaultExists;
        }

        public bool DefaultExists { get; }

        public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(DefaultExists);
        }
    }

    private sealed class FakeHotkeyBindingRepository : IHotkeyBindingRepository
    {
        private readonly List<HotkeyBinding> bindings = [];

        public Task<IReadOnlyList<HotkeyBinding>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<HotkeyBinding>>(bindings.ToArray());
        }

        public Task<HotkeyBinding?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(bindings.SingleOrDefault(binding => binding.Id == id));
        }

        public Task<HotkeyBinding?> GetForSoundAsync(Guid soundId, CancellationToken cancellationToken)
        {
            return Task.FromResult(bindings.SingleOrDefault(binding => binding.SoundId == soundId));
        }

        public Task<HotkeyBinding?> GetForGlobalCommandAsync(GlobalHotkeyCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(bindings.SingleOrDefault(binding => binding.GlobalCommand == command));
        }

        public Task<bool> CombinationExistsAsync(string normalizedKeyCombination, Guid? excludingBindingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(bindings.Any(binding =>
                binding.Id != excludingBindingId &&
                string.Equals(binding.NormalizedKeyCombination, normalizedKeyCombination, StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddAsync(HotkeyBinding binding, CancellationToken cancellationToken)
        {
            bindings.Add(binding);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(HotkeyBinding binding, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            bindings.RemoveAll(binding => binding.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHotkeyRuntime : IHotkeyRuntimeService
    {
        public Task<HotkeyRegistrationResult> RegisterBindingAsync(HotkeyBinding binding, CancellationToken cancellationToken)
        {
            return Task.FromResult(binding.IsEnabled
                ? HotkeyRegistrationResult.Active("Registered.")
                : HotkeyRegistrationResult.Disabled("Disabled."));
        }

        public Task UnregisterBindingAsync(Guid bindingId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public HotkeyRegistrationState GetRegistrationState(Guid bindingId)
        {
            return HotkeyRegistrationState.Active;
        }
    }

    private sealed class FakeAppSettingRepository : IAppSettingRepository
    {
        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(null);
        }

        public Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMicrophoneCaptureController : IMicrophoneCaptureController
    {
        public IMicrophonePcmSource? CurrentSource => null;

        public Task<IReadOnlyList<AudioInputDeviceDto>> ListInputDevicesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AudioInputDeviceDto>>([]);
        }

        public Task RestoreSelectionAsync(MicrophoneSettingsDto settings, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SelectDeviceAsync(AudioInputDeviceDto device, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SetGainAsync(double gain, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SetMutedAsync(bool isMuted, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public MicrophoneCaptureSnapshot GetSnapshot()
        {
            return MicrophoneCaptureSnapshot.Unavailable("No microphone available. Connect an input device.", MicrophoneSettingsDto.Default);
        }
    }

    private static PlaybackBarViewModel CreatePlaybackBarViewModel(FakeSoundLibraryRepository sounds, FakeSoundPlaybackEngine playback)
    {
        var microphone = new FakeMicrophoneCaptureController();
        return new PlaybackBarViewModel(
            playback,
            new QuerySoundLibraryUseCase(sounds, new FakeCategoryRepository(), new FakeSoundFileAvailabilityReader(DefaultExists: true)),
            new GetMicrophoneCaptureSnapshotUseCase(microphone),
            new SetMicrophoneGainUseCase(new FakeAppSettingRepository(), microphone));
    }

    private sealed class FakeSoundPlaybackEngine : ISoundPlaybackEngine
    {
        private SoundPlaybackSnapshot snapshot = SoundPlaybackSnapshot.Idle;

        public List<string> PlayedPaths { get; } = [];

        public List<string> Operations { get; } = [];

        public int ToggleCount { get; private set; }

        public Exception? PlayException { get; init; }

        public double LastVolume { get; private set; }

        public Task PlayAsync(string filePath, double volume, CancellationToken cancellationToken)
        {
            if (PlayException is not null)
            {
                throw PlayException;
            }

            PlayedPaths.Add(filePath);
            Operations.Add($"play:{filePath}");
            snapshot = new SoundPlaybackSnapshot(filePath, TimeSpan.Zero, TimeSpan.Zero, IsPlaying: true, IsPaused: false);
            return Task.CompletedTask;
        }

        public Task StopAllAsync(CancellationToken cancellationToken)
        {
            Operations.Add("stop");
            snapshot = SoundPlaybackSnapshot.Idle;
            return Task.CompletedTask;
        }

        public Task TogglePauseAsync(CancellationToken cancellationToken)
        {
            ToggleCount++;
            snapshot = snapshot with { IsPlaying = snapshot.IsPaused, IsPaused = snapshot.IsPlaying };
            return Task.CompletedTask;
        }

        public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken)
        {
            snapshot = snapshot with { Position = position };
            return Task.CompletedTask;
        }

        public Task SetVolumeAsync(double volume, CancellationToken cancellationToken)
        {
            LastVolume = volume;
            return Task.CompletedTask;
        }

        public SoundPlaybackSnapshot GetSnapshot() => snapshot;

        public void AdvanceTo(TimeSpan position)
        {
            snapshot = snapshot with { Position = position };
        }

        public void AdvanceTo(TimeSpan position, TimeSpan duration)
        {
            snapshot = snapshot with { Position = position, Duration = duration };
        }

        public void Complete()
        {
            snapshot = SoundPlaybackSnapshot.Idle;
        }
    }
}
