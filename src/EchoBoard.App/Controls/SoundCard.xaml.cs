using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EchoBoard.App.Controls;

public sealed partial class SoundCard : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(SoundCard),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(SoundCard),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty DurationTextProperty = DependencyProperty.Register(
        nameof(DurationText),
        typeof(string),
        typeof(SoundCard),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty HotkeyTextProperty = DependencyProperty.Register(
        nameof(HotkeyText),
        typeof(string),
        typeof(SoundCard),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty CategoryLabelProperty = DependencyProperty.Register(
        nameof(CategoryLabel),
        typeof(string),
        typeof(SoundCard),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty CategoryBrushProperty = DependencyProperty.Register(
        nameof(CategoryBrush),
        typeof(Brush),
        typeof(SoundCard),
        new PropertyMetadata(null, OnDisplayPropertyChanged));

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected),
        typeof(bool),
        typeof(SoundCard),
        new PropertyMetadata(false, OnDisplayPropertyChanged));

    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
        nameof(IsPlaying),
        typeof(bool),
        typeof(SoundCard),
        new PropertyMetadata(false, OnDisplayPropertyChanged));

    public static readonly DependencyProperty IsFavoriteProperty = DependencyProperty.Register(
        nameof(IsFavorite),
        typeof(bool),
        typeof(SoundCard),
        new PropertyMetadata(false, OnDisplayPropertyChanged));

    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
        nameof(IsCompact),
        typeof(bool),
        typeof(SoundCard),
        new PropertyMetadata(false, OnDisplayPropertyChanged));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(SoundCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter),
        typeof(object),
        typeof(SoundCard),
        new PropertyMetadata(null));

    public SoundCard()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string DurationText
    {
        get => (string)GetValue(DurationTextProperty);
        set => SetValue(DurationTextProperty, value);
    }

    public string HotkeyText
    {
        get => (string)GetValue(HotkeyTextProperty);
        set => SetValue(HotkeyTextProperty, value);
    }

    public string CategoryLabel
    {
        get => (string)GetValue(CategoryLabelProperty);
        set => SetValue(CategoryLabelProperty, value);
    }

    public Brush? CategoryBrush
    {
        get => (Brush?)GetValue(CategoryBrushProperty);
        set => SetValue(CategoryBrushProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public bool IsFavorite
    {
        get => (bool)GetValue(IsFavoriteProperty);
        set => SetValue(IsFavoriteProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public Brush CardBackground => (Brush)Microsoft.UI.Xaml.Application.Current.Resources[IsSelected || IsPlaying ? "EchoBoardCardActiveBrush" : "EchoBoardCardBrush"];

    public Brush CardBorderBrush => (Brush)Microsoft.UI.Xaml.Application.Current.Resources[IsPlaying ? "EchoBoardSuccessBrush" : IsSelected ? "EchoBoardActionBrush" : "EchoBoardBorderBrush"];

    public double CardMinHeight => IsCompact ? 116 : 168;

    public bool HotkeyUnavailable => string.IsNullOrWhiteSpace(HotkeyText);

    public Visibility FavoriteVisibility => IsFavorite ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PlayingVisibility => IsPlaying ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SubtitleVisibility => IsCompact || string.IsNullOrWhiteSpace(Subtitle) ? Visibility.Collapsed : Visibility.Visible;

    public Visibility DurationVisibility => string.IsNullOrWhiteSpace(DurationText) ? Visibility.Collapsed : Visibility.Visible;

    public string AccessibleLabel => $"{Title} {CategoryLabel} {DurationText}".Trim();

    public Brush DisplayCategoryBrush => CategoryBrush ?? (Brush)Microsoft.UI.Xaml.Application.Current.Resources["EchoBoardActionBrush"];

    private static void OnDisplayPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((SoundCard)dependencyObject).Bindings.Update();
    }
}
