using PushToOpen.Models;

namespace PushToOpen.Services;

public interface IGlobalHotkeyListener : IDisposable
{
    event EventHandler? Triggered;
    void SetBinding(KeyBindingInfo? key);
    void Start();
    void Stop();
}
