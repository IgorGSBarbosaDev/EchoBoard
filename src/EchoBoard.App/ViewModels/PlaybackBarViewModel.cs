using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Library;

namespace EchoBoard.App.ViewModels;

public sealed class PlaybackBarViewModel : ObservableObject
{
    private readonly ISoundPlaybackEngine playback;
    private readonly QuerySoundLibraryUseCase queryLibrary;
    private readonly GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot;
    private readonly SetMicrophoneGainUseCase setMicrophoneGain;
    private readonly PlaybackCoordinator? playbackCoordinator;
    private readonly AudioRoutingSettingsCoordinator? audioSettings;
    private SoundLibraryItemDto[] sounds = [];
    private SoundLibraryItemDto? currentSound;
    private string? currentFilePath;
    private TimeSpan currentDuration;
    private bool wasActive;
    private bool isRestarting;
    private bool isPlaying;
    private bool isPaused;
    private bool isRepeatEnabled;
    private bool isUserSeeking;
    private double progressPercent;
    private double microphonePercent = 100;
    private double effectsPercent = 80;
    private double monitorPercent = 60;
    private double virtualOutputPercent = 100;
    private string elapsedText = "0:00";
    private string durationText = "0:00";

    public PlaybackBarViewModel(
        ISoundPlaybackEngine playback,
        QuerySoundLibraryUseCase queryLibrary,
        GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot,
        SetMicrophoneGainUseCase setMicrophoneGain,
        PlaybackCoordinator? playbackCoordinator = null,
        AudioRoutingSettingsCoordinator? audioSettings = null)
    {
        this.playback = playback;
        this.queryLibrary = queryLibrary;
        this.getMicrophoneSnapshot = getMicrophoneSnapshot;
        this.setMicrophoneGain = setMicrophoneGain;
        this.playbackCoordinator = playbackCoordinator;
        this.audioSettings = audioSettings;

        PlayPauseCommand = new AsyncRelayCommand(PlayPauseAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        PreviousCommand = new AsyncRelayCommand(ct => SkipAsync(-1, ct));
        NextCommand = new AsyncRelayCommand(ct => SkipAsync(1, ct));
        ToggleRepeatCommand = new RelayCommand(() => IsRepeatEnabled = !IsRepeatEnabled);

        if (playbackCoordinator is not null)
        {
            playbackCoordinator.SnapshotChanged += OnPlaybackSnapshotChanged;
        }

        if (audioSettings is not null)
        {
            audioSettings.PropertyChanged += OnAudioSettingsPropertyChanged;
        }
    }

    public string Title => currentSound?.Name
        ?? (currentFilePath is null ? "Nenhum som em reprodução" : Path.GetFileNameWithoutExtension(currentFilePath));

    public string Metadata => currentSound is not null
        ? $"{currentSound.CategoryName ?? "Sem categoria"} · {currentSound.Extension.TrimStart('.').ToUpperInvariant()}"
        : currentFilePath is null
            ? "Selecione um card ou use uma hotkey"
            : Path.GetExtension(currentFilePath).TrimStart('.').ToUpperInvariant();

    public bool IsPlaying
    {
        get => isPlaying;
        private set
        {
            if (SetProperty(ref isPlaying, value))
            {
                OnPropertyChanged(nameof(PlayPauseGlyph));
                OnPropertyChanged(nameof(PlayPauseLabel));
            }
        }
    }

    public bool IsPaused
    {
        get => isPaused;
        private set => SetProperty(ref isPaused, value);
    }

    public string PlayPauseGlyph => IsPlaying ? "\uE769" : "\uE768";
    public string PlayPauseLabel => IsPlaying ? "Pausar" : "Reproduzir";

    public bool IsRepeatEnabled
    {
        get => isRepeatEnabled;
        private set => SetProperty(ref isRepeatEnabled, value);
    }

    public double ProgressPercent
    {
        get => progressPercent;
        private set => SetProperty(ref progressPercent, Math.Clamp(value, 0, 100));
    }

    public string ElapsedText
    {
        get => elapsedText;
        private set => SetProperty(ref elapsedText, value);
    }

    public string DurationText
    {
        get => durationText;
        private set => SetProperty(ref durationText, value);
    }

    public double MicrophonePercent
    {
        get => audioSettings?.MicrophonePercent ?? microphonePercent;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.MicrophonePercent = value;
            }
            else if (SetProperty(ref microphonePercent, Math.Clamp(value, 0, 100)))
            {
                OnPropertyChanged(nameof(MicrophonePercentText));
                _ = setMicrophoneGain.ExecuteAsync(microphonePercent / 100.0, CancellationToken.None);
            }
        }
    }

    public double EffectsPercent
    {
        get => audioSettings?.EffectsPercent ?? effectsPercent;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.EffectsPercent = value;
            }
            else if (SetProperty(ref effectsPercent, Math.Clamp(value, 0, 100)))
            {
                OnPropertyChanged(nameof(EffectsPercentText));
                _ = playback.SetVolumeAsync(effectsPercent / 100.0, CancellationToken.None);
            }
        }
    }

    public double MonitorPercent
    {
        get => audioSettings?.MonitorPercent ?? monitorPercent;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.MonitorPercent = value;
            }
            else if (SetProperty(ref monitorPercent, Math.Clamp(value, 0, 100)))
            {
                OnPropertyChanged(nameof(MonitorPercentText));
            }
        }
    }

    public double VirtualOutputPercent
    {
        get => audioSettings?.VirtualOutputPercent ?? virtualOutputPercent;
        set
        {
            if (audioSettings is not null)
            {
                audioSettings.VirtualOutputPercent = value;
            }
            else if (SetProperty(ref virtualOutputPercent, Math.Clamp(value, 0, 100)))
            {
                OnPropertyChanged(nameof(VirtualOutputPercentText));
            }
        }
    }

    public bool IsMicrophoneMuted => audioSettings?.IsMicrophoneMuted ?? false;
    public bool AreEffectsMuted => audioSettings?.AreEffectsMuted ?? false;
    public bool IsMonitorMuted => audioSettings?.IsMonitorMuted ?? false;
    public bool IsVirtualOutputMuted => audioSettings?.IsVirtualOutputMuted ?? false;
    public string MicrophonePercentText => $"{MicrophonePercent:0}%";
    public string EffectsPercentText => $"{EffectsPercent:0}%";
    public string MonitorPercentText => $"{MonitorPercent:0}%";
    public string VirtualOutputPercentText => $"{VirtualOutputPercent:0}%";

    public IAsyncRelayCommand PlayPauseCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand PreviousCommand { get; }
    public IAsyncRelayCommand NextCommand { get; }
    public IRelayCommand ToggleRepeatCommand { get; }
    public IRelayCommand? ToggleMicrophoneMuteCommand => audioSettings?.ToggleMicrophoneMuteCommand;
    public IRelayCommand? ToggleEffectsMuteCommand => audioSettings?.ToggleEffectsMuteCommand;
    public IRelayCommand? ToggleMonitorMuteCommand => audioSettings?.ToggleMonitorMuteCommand;
    public IRelayCommand? ToggleVirtualOutputMuteCommand => audioSettings?.ToggleVirtualOutputMuteCommand;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await queryLibrary.ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
        sounds = result.Sounds.Where(sound => !sound.IsMissingFile).OrderBy(sound => sound.SortOrder).ToArray();
        foreach (var sound in sounds)
        {
            playbackCoordinator?.TrackSound(sound.Id, sound.FilePath);
        }

        if (audioSettings is not null)
        {
            await audioSettings.LoadAsync(cancellationToken);
            NotifyAudioSettingsChanged();
        }
        else
        {
            microphonePercent = Math.Clamp(getMicrophoneSnapshot.Execute().Gain * 100.0, 0, 100);
            OnPropertyChanged(nameof(MicrophonePercent));
            OnPropertyChanged(nameof(MicrophonePercentText));
        }

        ApplySnapshot(playbackCoordinator?.Snapshot ?? playback.GetSnapshot());
    }

    public void Refresh()
    {
        ApplySnapshot(playbackCoordinator?.Snapshot ?? playback.GetSnapshot());
    }

    public void BeginSeek() => isUserSeeking = true;

    public async Task CommitSeekAsync(double percent, CancellationToken cancellationToken)
    {
        isUserSeeking = false;
        var duration = currentDuration;
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        var position = TimeSpan.FromMilliseconds(duration.TotalMilliseconds * Math.Clamp(percent, 0, 100) / 100.0);
        if (playbackCoordinator is not null)
        {
            await playbackCoordinator.SeekAsync(position, cancellationToken);
        }
        else
        {
            await playback.SeekAsync(position, cancellationToken);
        }

        ApplySnapshot(playbackCoordinator?.Snapshot ?? playback.GetSnapshot());
    }

    public Task SeekAsync(double percent, CancellationToken cancellationToken) =>
        CommitSeekAsync(percent, cancellationToken);

    private void ApplySnapshot(SoundPlaybackSnapshot snapshot)
    {
        var active = snapshot.IsPlaying || snapshot.IsPaused;
        if (!string.IsNullOrWhiteSpace(snapshot.FilePath))
        {
            var changedFile = !string.Equals(currentFilePath, snapshot.FilePath, StringComparison.OrdinalIgnoreCase);
            currentFilePath = snapshot.FilePath;
            currentDuration = snapshot.Duration;
            currentSound = sounds.FirstOrDefault(sound =>
                string.Equals(sound.FilePath, snapshot.FilePath, StringComparison.OrdinalIgnoreCase));
            if (changedFile)
            {
                ProgressPercent = 0;
                ElapsedText = FormatTime(TimeSpan.Zero);
            }
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Metadata));
        }

        IsPlaying = snapshot.IsPlaying;
        IsPaused = snapshot.IsPaused;
        var duration = snapshot.Duration > TimeSpan.Zero ? snapshot.Duration : currentSound?.Duration ?? currentDuration;
        currentDuration = duration;
        if (!isUserSeeking)
        {
            ProgressPercent = duration > TimeSpan.Zero
                ? snapshot.Position.TotalMilliseconds / duration.TotalMilliseconds * 100.0
                : 0;
        }
        ElapsedText = FormatTime(snapshot.Position);
        DurationText = FormatTime(duration);

        if (wasActive && !active && IsRepeatEnabled && currentFilePath is not null && !isRestarting)
        {
            _ = RestartAsync();
        }

        wasActive = active;
    }

    private async Task PlayPauseAsync(CancellationToken cancellationToken)
    {
        if (IsPlaying || IsPaused)
        {
            if (playbackCoordinator is not null)
            {
                await playbackCoordinator.TogglePauseAsync(cancellationToken);
            }
            else
            {
                await playback.TogglePauseAsync(cancellationToken);
            }
        }
        else if (currentSound is not null && playbackCoordinator is not null)
        {
            await playbackCoordinator.PlayAsync(currentSound.Id, cancellationToken);
        }
        else if (currentFilePath is not null)
        {
            await playback.PlayAsync(currentFilePath, EffectsPercent / 100.0, cancellationToken);
        }

        Refresh();
    }

    private async Task StopAsync(CancellationToken cancellationToken)
    {
        if (playbackCoordinator is not null)
        {
            await playbackCoordinator.StopAsync(cancellationToken);
        }
        else
        {
            await playback.StopAllAsync(cancellationToken);
        }
        Refresh();
    }

    private async Task SkipAsync(int offset, CancellationToken cancellationToken)
    {
        if (sounds.Length == 0)
        {
            return;
        }

        var index = currentSound is null ? 0 : sounds.ToList().FindIndex(sound => sound.Id == currentSound.Id);
        index = (index + offset + sounds.Length) % sounds.Length;
        currentSound = sounds[index];
        currentFilePath = currentSound.FilePath;
        currentDuration = currentSound.Duration;
        ProgressPercent = 0;
        ElapsedText = FormatTime(TimeSpan.Zero);
        DurationText = FormatTime(currentDuration);
        if (playbackCoordinator is not null)
        {
            await playbackCoordinator.PlayAsync(currentSound.Id, cancellationToken);
        }
        else
        {
            await playback.StopAllAsync(cancellationToken);
            await playback.PlayAsync(currentSound.FilePath, EffectsPercent / 100.0, cancellationToken);
        }
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Metadata));
        Refresh();
    }

    private async Task RestartAsync()
    {
        isRestarting = true;
        try
        {
            if (currentSound is not null && playbackCoordinator is not null)
            {
                await playbackCoordinator.PlayAsync(currentSound.Id, CancellationToken.None);
            }
            else
            {
                await playback.PlayAsync(currentFilePath!, EffectsPercent / 100.0, CancellationToken.None);
            }
            Refresh();
        }
        finally
        {
            isRestarting = false;
        }
    }

    private void OnPlaybackSnapshotChanged(object? sender, PlaybackSnapshotChange e) => ApplySnapshot(e.Snapshot);

    private void OnAudioSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e) => NotifyAudioSettingsChanged();

    private void NotifyAudioSettingsChanged()
    {
        foreach (var propertyName in new[]
                 {
                     nameof(MicrophonePercent), nameof(EffectsPercent), nameof(MonitorPercent),
                     nameof(VirtualOutputPercent), nameof(MicrophonePercentText), nameof(EffectsPercentText),
                     nameof(MonitorPercentText), nameof(VirtualOutputPercentText), nameof(IsMicrophoneMuted),
                     nameof(AreEffectsMuted), nameof(IsMonitorMuted), nameof(IsVirtualOutputMuted)
                 })
        {
            OnPropertyChanged(propertyName);
        }
    }

    private static string FormatTime(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
        : $"{(int)value.TotalMinutes}:{value.Seconds:00}";
}
