namespace QuickMove;

[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Control = 1,
    Alt = 2,
    Shift = 4,
    Win = 8
}

public sealed record Hotkey(HotkeyModifiers Modifiers, Keys Key);

public static class HotkeyText
{
    public static string Format(Hotkey hotkey)
    {
        var parts = new List<string>();
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Control))
            parts.Add("Ctrl");
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt))
            parts.Add("Alt");
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Shift))
            parts.Add("Shift");
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Win))
            parts.Add("Win");
        parts.Add(FormatKey(hotkey.Key));
        return string.Join("+", parts);
    }

    public static IReadOnlyList<string> ModifierNames(HotkeyModifiers modifiers)
    {
        var names = new List<string>();
        if (modifiers.HasFlag(HotkeyModifiers.Control))
            names.Add("Control");
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
            names.Add("Alt");
        if (modifiers.HasFlag(HotkeyModifiers.Shift))
            names.Add("Shift");
        if (modifiers.HasFlag(HotkeyModifiers.Win))
            names.Add("Win");
        return names;
    }

    public static bool TryCreate(IReadOnlyList<string> modifiers, string? key, out Hotkey? hotkey)
    {
        hotkey = null;
        if (string.IsNullOrWhiteSpace(key) || !Enum.TryParse<Keys>(key, ignoreCase: false, out var parsed))
            return false;
        if (parsed == Keys.None || IsModifierKey(parsed))
            return false;

        var flags = HotkeyModifiers.None;
        foreach (var name in modifiers)
        {
            switch (name)
            {
                case "Control":
                    flags |= HotkeyModifiers.Control;
                    break;
                case "Alt":
                    flags |= HotkeyModifiers.Alt;
                    break;
                case "Shift":
                    flags |= HotkeyModifiers.Shift;
                    break;
                case "Win":
                    flags |= HotkeyModifiers.Win;
                    break;
                default:
                    return false;
            }
        }

        if (flags == HotkeyModifiers.None)
            return false;

        hotkey = new Hotkey(flags, parsed);
        return true;
    }

    static bool IsModifierKey(Keys key) =>
        key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin;

    static string FormatKey(Keys key)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
            return ((int)(key - Keys.D0)).ToString();
        return key.ToString();
    }
}
