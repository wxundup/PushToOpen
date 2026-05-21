using PushToOpen.Models;

namespace PushToOpen.Services;

public interface IThemeService
{
    IReadOnlyList<ThemeDefinition> Themes { get; }
    ThemeDefinition Current { get; }
    event EventHandler<ThemeDefinition>? ThemeChanged;
    void Apply(string name);
    ThemeDefinition? Find(string name);
}
