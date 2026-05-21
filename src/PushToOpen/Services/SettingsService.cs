using System.Text.Json;
using PushToOpen.Models;

namespace PushToOpen.Services;

public sealed class SettingsService : ISettingsService, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _path;
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private readonly object _mutateLock = new();
    private readonly System.Threading.Timer _debounceTimer;
    private AppSettings _current = new();
    private volatile bool _pendingSave;
    private bool _loaded;

    public SettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PushToOpen");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
        _debounceTimer = new System.Threading.Timer(OnDebounceTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public AppSettings Current => _current;

    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        await _ioLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (File.Exists(_path))
            {
                await using var stream = File.OpenRead(_path);
                var loaded = await JsonSerializer
                    .DeserializeAsync<AppSettings>(stream, JsonOpts, ct)
                    .ConfigureAwait(false);
                if (loaded is not null) _current = loaded;
            }
            _loaded = true;
        }
        catch
        {
            _current = new AppSettings();
            _loaded = true;
        }
        finally
        {
            _ioLock.Release();
        }

        SettingsChanged?.Invoke(this, _current);
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        await _ioLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var tmp = _path + ".tmp";
            await using (var stream = File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(stream, _current, JsonOpts, ct).ConfigureAwait(false);
            }
            File.Move(tmp, _path, overwrite: true);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public void Mutate(Action<AppSettings> mutate)
    {
        AppSettings snapshot;
        lock (_mutateLock)
        {
            mutate(_current);
            snapshot = _current;
        }
        SettingsChanged?.Invoke(this, snapshot);
        if (!_loaded) return;
        _pendingSave = true;
        _debounceTimer.Change(400, Timeout.Infinite);
    }

    private async void OnDebounceTick(object? _)
    {
        if (!_pendingSave) return;
        _pendingSave = false;
        try
        {
            await SaveAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[SettingsService] save failed: " + ex.Message);
            _pendingSave = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _debounceTimer.DisposeAsync().ConfigureAwait(false);
        if (_pendingSave) { try { await SaveAsync().ConfigureAwait(false); } catch { } }
        _ioLock.Dispose();
    }
}
