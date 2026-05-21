using Microsoft.UI.Dispatching;

namespace PushToOpen.Utilities;

public static class DispatcherHelper
{
    private static DispatcherQueue? _queue;

    public static void Initialize(DispatcherQueue queue) => _queue = queue;

    public static void Post(Action action)
    {
        var q = _queue;
        if (q is null) { action(); return; }
        if (q.HasThreadAccess) action();
        else q.TryEnqueue(() => action());
    }
}
