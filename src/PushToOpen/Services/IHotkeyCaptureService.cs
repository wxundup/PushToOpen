using PushToOpen.Models;

namespace PushToOpen.Services;

public interface IHotkeyCaptureService : IDisposable
{
    bool IsCapturing { get; }

    event EventHandler<KeyBindingInfo>? Captured;
    event EventHandler? Cancelled;

    void Start();
    void Cancel();
}
