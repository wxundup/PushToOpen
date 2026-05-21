namespace PushToOpen.Models;

public sealed class KeyBindingInfo
{
    public ushort VirtualKey { get; set; }

    public ushort ScanCode { get; set; }

    public bool IsExtended { get; set; }

    public string DisplayName { get; set; } = "V";

    public static KeyBindingInfo DefaultPushToTalk => new()
    {
        VirtualKey = 0x56,
        ScanCode = 0x2F,
        IsExtended = false,
        DisplayName = "V"
    };

    public KeyBindingInfo Clone() => (KeyBindingInfo)MemberwiseClone();
}
