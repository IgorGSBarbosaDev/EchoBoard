using EchoBoard.App.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EchoBoard.App.ViewModels;

public sealed record SoundCardPreviewModel(
    string Title,
    string Subtitle,
    string DurationText,
    string HotkeyText,
    string CategoryLabel,
    Brush? CategoryBrush,
    bool IsSelected = false,
    bool IsPlaying = false,
    bool IsFavorite = false,
    bool IsCompact = false,
    bool IsEnabled = true);

public sealed record CategoryPreviewModel(
    string Name,
    string CountText,
    Symbol Icon,
    Brush? IndicatorBrush,
    bool IsSelected = false,
    bool IsEnabled = true);

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
