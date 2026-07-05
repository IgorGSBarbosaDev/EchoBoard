using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EchoBoard.App.Controls;

public sealed partial class EmptyState : UserControl
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(Symbol),
        typeof(EmptyState),
        new PropertyMetadata(Symbol.Audio, OnDisplayPropertyChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(EmptyState),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(EmptyState),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty PrimaryActionTextProperty = DependencyProperty.Register(
        nameof(PrimaryActionText),
        typeof(string),
        typeof(EmptyState),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty PrimaryCommandProperty = DependencyProperty.Register(
        nameof(PrimaryCommand),
        typeof(ICommand),
        typeof(EmptyState),
        new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryActionTextProperty = DependencyProperty.Register(
        nameof(SecondaryActionText),
        typeof(string),
        typeof(EmptyState),
        new PropertyMetadata(string.Empty, OnDisplayPropertyChanged));

    public static readonly DependencyProperty SecondaryCommandProperty = DependencyProperty.Register(
        nameof(SecondaryCommand),
        typeof(ICommand),
        typeof(EmptyState),
        new PropertyMetadata(null));

    public EmptyState()
    {
        InitializeComponent();
    }

    public Symbol Icon
    {
        get => (Symbol)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string PrimaryActionText
    {
        get => (string)GetValue(PrimaryActionTextProperty);
        set => SetValue(PrimaryActionTextProperty, value);
    }

    public ICommand? PrimaryCommand
    {
        get => (ICommand?)GetValue(PrimaryCommandProperty);
        set => SetValue(PrimaryCommandProperty, value);
    }

    public string SecondaryActionText
    {
        get => (string)GetValue(SecondaryActionTextProperty);
        set => SetValue(SecondaryActionTextProperty, value);
    }

    public ICommand? SecondaryCommand
    {
        get => (ICommand?)GetValue(SecondaryCommandProperty);
        set => SetValue(SecondaryCommandProperty, value);
    }

    public Visibility PrimaryActionVisibility => string.IsNullOrWhiteSpace(PrimaryActionText) ? Visibility.Collapsed : Visibility.Visible;

    public Visibility SecondaryActionVisibility => string.IsNullOrWhiteSpace(SecondaryActionText) ? Visibility.Collapsed : Visibility.Visible;

    private static void OnDisplayPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((EmptyState)dependencyObject).Bindings.Update();
    }
}
