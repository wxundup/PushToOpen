using PushToOpen.Models;

namespace PushToOpen.Services;

public interface IWindowEnumerator
{
    IReadOnlyList<WindowInfo> EnumerateTopLevelWindows();
}
