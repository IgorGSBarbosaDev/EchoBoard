using EchoBoard.Application.Appearance;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace EchoBoard.App.Appearance;

public sealed class AppearanceResourceManager : IAppearanceResourceManager
{
    public void Apply(string palette, ElementTheme theme)
    {
        var normalizedPalette = AppearancePalettes.Normalize(palette);
        var themeName = theme == ElementTheme.Light ? AppearanceThemes.Light : AppearanceThemes.Dark;
        var resourcePrefix = $"EchoBoardPalette{normalizedPalette}{themeName}";

        SetBrushColor("EchoBoardActionBrush", GetColor($"{resourcePrefix}ActionColor"));
        SetBrushColor("EchoBoardActionHoverBrush", GetColor($"{resourcePrefix}HoverColor"));
        SetBrushColor("EchoBoardActionPressedBrush", GetColor($"{resourcePrefix}PressedColor"));
        SetBrushColor("EchoBoardFocusBrush", GetColor($"{resourcePrefix}HoverColor"));
        SetBrushColor("EchoBoardCardActiveBrush", GetColor($"{resourcePrefix}TintColor"));
        SetBrushColor("EchoBoardSelectedBrush", GetColor($"{resourcePrefix}TintColor"));
        SetBrushColor("NavigationViewItemForegroundSelected", GetColor($"{resourcePrefix}ActionColor"));
        SetBrushColor("NavigationViewItemForegroundSelectedPointerOver", GetColor($"{resourcePrefix}HoverColor"));
        SetBrushColor("NavigationViewItemBackgroundSelected", GetColor($"{resourcePrefix}TintColor"));
        SetBrushColor("NavigationViewItemBackgroundSelectedPointerOver", GetColor($"{resourcePrefix}TintColor"));
        SetBrushColor("NavigationViewItemBackgroundSelectedPressed", GetColor($"{resourcePrefix}TintColor"));
        SetBrushColor("NavigationViewSelectionIndicatorForeground", GetColor($"{resourcePrefix}ActionColor"));
    }

    private static Color GetColor(string key)
    {
        var resource = FindResource(Microsoft.UI.Xaml.Application.Current.Resources, key);
        return resource is Color color
            ? color
            : throw new InvalidOperationException($"Appearance color resource '{key}' was not found.");
    }

    private static void SetBrushColor(string key, Color color)
    {
        var resource = FindResource(Microsoft.UI.Xaml.Application.Current.Resources, key);
        if (resource is not SolidColorBrush brush)
        {
            throw new InvalidOperationException($"Appearance brush resource '{key}' was not found.");
        }

        brush.Color = color;
    }

    private static object? FindResource(ResourceDictionary dictionary, string key)
    {
        if (dictionary.TryGetValue(key, out var resource))
        {
            return resource;
        }

        foreach (var mergedDictionary in dictionary.MergedDictionaries.Reverse())
        {
            resource = FindResource(mergedDictionary, key);
            if (resource is not null)
            {
                return resource;
            }
        }

        return null;
    }
}
