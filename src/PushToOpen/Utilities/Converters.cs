using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace PushToOpen.Utilities;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool b = value is bool v && v;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

public sealed class GateBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool open = value is bool b && b;
        var key = open ? "GateOpenBrush" : "GateClosedBrush";
        return Application.Current.Resources[key];
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

public sealed class GateLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? "OPEN" : "CLOSED";
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

public sealed class FormatDbConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double d)
        {
            if (double.IsNegativeInfinity(d) || d < -90) return "−∞ dB";
            return $"{d:0.0} dB";
        }
        return "—";
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

public sealed class SectionEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool match = string.Equals(value as string, parameter as string, StringComparison.OrdinalIgnoreCase);
        if (targetType == typeof(Visibility))
            return match ? Visibility.Visible : Visibility.Collapsed;
        return match;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? (parameter as string ?? string.Empty) : DependencyProperty.UnsetValue;
}

public sealed class LockGlyphConverter : IValueConverter
{
    // Segoe Fluent Icons: E72E = Lock, E785 = Unlock
    private const string LockGlyph = "";
    private const string UnlockGlyph = "";
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? LockGlyph : UnlockGlyph;
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

public sealed class LockBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool locked = value is bool b && b;
        var key = locked ? "AccentPrimaryBrush" : "TextTertiaryBrush";
        return Application.Current.Resources[key];
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
