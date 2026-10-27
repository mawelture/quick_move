namespace QuickMove;

public static class HotkeyNative
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;
    public const uint ModNoRepeat = 0x4000;

    public static uint ToModifiers(HotkeyModifiers modifiers)
    {
        uint value = ModNoRepeat;
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
            value |= ModAlt;
        if (modifiers.HasFlag(HotkeyModifiers.Control))
            value |= ModControl;
        if (modifiers.HasFlag(HotkeyModifiers.Shift))
            value |= ModShift;
        if (modifiers.HasFlag(HotkeyModifiers.Win))
            value |= ModWin;
        return value;
    }

    public static uint ToVirtualKey(Keys key) => (uint)key;
}
