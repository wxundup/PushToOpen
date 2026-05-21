using System.Runtime.InteropServices;
using PushToOpen.Models;

namespace PushToOpen.Utilities;

public static class KeyCodeMap
{
    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetKeyNameText(int lParam, System.Text.StringBuilder lpString, int nSize);

    private const uint MAPVK_VK_TO_VSC_EX = 0x04;

    public static KeyBindingInfo FromVirtualKey(ushort vk)
    {
        uint mapped = MapVirtualKey(vk, MAPVK_VK_TO_VSC_EX);
        ushort scan = (ushort)(mapped & 0xFF);
        bool extended = (mapped & 0xE000) != 0 || IsAlwaysExtended(vk);

        int lParam = (scan << 16) | (extended ? 1 << 24 : 0);
        var sb = new System.Text.StringBuilder(64);
        GetKeyNameText(lParam, sb, sb.Capacity);
        string name = sb.Length > 0 ? sb.ToString() : FallbackName(vk);

        return new KeyBindingInfo
        {
            VirtualKey = vk,
            ScanCode = scan,
            IsExtended = extended,
            DisplayName = name,
        };
    }

    private static bool IsAlwaysExtended(ushort vk) => vk switch
    {
        0x21 or 0x22 or 0x23 or 0x24 or 0x25 or 0x26 or 0x27 or 0x28 or 0x2D or 0x2E or 0x6F or 0x90 or 0xA3 or 0xA5 => true,
        _ => false,
    };

    private static string FallbackName(ushort vk) => vk switch
    {
        0x01 => "Mouse L", 0x02 => "Mouse R", 0x04 => "Mouse M",
        0x05 => "Mouse X1", 0x06 => "Mouse X2",
        _ => $"VK {vk:X2}",
    };
}
