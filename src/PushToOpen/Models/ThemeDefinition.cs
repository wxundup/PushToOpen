namespace PushToOpen.Models;

public sealed class ThemeDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required Dictionary<string, string> Colors { get; init; }
    /// <summary>Two hex colors for the accent gradient (start → end).</summary>
    public required (string Start, string End) AccentGradient { get; init; }
}
