using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EchoBoard.App.Controls;

public sealed partial class AudioLevelMeter : UserControl
{
    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(
        nameof(Level),
        typeof(double),
        typeof(AudioLevelMeter),
        new PropertyMetadata(0.0, OnDisplayPropertyChanged));

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant),
        typeof(AudioLevelMeterVariant),
        typeof(AudioLevelMeter),
        new PropertyMetadata(AudioLevelMeterVariant.Microphone, OnDisplayPropertyChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(AudioLevelMeter),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText),
        typeof(string),
        typeof(AudioLevelMeter),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public AudioLevelMeter()
    {
        InitializeComponent();
    }

    public double Level
    {
        get => (double)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public AudioLevelMeterVariant Variant
    {
        get => (AudioLevelMeterVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public double NormalizedLevel => Math.Clamp(Level, 0, 1);

    public GridLength FilledWidth => new(NormalizedLevel, GridUnitType.Star);

    public GridLength EmptyWidth => new(1 - NormalizedLevel, GridUnitType.Star);

    public string DisplayValue => string.IsNullOrWhiteSpace(ValueText) ? $"{NormalizedLevel:P0}" : ValueText;

    public string AccessibleLabel => $"{Label} level {DisplayValue}".Trim();

    public Brush MeterBrush => (Brush)Microsoft.UI.Xaml.Application.Current.Resources[Variant switch
    {
        AudioLevelMeterVariant.Microphone => "EchoBoardSuccessBrush",
        AudioLevelMeterVariant.Effects => "EchoBoardActionBrush",
        AudioLevelMeterVariant.Monitor => "EchoBoardWarningBrush",
        _ => "EchoBoardTextSecondaryBrush"
    }];

    private static void OnDisplayPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((AudioLevelMeter)dependencyObject).Bindings.Update();
    }
}
