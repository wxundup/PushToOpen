namespace PushToOpen.Services;

public interface IForegroundWatcher : IDisposable
{
    /// <summary>Lowercase exe name of the current foreground process, e.g. "discord.exe". May be null.</summary>
    string? ForegroundProcessName { get; }

    event EventHandler<string?>? ForegroundChanged;

    void Start();
    void Stop();
}
