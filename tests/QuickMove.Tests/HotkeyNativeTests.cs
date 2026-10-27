namespace QuickMove.Tests;

public class HotkeyNativeTests
{
    [Fact]
    public void Modifiers_include_norepeat()
    {
        var value = HotkeyNative.ToModifiers(HotkeyModifiers.Control | HotkeyModifiers.Alt);
        Assert.Equal(HotkeyNative.ModControl | HotkeyNative.ModAlt | HotkeyNative.ModNoRepeat, value);
        Assert.Equal((uint)Keys.D1, HotkeyNative.ToVirtualKey(Keys.D1));
    }
}
