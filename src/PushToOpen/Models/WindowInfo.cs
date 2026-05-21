namespace PushToOpen.Models;

public sealed class WindowInfo
{
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public string? ExePath { get; init; }

    public string DisplayLabel => string.IsNullOrWhiteSpace(Title)
        ? ProcessName
        : $"{Title}  —  {ProcessName}";

    public override string ToString() => DisplayLabel;
}
