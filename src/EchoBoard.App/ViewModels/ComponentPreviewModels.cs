using EchoBoard.App.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;

namespace EchoBoard.App.ViewModels;

public sealed class SoundCardPreviewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private string durationText;
    private bool isSelected;
    private bool isPlaying;
    private bool isPaused;
    private bool isFavorite;
    private bool isMissingFile;
    private string statusText;
    private string usageText;

    public SoundCardPreviewModel(
        string Title,
        string Subtitle,
        string DurationText,
        string HotkeyText,
        string CategoryLabel,
        Brush? CategoryBrush,
        bool IsSelected = false,
        bool IsPlaying = false,
        bool IsPaused = false,
        bool IsFavorite = false,
        bool IsCompact = false,
        bool IsEnabled = true,
        Guid Id = default,
        bool IsMissingFile = false,
        string StatusText = "",
        ICommand? SelectCommand = null,
        ICommand? FavoriteCommand = null,
        ICommand? AssignCategoryCommand = null,
        string FormatText = "",
        string UsageText = "",
        IReadOnlyList<WaveformBarViewModel>? WaveformBars = null,
        ICommand? DetailsCommand = null,
        ICommand? EditCommand = null,
        ICommand? DeleteCommand = null)
    {
        this.Title = Title;
        this.Subtitle = Subtitle;
        durationText = DurationText;
        this.HotkeyText = HotkeyText;
        this.CategoryLabel = CategoryLabel;
        this.CategoryBrush = CategoryBrush;
        isSelected = IsSelected;
        isPlaying = IsPlaying;
        isPaused = IsPaused;
        isFavorite = IsFavorite;
        this.IsCompact = IsCompact;
        this.IsEnabled = IsEnabled;
        this.Id = Id;
        isMissingFile = IsMissingFile;
        statusText = StatusText;
        this.SelectCommand = SelectCommand;
        this.FavoriteCommand = FavoriteCommand;
        this.AssignCategoryCommand = AssignCategoryCommand;
        this.FormatText = FormatText;
        usageText = UsageText;
        this.WaveformBars = WaveformBars;
        this.DetailsCommand = DetailsCommand;
        this.EditCommand = EditCommand;
        this.DeleteCommand = DeleteCommand;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string DurationText { get => durationText; set => SetProperty(ref durationText, value); }
    public string HotkeyText { get; }
    public string CategoryLabel { get; }
    public Brush? CategoryBrush { get; }
    public bool IsSelected { get => isSelected; set => SetProperty(ref isSelected, value); }
    public bool IsPlaying
    {
        get => isPlaying;
        set
        {
            if (SetProperty(ref isPlaying, value))
            {
                OnPropertyChanged(nameof(PlayGlyph));
                OnPropertyChanged(nameof(PlayLabel));
            }
        }
    }
    public bool IsPaused
    {
        get => isPaused;
        set
        {
            if (SetProperty(ref isPaused, value))
            {
                OnPropertyChanged(nameof(PlayGlyph));
                OnPropertyChanged(nameof(PlayLabel));
            }
        }
    }
    public bool IsFavorite
    {
        get => isFavorite;
        set
        {
            if (SetProperty(ref isFavorite, value))
            {
                OnPropertyChanged(nameof(FavoriteMenuText));
            }
        }
    }
    public bool IsCompact { get; }
    public bool IsEnabled { get; }
    public Guid Id { get; }
    public bool IsMissingFile { get => isMissingFile; set => SetProperty(ref isMissingFile, value); }
    public string StatusText { get => statusText; set => SetProperty(ref statusText, value); }
    public ICommand? SelectCommand { get; }
    public ICommand? FavoriteCommand { get; }
    public ICommand? AssignCategoryCommand { get; }
    public string FormatText { get; }
    public string UsageText { get => usageText; set => SetProperty(ref usageText, value); }
    public IReadOnlyList<WaveformBarViewModel>? WaveformBars { get; }
    public ICommand? DetailsCommand { get; }
    public ICommand? EditCommand { get; }
    public ICommand? DeleteCommand { get; }
    public string PlayGlyph => IsPlaying ? "\uE769" : "\uE768";
    public string PlayLabel => IsPlaying ? "Pausar" : IsPaused ? "Continuar" : "Reproduzir";
    public string FavoriteMenuText => IsFavorite ? "Remover dos favoritos" : "Adicionar aos favoritos";
    public bool HasWaveform => WaveformBars is { Count: > 0 };
}

public sealed record WaveformBarViewModel(double Height);

public sealed record CategoryPreviewModel(
    string Name,
    string CountText,
    Symbol Icon,
    Brush? IndicatorBrush,
    bool IsSelected = false,
    bool IsEnabled = true,
    Guid? Id = null,
    string FilterKind = "Category",
    ICommand? SelectCommand = null);

public sealed record DevicePreviewModel(
    string Label,
    string DeviceName,
    Symbol Icon,
    DeviceStatusKind Status);

public sealed record AudioMeterPreviewModel
{
    public AudioMeterPreviewModel(string label, double level, AudioLevelMeterVariant variant, string? valueText = null)
    {
        Label = label;
        Level = Math.Clamp(level, 0, 1);
        Variant = variant;
        ValueText = valueText ?? $"{Level:P0}";
    }

    public string Label { get; }

    public double Level { get; }

    public AudioLevelMeterVariant Variant { get; }

    public string ValueText { get; }
}

public sealed record VolumePreviewModel(
    string Label,
    Symbol Icon,
    double Value,
    bool IsReadOnly = true);

public sealed record EmptyStatePreviewModel(
    Symbol Icon,
    string Title,
    string Description,
    string PrimaryActionText,
    string SecondaryActionText = "");

public sealed record ToastPreviewModel(
    ToastNotificationKind Kind,
    string Title,
    string Description);
