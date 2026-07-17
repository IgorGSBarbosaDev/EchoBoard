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
    private SoundLibraryItemDto[] sounds = [];
    private SoundLibraryItemDto? currentSound;
    private string? currentFilePath;
    private TimeSpan currentDuration;
    private bool wasActive;
    private bool isRestarting;
    private bool isPlaying;
    private bool isPaused;
    private bool isRepeatEnabled;
    private double progressPercent;
    private double microphonePercent = 100;
    private double effectsPercent = 80;
    private double monitorPercent = 60;
    private string elapsedText = "0:00";
    private string durationText = "0:00";

    public PlaybackBarViewModel(
        ISoundPlaybackEngine playback,
        QuerySoundLibraryUseCase queryLibrary,
        GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot,
        SetMicrophoneGainUseCase setMicrophoneGain)
    {
        this.playback = playback;
        this.queryLibrary = queryLibrary;
        this.getMicrophoneSnapshot = getMicrophoneSnapshot;
        this.setMicrophoneGain = setMicrophoneGain;

        PlayPauseCommand = new AsyncRelayCommand(PlayPauseAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        StopAllCommand = new AsyncRelayCommand(StopAsync);
        PreviousCommand = new AsyncRelayCommand(ct => SkipAsync(-1, ct));
        NextCommand = new AsyncRelayCommand(ct => SkipAsync(1, ct));
        ToggleRepeatCommand = new RelayCommand(() => IsRepeatEnabled = !IsRepeatEnabled);
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
        get => microphonePercent;
        set
        {
            value = Math.Clamp(value, 0, 100);
            if (SetProperty(ref microphonePercent, value))
            {
                OnPropertyChanged(nameof(MicrophonePercentText));
                _ = SetMicrophoneGainAsync(value, CancellationToken.None);
            }
        }
    }

    public string MicrophonePercentText => $"{MicrophonePercent:0}%";

    public double EffectsPercent
    {
        get => effectsPercent;
        set
        {
            value = Math.Clamp(value, 0, 100);
            if (SetProperty(ref effectsPercent, value))
            {
                OnPropertyChanged(nameof(EffectsPercentText));
                _ = playback.SetVolumeAsync(value / 100.0, CancellationToken.None);
            }
        }
    }

    public string EffectsPercentText => $"{EffectsPercent:0}%";

    public double MonitorPercent
    {
        get => monitorPercent;
        set
        {
            if (SetProperty(ref monitorPercent, Math.Clamp(value, 0, 100)))
            {
                OnPropertyChanged(nameof(MonitorPercentText));
            }
        }
    }

    public string MonitorPercentText => $"{MonitorPercent:0}%";

    public IAsyncRelayCommand PlayPauseCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand StopAllCommand { get; }
    public IAsyncRelayCommand PreviousCommand { get; }
    public IAsyncRelayCommand NextCommand { get; }
    public IRelayCommand ToggleRepeatCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await queryLibrary.ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
        sounds = result.Sounds.Where(sound => !sound.IsMissingFile).OrderBy(sound => sound.SortOrder).ToArray();
        var microphone = getMicrophoneSnapshot.Execute();
        microphonePercent = Math.Clamp(microphone.Gain * 100.0, 0, 100);
        OnPropertyChanged(nameof(MicrophonePercent));
        OnPropertyChanged(nameof(MicrophonePercentText));
        Refresh();
    }

    public void Refresh()
    {
        var snapshot = playback.GetSnapshot();
        var active = snapshot.IsPlaying || snapshot.IsPaused;
        if (!string.IsNullOrWhiteSpace(snapshot.FilePath))
        {
            currentFilePath = snapshot.FilePath;
            currentDuration = snapshot.Duration;
            currentSound = sounds.FirstOrDefault(sound =>
                string.Equals(sound.FilePath, snapshot.FilePath, StringComparison.OrdinalIgnoreCase));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Metadata));
        }

        IsPlaying = snapshot.IsPlaying;
        IsPaused = snapshot.IsPaused;
        var duration = snapshot.Duration > TimeSpan.Zero ? snapshot.Duration : currentSound?.Duration ?? currentDuration;
        ProgressPercent = duration > TimeSpan.Zero ? snapshot.Position.TotalMilliseconds / duration.TotalMilliseconds * 100.0 : 0;
        ElapsedText = FormatTime(snapshot.Position);
        DurationText = FormatTime(duration);

        if (wasActive && !active && IsRepeatEnabled && currentFilePath is not null && !isRestarting)
        {
            _ = RestartAsync();
        }

        wasActive = active;
    }

    public Task SeekAsync(double percent, CancellationToken cancellationToken)
    {
        var duration = currentSound?.Duration ?? currentDuration;
        return duration <= TimeSpan.Zero
            ? Task.CompletedTask
            : playback.SeekAsync(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * Math.Clamp(percent, 0, 100) / 100.0), cancellationToken);
    }

    private async Task PlayPauseAsync(CancellationToken cancellationToken)
    {
        if (IsPlaying || IsPaused)
        {
            await playback.TogglePauseAsync(cancellationToken);
        }
        else if (currentFilePath is not null)
        {
            await playback.PlayAsync(currentFilePath, EffectsPercent / 100.0, cancellationToken);
        }

        Refresh();
    }

    private async Task StopAsync(CancellationToken cancellationToken)
    {
        await playback.StopAllAsync(cancellationToken);
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
        await playback.StopAllAsync(cancellationToken);
        await playback.PlayAsync(currentSound.FilePath, EffectsPercent / 100.0, cancellationToken);
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Metadata));
        Refresh();
    }

    private async Task RestartAsync()
    {
        isRestarting = true;
        try
        {
            await playback.PlayAsync(currentFilePath!, EffectsPercent / 100.0, CancellationToken.None);
            Refresh();
        }
        finally
        {
            isRestarting = false;
        }
    }

    private async Task SetMicrophoneGainAsync(double percent, CancellationToken cancellationToken)
    {
        await setMicrophoneGain.ExecuteAsync(percent / 100.0, cancellationToken);
    }

    private static string FormatTime(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
        : $"{(int)value.TotalMinutes}:{value.Seconds:00}";
}
