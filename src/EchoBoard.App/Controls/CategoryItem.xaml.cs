using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EchoBoard.App.Controls;

public sealed partial class CategoryItem : UserControl
{
    public static readonly DependencyProperty CategoryNameProperty = DependencyProperty.Register(
        nameof(CategoryName),
        typeof(string),
        typeof(CategoryItem),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty CountTextProperty = DependencyProperty.Register(
        nameof(CountText),
        typeof(string),
        typeof(CategoryItem),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(Symbol),
        typeof(CategoryItem),
        new PropertyMetadata(Symbol.Library, OnDisplayPropertyChanged));

    public static readonly DependencyProperty IndicatorBrushProperty = DependencyProperty.Register(
        nameof(IndicatorBrush),
        typeof(Brush),
        typeof(CategoryItem),
        new PropertyMetadata(null, OnDisplayPropertyChanged));

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected),
        typeof(bool),
        typeof(CategoryItem),
        new PropertyMetadata(false, OnDisplayPropertyChanged));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(CategoryItem),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter),
        typeof(object),
        typeof(CategoryItem),
        new PropertyMetadata(null));

    public CategoryItem()
    {
        InitializeComponent();
    }

    public string CategoryName
    {
        get => (string)GetValue(CategoryNameProperty);
        set => SetValue(CategoryNameProperty, value);
    }

    public string CountText
    {
        get => (string)GetValue(CountTextProperty);
        set => SetValue(CountTextProperty, value);
    }

    public Symbol Icon
    {
        get => (Symbol)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Brush? IndicatorBrush
    {
        get => (Brush?)GetValue(IndicatorBrushProperty);
        set => SetValue(IndicatorBrushProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
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

    public Visibility CountVisibility => string.IsNullOrWhiteSpace(CountText) ? Visibility.Collapsed : Visibility.Visible;

    public string AccessibleLabel => $"{CategoryName} category {CountText}".Trim();

    public Brush DisplayIndicatorBrush => IndicatorBrush ?? (Brush)Microsoft.UI.Xaml.Application.Current.Resources["EchoBoardActionBrush"];

    public Brush ItemBackground => (Brush)Microsoft.UI.Xaml.Application.Current.Resources[IsSelected ? "EchoBoardSelectedBrush" : "EchoBoardCardBrush"];

    public Brush ItemBorderBrush => (Brush)Microsoft.UI.Xaml.Application.Current.Resources[IsSelected ? "EchoBoardActionBrush" : "EchoBoardBorderBrush"];

    private static void OnDisplayPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((CategoryItem)dependencyObject).Bindings.Update();
    }
}
