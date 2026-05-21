using System.Text.Json.Serialization;

namespace PushToOpen.Models;

public sealed class AppSettings
{
    public string? InputDeviceId { get; set; }

    public double ThresholdDb { get; set; } = -38.0;

    public double NoiseGateDb { get; set; } = -55.0;

    public double GainDb { get; set; } = 0.0;

    public int AttackMs { get; set; } = 25;

    public int ReleaseMs { get; set; } = 220;

    public int DebounceMs { get; set; } = 40;

    public int PollIntervalMs { get; set; } = 10;

    public bool Enabled { get; set; } = true;

    public bool Muted { get; set; }

    public bool StartMinimized { get; set; }

    public bool MinimizeToTray { get; set; } = true;

    public bool LaunchOnStartup { get; set; }

    public bool ShowOverlay { get; set; }

    public bool OverlayAlwaysOnTop { get; set; } = true;

    public bool OverlayLocked { get; set; }

    public double OverlayX { get; set; } = double.NaN;

    public double OverlayY { get; set; } = double.NaN;

    public KeyBindingInfo Hotkey { get; set; } = KeyBindingInfo.DefaultPushToTalk;

    [JsonIgnore]
    public double ThresholdLinear => DbToLinear(ThresholdDb);

    [JsonIgnore]
    public double NoiseGateLinear => DbToLinear(NoiseGateDb);

    [JsonIgnore]
    public double GainLinear => DbToLinear(GainDb);

    public static double DbToLinear(double db) => System.Math.Pow(10.0, db / 20.0);

    public static double LinearToDb(double linear) =>
        linear <= 1e-6 ? -120.0 : 20.0 * System.Math.Log10(linear);

    public AppSettings Clone() => (AppSettings)MemberwiseClone();
}
