using PushToOpen.Models;

namespace PushToOpen.Services;

public interface ISettingsService
{
    AppSettings Current { get; }

    event EventHandler<AppSettings>? SettingsChanged;

    Task LoadAsync(CancellationToken ct = default);

    Task SaveAsync(CancellationToken ct = default);

    void Mutate(Action<AppSettings> mutate);
}
