namespace QuickMove.Tests;

public class HotkeyTextTests
{
    [Fact]
    public void Format_uses_ctrl_alt_shift_win_order()
    {
        var hotkey = new Hotkey(
            HotkeyModifiers.Win | HotkeyModifiers.Shift | HotkeyModifiers.Alt | HotkeyModifiers.Control,
            Keys.A);
        Assert.Equal("Ctrl+Alt+Shift+Win+A", HotkeyText.Format(hotkey));
    }

    [Fact]
    public void Digit_key_displays_as_a_number()
    {
        var hotkey = new Hotkey(HotkeyModifiers.Control, Keys.D1);
        Assert.Equal("Ctrl+1", HotkeyText.Format(hotkey));
    }

    [Fact]
    public void TryCreate_reads_config_parts()
    {
        var ok = HotkeyText.TryCreate(new[] { "Control", "Alt" }, "D1", out var hotkey);
        Assert.True(ok);
        Assert.Equal(new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.D1), hotkey);
    }

    [Fact]
    public void TryCreate_rejects_a_hotkey_without_a_modifier()
    {
        Assert.False(HotkeyText.TryCreate(Array.Empty<string>(), "A", out _));
    }

    [Fact]
    public void TryCreate_rejects_unknown_parts()
    {
        Assert.False(HotkeyText.TryCreate(new[] { "Control", "Super" }, "A", out _));
        Assert.False(HotkeyText.TryCreate(new[] { "Control" }, "NotAKey", out _));
        Assert.False(HotkeyText.TryCreate(new[] { "Control" }, "ControlKey", out _));
    }
}
