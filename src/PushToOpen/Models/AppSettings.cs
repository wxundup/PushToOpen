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

    public KeyBindingInfo? MuteToggleHotkey { get; set; }

    public bool MonitorEnabled { get; set; }

    public double MonitorGainDb { get; set; } = -6.0;

    public bool NoiseSuppressionEnabled { get; set; }

    public double NoiseSuppressionStrength { get; set; } = 0.7;

    /// <summary>
    /// When non-null, PushToOpen only fires when this process owns the foreground window.
    /// Match is case-insensitive on executable name (e.g. "discord.exe"). Null = always on.
    /// </summary>
    public string? RestrictToProcessName { get; set; }

    public string? RestrictToProcessDisplayName { get; set; }

    // Theme system
    public string ThemeName { get; set; } = "Indigo Night";

    public string? BackgroundImagePath { get; set; }

    public double BackgroundImageOpacity { get; set; } = 0.18;

    /// <summary>"sidebar" (default) or "compact" (top tabs, no left rail).</summary>
    public string LayoutMode { get; set; } = "sidebar";

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
