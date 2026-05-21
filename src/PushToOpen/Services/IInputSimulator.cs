using PushToOpen.Models;

namespace PushToOpen.Services;

public interface IInputSimulator : IDisposable
{
    bool IsDown { get; }

    void Bind(KeyBindingInfo key);

    void Press();

    void Release();
}
