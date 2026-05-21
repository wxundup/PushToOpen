using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace PushToOpen.Controls;

public sealed class VuMeter : UserControl
{
    private readonly Grid _root;
    private readonly Rectangle _track;
    private readonly Rectangle _bar;
    private readonly Rectangle _threshold;
    private readonly Border _halo;

    private static readonly GradientBrush BarBrush = new LinearGradientBrush
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 0),
        GradientStops = new GradientStopCollection
        {
            new GradientStop { Color = Color.FromArgb(255, 92, 126, 230), Offset = 0 },
            new GradientStop { Color = Color.FromArgb(255, 124, 156, 255), Offset = 0.6 },
            new GradientStop { Color = Color.FromArgb(255, 61, 220, 151), Offset = 1 },
        }
    };

    private static readonly GradientBrush HaloBrush = new LinearGradientBrush
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 0),
        GradientStops = new GradientStopCollection
        {
            new GradientStop { Color = Color.FromArgb(0,   61, 220, 151), Offset = 0 },
            new GradientStop { Color = Color.FromArgb(85,  61, 220, 151), Offset = 1 },
        }
    };

    public VuMeter()
    {
        _track = new Rectangle
        {
            Fill = new SolidColorBrush(Color.FromArgb(255, 17, 21, 29)),
            RadiusX = 7,
            RadiusY = 7,
        };

        _halo = new Border
        {
            Background = HaloBrush,
            CornerRadius = new CornerRadius(7),
            Opacity = 0,
        };

        _bar = new Rectangle
        {
            Fill = BarBrush,
            RadiusX = 7,
            RadiusY = 7,
            Height = 12,
            Margin = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0,
        };

        _threshold = new Rectangle
        {
            Fill = new SolidColorBrush(Color.FromArgb(255, 255, 179, 71)),
            Width = 2,
            HorizontalAlignment = HorizontalAlignment.Left,
        };

        _root = new Grid
        {
            Height = 14,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Children = { _track, _halo, _bar, _threshold }
        };

        Content = _root;
        SizeChanged += (_, _) => UpdateLayout2();
    }

    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(
        nameof(Level), typeof(double), typeof(VuMeter), new PropertyMetadata(0.0, OnChanged));
    public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(
        nameof(Threshold), typeof(double), typeof(VuMeter), new PropertyMetadata(0.5, OnChanged));
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(VuMeter), new PropertyMetadata(false, OnActiveChanged));

    public double Level { get => (double)GetValue(LevelProperty); set => SetValue(LevelProperty, value); }
    public double Threshold { get => (double)GetValue(ThresholdProperty); set => SetValue(ThresholdProperty, value); }
    public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((VuMeter)d).UpdateLayout2();

    private static void OnActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((VuMeter)d).UpdateHalo();

    private void UpdateLayout2()
    {
        double w = _root.ActualWidth;
        if (w <= 0) return;
        _bar.Width = Math.Clamp(Level, 0, 1) * w;
        _threshold.Margin = new Thickness(Math.Clamp(Threshold, 0, 1) * w, 0, 0, 0);
    }

    private void UpdateHalo() => _halo.Opacity = IsActive ? 1 : 0;
}
