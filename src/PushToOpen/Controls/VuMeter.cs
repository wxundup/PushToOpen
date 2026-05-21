using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace PushToOpen.Controls;

public sealed class VuMeter : UserControl
{
    private const double BarHeight = 10;
    private const double TrackHeight = 10;

    private readonly Grid _root;
    private readonly Rectangle _track;
    private readonly Border _bar;
    private readonly Border _glow;
    private readonly Rectangle _thresholdMarker;
    private readonly Storyboard _glowStory;

    private static readonly Color GlowColor    = Color.FromArgb(160, 123, 97, 255);
    private static readonly Color GlowMidColor = Color.FromArgb(120, 90,  143, 255);

    private static LinearGradientBrush BuildBarBrush() => new()
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 0),
        GradientStops =
        {
            new GradientStop { Color = Color.FromArgb(255,  61, 220, 151), Offset = 0.00 },
            new GradientStop { Color = Color.FromArgb(255,  61, 220, 151), Offset = 0.55 },
            new GradientStop { Color = Color.FromArgb(255, 245, 200,  66), Offset = 0.80 },
            new GradientStop { Color = Color.FromArgb(255, 255,  92, 122), Offset = 1.00 },
        }
    };

    private static LinearGradientBrush BuildGlowBrush() => new()
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 0),
        GradientStops =
        {
            new GradientStop { Color = GlowColor,    Offset = 0 },
            new GradientStop { Color = GlowMidColor, Offset = 1 },
        }
    };

    public VuMeter()
    {
        _track = new Rectangle
        {
            Height = TrackHeight,
            RadiusX = TrackHeight / 2,
            RadiusY = TrackHeight / 2,
            Fill = new SolidColorBrush(Color.FromArgb(255, 31, 35, 48)),
        };

        _glow = new Border
        {
            Height = BarHeight + 6,
            CornerRadius = new CornerRadius((BarHeight + 6) / 2),
            Background = BuildGlowBrush(),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0,
            Margin = new Thickness(-3, -3, 0, 0),
            Opacity = 0,
        };

        _bar = new Border
        {
            Height = BarHeight,
            CornerRadius = new CornerRadius(BarHeight / 2),
            Background = BuildBarBrush(),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0,
        };

        _thresholdMarker = new Rectangle
        {
            Width = 2,
            Height = BarHeight + 8,
            RadiusX = 1,
            RadiusY = 1,
            Fill = new SolidColorBrush(Color.FromArgb(255, 245, 200, 66)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };

        _root = new Grid
        {
            Height = BarHeight + 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _root.Children.Add(_track);
        _root.Children.Add(_glow);
        _root.Children.Add(_bar);
        _root.Children.Add(_thresholdMarker);

        var glowAnim = new DoubleAnimation
        {
            From = 0.55,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(900)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        Storyboard.SetTarget(glowAnim, _glow);
        Storyboard.SetTargetProperty(glowAnim, "Opacity");
        _glowStory = new Storyboard();
        _glowStory.Children.Add(glowAnim);

        Content = _root;
        SizeChanged += (_, _) => UpdateLayoutAll();
    }

    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(
        nameof(Level), typeof(double), typeof(VuMeter),
        new PropertyMetadata(0.0, (d, _) => ((VuMeter)d).UpdateLayoutAll()));

    public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(
        nameof(Threshold), typeof(double), typeof(VuMeter),
        new PropertyMetadata(0.5, (d, _) => ((VuMeter)d).UpdateLayoutAll()));

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(VuMeter),
        new PropertyMetadata(false, (d, e) => ((VuMeter)d).OnActiveChanged((bool)e.NewValue)));

    public double Level     { get => (double)GetValue(LevelProperty);    set => SetValue(LevelProperty, value); }
    public double Threshold { get => (double)GetValue(ThresholdProperty); set => SetValue(ThresholdProperty, value); }
    public bool IsActive    { get => (bool)GetValue(IsActiveProperty);   set => SetValue(IsActiveProperty, value); }

    private void UpdateLayoutAll()
    {
        double w = _root.ActualWidth;
        if (w <= 0) return;
        double level = Math.Clamp(Level, 0, 1);
        double barWidth = level * w;
        _bar.Width = barWidth;
        _glow.Width = barWidth + 6;
        _thresholdMarker.Margin = new Thickness(Math.Clamp(Threshold, 0, 1) * w - 1, 0, 0, 0);
    }

    private void OnActiveChanged(bool active)
    {
        if (active)
        {
            _glow.Opacity = 0.7;
            _glowStory.Begin();
        }
        else
        {
            _glowStory.Stop();
            _glow.Opacity = 0;
        }
    }
}
