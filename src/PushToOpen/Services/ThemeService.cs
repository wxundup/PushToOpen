using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using PushToOpen.Models;
using Windows.UI;

namespace PushToOpen.Services;

public sealed class ThemeService : IThemeService
{
    private ThemeDefinition _current;

    public ThemeService()
    {
        Themes = BuildThemes();
        _current = Themes[0];
    }

    public IReadOnlyList<ThemeDefinition> Themes { get; }
    public ThemeDefinition Current => _current;
    public event EventHandler<ThemeDefinition>? ThemeChanged;

    public ThemeDefinition? Find(string name)
        => Themes.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

    public void Apply(string name)
    {
        var t = Find(name);
        if (t is null) return;
        _current = t;
        ApplyToResources(t);
        try { ThemeChanged?.Invoke(this, t); } catch { }
    }

    private static void ApplyToResources(ThemeDefinition t)
    {
        var res = Application.Current.Resources;
        foreach (var kv in t.Colors)
        {
            var brushKey = kv.Key + "Brush";
            if (res.TryGetValue(brushKey, out var obj) && obj is SolidColorBrush brush)
            {
                brush.Color = ParseHex(kv.Value);
            }
        }

        // Accent gradient brush — mutate its two stops in place.
        if (res.TryGetValue("AccentGradient", out var gObj) && gObj is LinearGradientBrush grad
            && grad.GradientStops.Count >= 2)
        {
            grad.GradientStops[0].Color = ParseHex(t.AccentGradient.Start);
            grad.GradientStops[1].Color = ParseHex(t.AccentGradient.End);
        }
    }

    private static Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        byte a = 0xFF, r, g, b;
        if (hex.Length == 8)
        {
            a = byte.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber);
            r = byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex[6..8], System.Globalization.NumberStyles.HexNumber);
        }
        else
        {
            r = byte.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber);
        }
        return Color.FromArgb(a, r, g, b);
    }

    private static ThemeDefinition[] BuildThemes() => new[]
    {
        new ThemeDefinition
        {
            Name = "Indigo Night",
            Description = "Default — deep cool greys with violet accent.",
            AccentGradient = ("#7B61FF", "#5A8FFF"),
            Colors = new()
            {
                ["SurfaceBase"]      = "#0F1115",
                ["SurfaceRaised"]    = "#161922",
                ["SurfaceFloating"]  = "#1D212C",
                ["SurfaceHover"]     = "#252A38",
                ["SurfacePanel"]     = "#1A1D27",
                ["SurfaceElevated"]  = "#222633",
                ["Divider"]          = "#252938",
                ["DividerBright"]    = "#323849",
                ["RuleLine"]         = "#1C202B",
                ["TextPrimary"]      = "#F1F3F8",
                ["TextSecondary"]    = "#A4ACBE",
                ["TextTertiary"]     = "#6B7385",
                ["TextMuted"]        = "#3D4456",
                ["AccentPrimary"]    = "#7B61FF",
                ["AccentHover"]      = "#8E78FF",
                ["AccentDim"]        = "#4A3AAB",
                ["AccentSoft"]       = "#2A2447",
                ["GateOpen"]         = "#3DDC97",
                ["GateClosed"]       = "#4A5266",
                ["Warn"]             = "#F5C842",
                ["Error"]            = "#FF5C7A",
            },
        },
        new ThemeDefinition
        {
            Name = "Phosphor",
            Description = "Mono-green CRT — vintage broadcast terminal.",
            AccentGradient = ("#22FF6A", "#0AC050"),
            Colors = new()
            {
                ["SurfaceBase"]      = "#070A07",
                ["SurfaceRaised"]    = "#0C1310",
                ["SurfaceFloating"]  = "#101A14",
                ["SurfaceHover"]     = "#16241B",
                ["SurfacePanel"]     = "#0A140E",
                ["SurfaceElevated"]  = "#162820",
                ["Divider"]          = "#1B2A22",
                ["DividerBright"]    = "#2A4034",
                ["RuleLine"]         = "#10180F",
                ["TextPrimary"]      = "#D7FFE2",
                ["TextSecondary"]    = "#7AC290",
                ["TextTertiary"]     = "#4A7E5C",
                ["TextMuted"]        = "#2A4A35",
                ["AccentPrimary"]    = "#22FF6A",
                ["AccentHover"]      = "#54FF8B",
                ["AccentDim"]        = "#0A7A2A",
                ["AccentSoft"]       = "#0F2418",
                ["GateOpen"]         = "#22FF6A",
                ["GateClosed"]       = "#2A4034",
                ["Warn"]             = "#F0B840",
                ["Error"]            = "#FF5060",
            },
        },
        new ThemeDefinition
        {
            Name = "Crimson",
            Description = "Hot accent on warm-shadowed charcoal.",
            AccentGradient = ("#FF4D5E", "#E03050"),
            Colors = new()
            {
                ["SurfaceBase"]      = "#0F0B0D",
                ["SurfaceRaised"]    = "#1A1316",
                ["SurfaceFloating"]  = "#221A1E",
                ["SurfaceHover"]     = "#2E2128",
                ["SurfacePanel"]     = "#1B1418",
                ["SurfaceElevated"]  = "#28202A",
                ["Divider"]          = "#2C2128",
                ["DividerBright"]    = "#3D2A33",
                ["RuleLine"]         = "#1A1418",
                ["TextPrimary"]      = "#FBF1F2",
                ["TextSecondary"]    = "#BBA6AC",
                ["TextTertiary"]     = "#7C6770",
                ["TextMuted"]        = "#4B3A40",
                ["AccentPrimary"]    = "#FF4D5E",
                ["AccentHover"]      = "#FF7684",
                ["AccentDim"]        = "#A02438",
                ["AccentSoft"]       = "#3A1A22",
                ["GateOpen"]         = "#46DDA0",
                ["GateClosed"]       = "#5A4650",
                ["Warn"]             = "#F0B040",
                ["Error"]            = "#FF4D5E",
            },
        },
        new ThemeDefinition
        {
            Name = "Aurora",
            Description = "Teal + cyan over deep navy.",
            AccentGradient = ("#22D3EE", "#3B82F6"),
            Colors = new()
            {
                ["SurfaceBase"]      = "#0A1322",
                ["SurfaceRaised"]    = "#101D33",
                ["SurfaceFloating"]  = "#152641",
                ["SurfaceHover"]     = "#1D3251",
                ["SurfacePanel"]     = "#0E1A2D",
                ["SurfaceElevated"]  = "#1B304F",
                ["Divider"]          = "#1E3050",
                ["DividerBright"]    = "#2C4470",
                ["RuleLine"]         = "#11203A",
                ["TextPrimary"]      = "#E8F4FF",
                ["TextSecondary"]    = "#9BBCD9",
                ["TextTertiary"]     = "#5F7E9A",
                ["TextMuted"]        = "#36506C",
                ["AccentPrimary"]    = "#22D3EE",
                ["AccentHover"]      = "#67E8F9",
                ["AccentDim"]        = "#0E7490",
                ["AccentSoft"]       = "#0F2C3C",
                ["GateOpen"]         = "#34D399",
                ["GateClosed"]       = "#3F567A",
                ["Warn"]             = "#FBBF24",
                ["Error"]            = "#F87171",
            },
        },
        new ThemeDefinition
        {
            Name = "Sunset",
            Description = "Warm amber/peach on bronze-shadow.",
            AccentGradient = ("#FB923C", "#F472B6"),
            Colors = new()
            {
                ["SurfaceBase"]      = "#130E0A",
                ["SurfaceRaised"]    = "#1E1812",
                ["SurfaceFloating"]  = "#28201A",
                ["SurfaceHover"]     = "#352B22",
                ["SurfacePanel"]     = "#1F1812",
                ["SurfaceElevated"]  = "#332720",
                ["Divider"]          = "#332720",
                ["DividerBright"]    = "#4A3826",
                ["RuleLine"]         = "#1F1812",
                ["TextPrimary"]      = "#FFF1E0",
                ["TextSecondary"]    = "#D4B79A",
                ["TextTertiary"]     = "#967462",
                ["TextMuted"]        = "#5C4537",
                ["AccentPrimary"]    = "#FB923C",
                ["AccentHover"]      = "#FDBA74",
                ["AccentDim"]        = "#9A5A1E",
                ["AccentSoft"]       = "#3E2A1A",
                ["GateOpen"]         = "#65D9B0",
                ["GateClosed"]       = "#6A5040",
                ["Warn"]             = "#FBBF24",
                ["Error"]            = "#F87171",
            },
        },
        new ThemeDefinition
        {
            Name = "Mono",
            Description = "Pure greyscale — for the brutalists.",
            AccentGradient = ("#F0F0F0", "#A0A0A0"),
            Colors = new()
            {
                ["SurfaceBase"]      = "#0A0A0A",
                ["SurfaceRaised"]    = "#141414",
                ["SurfaceFloating"]  = "#1C1C1C",
                ["SurfaceHover"]     = "#262626",
                ["SurfacePanel"]     = "#101010",
                ["SurfaceElevated"]  = "#222222",
                ["Divider"]          = "#262626",
                ["DividerBright"]    = "#3A3A3A",
                ["RuleLine"]         = "#161616",
                ["TextPrimary"]      = "#F5F5F5",
                ["TextSecondary"]    = "#9E9E9E",
                ["TextTertiary"]     = "#666666",
                ["TextMuted"]        = "#3A3A3A",
                ["AccentPrimary"]    = "#F0F0F0",
                ["AccentHover"]      = "#FFFFFF",
                ["AccentDim"]        = "#808080",
                ["AccentSoft"]       = "#2A2A2A",
                ["GateOpen"]         = "#E0E0E0",
                ["GateClosed"]       = "#4A4A4A",
                ["Warn"]             = "#E0E0E0",
                ["Error"]            = "#FF6060",
            },
        },
    };
}
