namespace QuickMove.Tests;

public class BindingValidatorTests
{
    static Binding Off() => new(false, null, null);

    [Fact]
    public void Blank_config_has_five_rows()
    {
        Assert.Equal(5, AppConfig.Blank().Bindings.Count);
        Assert.All(AppConfig.Blank().Bindings, row => Assert.False(row.Enabled));
    }

    [Fact]
    public void Empty_disabled_rows_are_valid()
    {
        Assert.Null(BindingValidator.Validate(AppConfig.Blank().Bindings));
    }

    [Fact]
    public void Enabled_row_without_a_hotkey_fails()
    {
        var rows = AppConfig.Blank().Bindings.ToArray();
        rows[0] = new Binding(true, null, null);
        var missingKey = BindingValidator.Validate(rows);
        Assert.NotNull(missingKey);
        Assert.Equal(0, missingKey.RowIndex);
        Assert.Equal("请填写快捷键", missingKey.Message);
    }

    [Fact]
    public void Enabled_row_without_an_existing_directory_fails()
    {
        var rows = AppConfig.Blank().Bindings.ToArray();
        rows[1] = new Binding(true, new Hotkey(HotkeyModifiers.Control, Keys.D1), @"C:\this-directory-does-not-exist-quickmove");
        var missingDir = BindingValidator.Validate(rows);
        Assert.NotNull(missingDir);
        Assert.Equal(1, missingDir.RowIndex);
        Assert.Equal("请选择已存在的目录", missingDir.Message);
    }

    [Fact]
    public void Enabled_row_with_a_real_directory_is_valid()
    {
        var dir = Directory.CreateTempSubdirectory("quickmove-valid");
        try
        {
            var rows = AppConfig.Blank().Bindings.ToArray();
            rows[2] = new Binding(true, new Hotkey(HotkeyModifiers.Control, Keys.D2), dir.FullName);
            Assert.Null(BindingValidator.Validate(rows));
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Fact]
    public void Duplicate_filled_hotkeys_fail_even_when_disabled()
    {
        var hotkey = new Hotkey(HotkeyModifiers.Control, Keys.D1);
        var rows = AppConfig.Blank().Bindings.ToArray();
        rows[0] = new Binding(false, hotkey, null);
        rows[3] = new Binding(false, hotkey, null);
        var failure = BindingValidator.Validate(rows);
        Assert.NotNull(failure);
        Assert.Equal(3, failure.RowIndex);
        Assert.Equal("快捷键与第 1 行重复", failure.Message);
    }

    [Fact]
    public void Hotkey_without_a_modifier_fails()
    {
        var rows = AppConfig.Blank().Bindings.ToArray();
        rows[4] = new Binding(false, new Hotkey(HotkeyModifiers.None, Keys.A), null);
        var failure = BindingValidator.Validate(rows);
        Assert.NotNull(failure);
        Assert.Equal(4, failure.RowIndex);
        Assert.Equal("快捷键必须包含 Ctrl、Alt、Shift 或 Win", failure.Message);
    }
}
