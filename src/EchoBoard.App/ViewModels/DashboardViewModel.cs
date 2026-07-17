using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EchoBoard.App.Controls;
using EchoBoard.App.Navigation;
using EchoBoard.Application.Audio;
using EchoBoard.Application.Hotkeys;
using EchoBoard.Application.Library;
using EchoBoard.Domain.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly QuerySoundLibraryUseCase queryLibrary;
    private readonly ImportSoundsUseCase importSounds;
    private readonly SetSoundFavoriteUseCase setSoundFavorite;
    private readonly GenerateSoundWaveformUseCase generateWaveform;
    private readonly ListHotkeyBindingsUseCase listHotkeys;
    private readonly GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot;
    private readonly PlaySoundUseCase playSound;
    private readonly SoundDetailsViewModel details;
    private string libraryValue = "0 sons";
    private string libraryNote = "Nenhuma categoria organizada";
    private string hotkeyValue = "0 ativas";
    private string hotkeyNote = "Nenhum conflito detectado";
    private string microphoneValue = "Não configurado";
    private string microphoneNote = "Selecione uma entrada de áudio";
    private string feedbackMessage = string.Empty;
    private double microphoneLevel;
    private string microphoneLevelText = "Inativo";

    public DashboardViewModel(
        QuerySoundLibraryUseCase queryLibrary,
        ImportSoundsUseCase importSounds,
        SetSoundFavoriteUseCase setSoundFavorite,
        GenerateSoundWaveformUseCase generateWaveform,
        ListHotkeyBindingsUseCase listHotkeys,
        GetMicrophoneCaptureSnapshotUseCase getMicrophoneSnapshot,
        PlaySoundUseCase playSound,
        SoundDetailsViewModel details,
        INavigationService navigation)
    {
        this.queryLibrary = queryLibrary;
        this.importSounds = importSounds;
        this.setSoundFavorite = setSoundFavorite;
        this.generateWaveform = generateWaveform;
        this.listHotkeys = listHotkeys;
        this.getMicrophoneSnapshot = getMicrophoneSnapshot;
        this.playSound = playSound;
        this.details = details;

        QuickSounds = [];
        SetupSteps = [];
        OpenSettingsCommand = new RelayCommand(() => navigation.NavigateTo(ShellRoute.Settings));
        OpenLibraryCommand = new RelayCommand(() => navigation.NavigateTo(ShellRoute.Library));
        details.SoundChanged += OnSoundChanged;
    }

    public string LibraryValue { get => libraryValue; private set => SetProperty(ref libraryValue, value); }
    public string LibraryNote { get => libraryNote; private set => SetProperty(ref libraryNote, value); }
    public string HotkeyValue { get => hotkeyValue; private set => SetProperty(ref hotkeyValue, value); }
    public string HotkeyNote { get => hotkeyNote; private set => SetProperty(ref hotkeyNote, value); }
    public string MicrophoneValue { get => microphoneValue; private set => SetProperty(ref microphoneValue, value); }
    public string MicrophoneNote { get => microphoneNote; private set => SetProperty(ref microphoneNote, value); }
    public string RoutingValue => "Pendente";
    public string RoutingNote => "Saída virtual ainda não implementada";

    public ObservableCollection<SoundCardPreviewModel> QuickSounds { get; }
    public ObservableCollection<DashboardSetupStepViewModel> SetupSteps { get; }
    public IRelayCommand OpenSettingsCommand { get; }
    public IRelayCommand OpenLibraryCommand { get; }

    public Visibility QuickSoundsVisibility => QuickSounds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility QuickEmptyVisibility => QuickSounds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public double MicrophoneLevel
    {
        get => microphoneLevel;
        private set => SetProperty(ref microphoneLevel, Math.Clamp(value, 0, 1));
    }

    public string MicrophoneLevelText
    {
        get => microphoneLevelText;
        private set => SetProperty(ref microphoneLevelText, value);
    }

    public string EffectsLevelText => "Sem telemetria";
    public string VirtualOutputLevelText => "Indisponível";

    public string FeedbackMessage
    {
        get => feedbackMessage;
        private set
        {
            if (SetProperty(ref feedbackMessage, value))
            {
                OnPropertyChanged(nameof(FeedbackVisibility));
            }
        }
    }

    public Visibility FeedbackVisibility => string.IsNullOrWhiteSpace(FeedbackMessage) ? Visibility.Collapsed : Visibility.Visible;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var library = await queryLibrary.ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
        var waveformCandidates = library.Sounds
            .Where(sound => !sound.IsMissingFile && sound.WaveformPeaks.Length == 0)
            .OrderByDescending(sound => sound.IsFavorite)
            .ThenByDescending(sound => sound.PlayCount)
            .Take(4)
            .ToArray();
        foreach (var sound in waveformCandidates)
        {
            try
            {
                await generateWaveform.ExecuteAsync(sound.Id, cancellationToken);
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException or AudioFileUnreadableException or AudioFileMetadataException)
            {
                // The card keeps the explicit unavailable state when decoding fails.
            }
        }

        if (waveformCandidates.Length > 0)
        {
            library = await queryLibrary.ExecuteAsync(SoundLibraryFilter.All, cancellationToken);
        }

        var hotkeys = await listHotkeys.ExecuteAsync(cancellationToken);
        var microphone = getMicrophoneSnapshot.Execute();

        LibraryValue = $"{library.TotalSoundCount} {(library.TotalSoundCount == 1 ? "som" : "sons")}";
        LibraryNote = library.Categories.Count == 0
            ? "Nenhuma categoria organizada"
            : $"{library.Categories.Count} {(library.Categories.Count == 1 ? "categoria organizada" : "categorias organizadas")}";

        var activeHotkeys = hotkeys.Count(binding => binding.IsEnabled && binding.RegistrationState == HotkeyRegistrationState.Active);
        var conflicts = hotkeys.Count(binding => binding.RegistrationState == HotkeyRegistrationState.Conflicting);
        HotkeyValue = $"{activeHotkeys} ativas";
        HotkeyNote = conflicts == 0 ? "Nenhum conflito detectado" : $"{conflicts} em conflito";

        ApplyMicrophone(microphone);
        ReplaceQuickSounds(library.Sounds, hotkeys);
        ReplaceSetupSteps(library.TotalSoundCount, activeHotkeys, microphone);
    }

    public void RefreshLiveState()
    {
        ApplyMicrophone(getMicrophoneSnapshot.Execute());
    }

    public async Task ImportFilePathsAsync(IReadOnlyList<string> filePaths, CancellationToken cancellationToken)
    {
        if (filePaths.Count == 0)
        {
            return;
        }

        var result = await importSounds.ExecuteAsync(new ImportSoundsRequest(filePaths, DateTimeOffset.UtcNow), cancellationToken);
        var imported = result.Items.Count(item => item.Status == ImportSoundStatus.Imported);
        FeedbackMessage = imported == 0 ? "Nenhum som foi importado." : $"{imported} {(imported == 1 ? "som importado" : "sons importados")}.";
        await LoadAsync(cancellationToken);
    }

    private void ReplaceQuickSounds(IReadOnlyList<SoundLibraryItemDto> sounds, IReadOnlyList<HotkeyBindingDto> hotkeys)
    {
        var hotkeyBySoundId = hotkeys
            .Where(binding => binding.SoundId is not null)
            .ToDictionary(binding => binding.SoundId!.Value);
        QuickSounds.Clear();
        foreach (var sound in sounds
                     .OrderByDescending(sound => sound.IsFavorite)
                     .ThenByDescending(sound => sound.PlayCount)
                     .ThenBy(sound => sound.SortOrder)
                     .Take(4))
        {
            hotkeyBySoundId.TryGetValue(sound.Id, out var hotkey);
            QuickSounds.Add(ToCard(sound, hotkey));
        }

        OnPropertyChanged(nameof(QuickSoundsVisibility));
        OnPropertyChanged(nameof(QuickEmptyVisibility));
    }

    private SoundCardPreviewModel ToCard(SoundLibraryItemDto sound, HotkeyBindingDto? hotkey)
    {
        return new SoundCardPreviewModel(
            sound.Name,
            string.Empty,
            FormatDuration(sound.Duration),
            hotkey?.NormalizedKeyCombination ?? "Sem hotkey",
            sound.CategoryName ?? "Sem categoria",
            null,
            IsFavorite: sound.IsFavorite,
            Id: sound.Id,
            IsMissingFile: sound.IsMissingFile,
            StatusText: sound.IsMissingFile ? "Arquivo ausente" : "Pronto",
            SelectCommand: new AsyncRelayCommand(_ => PlayAsync(sound.Id, CancellationToken.None)),
            FavoriteCommand: new AsyncRelayCommand(_ => ToggleFavoriteAsync(sound.Id, sound.IsFavorite, CancellationToken.None)),
            FormatText: sound.Extension.TrimStart('.').ToUpperInvariant(),
            UsageText: $"{sound.PlayCount} {(sound.PlayCount == 1 ? "uso" : "usos")}",
            WaveformBars: ToWaveform(sound.WaveformPeaks),
            DetailsCommand: details.OpenCommand,
            EditCommand: details.OpenEditCommand);
    }

    private async Task PlayAsync(Guid soundId, CancellationToken cancellationToken)
    {
        try
        {
            await playSound.ExecuteAsync(new PlaySoundRequest(soundId, DateTimeOffset.UtcNow), cancellationToken);
            FeedbackMessage = string.Empty;
            await LoadAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            FeedbackMessage = exception.Message;
        }
    }

    private async Task ToggleFavoriteAsync(Guid soundId, bool isFavorite, CancellationToken cancellationToken)
    {
        await setSoundFavorite.ExecuteAsync(new SetSoundFavoriteRequest(soundId, !isFavorite, DateTimeOffset.UtcNow), cancellationToken);
        await LoadAsync(cancellationToken);
    }

    private void ReplaceSetupSteps(int soundCount, int activeHotkeys, MicrophoneCaptureSnapshot microphone)
    {
        SetupSteps.Clear();
        SetupSteps.Add(new("Importar sons", soundCount > 0 ? $"{soundCount} disponíveis" : "Adicione arquivos locais", soundCount > 0, false));
        SetupSteps.Add(new("Selecionar microfone", microphone.SelectedDeviceId is null ? "Nenhum dispositivo selecionado" : microphone.SelectedDeviceName ?? "Selecionado", microphone.SelectedDeviceId is not null, false));
        SetupSteps.Add(new("Definir hotkeys", activeHotkeys > 0 ? $"{activeHotkeys} registradas" : "Nenhum atalho ativo", activeHotkeys > 0, false));
        SetupSteps.Add(new("Selecionar saída virtual", "Recurso ainda não implementado", false, true));
    }

    private void ApplyMicrophone(MicrophoneCaptureSnapshot snapshot)
    {
        MicrophoneValue = snapshot.SelectedDeviceId is null
            ? "Não configurado"
            : snapshot.State == MicrophoneCaptureState.Active ? "Ativo" : "Pronto";
        MicrophoneNote = snapshot.SelectedDeviceName ?? "Selecione uma entrada de áudio";
        MicrophoneLevel = snapshot.State == MicrophoneCaptureState.Active && !snapshot.IsMuted ? snapshot.Level : 0;
        MicrophoneLevelText = snapshot.IsMuted ? "Mudo" : snapshot.State == MicrophoneCaptureState.Active ? $"{snapshot.Level:P0}" : "Inativo";
    }

    private async void OnSoundChanged(object? sender, EventArgs e)
    {
        await LoadAsync(CancellationToken.None);
    }

    private static WaveformBarViewModel[] ToWaveform(byte[] peaks) => peaks.Length == 32
        ? peaks.Select(peak => new WaveformBarViewModel(6 + peak / 255.0 * 28)).ToArray()
        : [];

    private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1
        ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
        : $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
}

public sealed record DashboardSetupStepViewModel(string Title, string Description, bool IsComplete, bool IsUnavailable)
{
    public Symbol Icon => IsComplete ? Symbol.Accept : IsUnavailable ? Symbol.Important : Symbol.Clock;
    public string StatusText => IsComplete ? "Concluído" : IsUnavailable ? "Indisponível" : "Pendente";
}
