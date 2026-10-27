namespace QuickMove.Tests;

public class ConfigStoreTests : IDisposable
{
    readonly string _dir = Directory.CreateTempSubdirectory("quickmove-config").FullName;

    string PathOf(string name) => Path.Combine(_dir, name);

    [Fact]
    public void Default_path_is_under_appdata()
    {
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickMove",
            "config.json");
        Assert.Equal(expected, ConfigStore.DefaultPath);
    }

    [Fact]
    public void Missing_file_is_five_blank_rows()
    {
        var loaded = new ConfigStore().Load(PathOf("missing.json"));
        Assert.False(loaded.RecoveredFromCorruptFile);
        Assert.Equal(5, loaded.Config.Bindings.Count);
        Assert.All(loaded.Config.Bindings, row => Assert.Null(row.Hotkey));
    }

    [Fact]
    public void Round_trip_keeps_a_binding_and_pads_to_five()
    {
        var dir = Directory.CreateTempSubdirectory("quickmove-dest").FullName;
        var rows = AppConfig.Blank().Bindings.ToArray();
        rows[0] = new Binding(true, new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.D1), dir);
        var path = PathOf("config.json");
        var store = new ConfigStore();
        store.Save(path, new AppConfig { Bindings = rows });
        var loaded = store.Load(path);
        Assert.False(loaded.RecoveredFromCorruptFile);
        Assert.Equal(rows[0], loaded.Config.Bindings[0]);
        Assert.Equal(5, loaded.Config.Bindings.Count);
        Assert.Null(loaded.Config.Bindings[4].Hotkey);
    }

    [Fact]
    public void More_than_five_rows_keeps_the_first_five()
    {
        var path = PathOf("config.json");
        var bindings = string.Join(",", Enumerable.Range(1, 6).Select(i =>
            "{\"enabled\":false,\"modifiers\":[\"Control\"],\"key\":\"D" + i + "\",\"directory\":null}"));
        File.WriteAllText(path, "{\"bindings\":[" + bindings + "]}");
        var loaded = new ConfigStore().Load(path);
        Assert.Equal(Keys.D5, loaded.Config.Bindings[4].Hotkey!.Key);
        Assert.Equal(5, loaded.Config.Bindings.Count);
    }

    [Fact]
    public void Bad_hotkey_row_becomes_blank_and_the_next_row_remains()
    {
        var path = PathOf("config.json");
        File.WriteAllText(path, """
            {
              "bindings": [
                {"enabled": true, "modifiers": ["Control"], "key": "NotAKey", "directory": "D:\\A"},
                {"enabled": true, "modifiers": [], "key": "A", "directory": "D:\\B"},
                {"enabled": true, "modifiers": ["Alt"], "key": "F2", "directory": "D:\\C"}
              ]
            }
            """);
        var loaded = new ConfigStore().Load(path);
        Assert.Null(loaded.Config.Bindings[0].Hotkey);
        Assert.Null(loaded.Config.Bindings[0].Directory);
        Assert.False(loaded.Config.Bindings[0].Enabled);
        Assert.Null(loaded.Config.Bindings[1].Hotkey);
        Assert.Equal(new Hotkey(HotkeyModifiers.Alt, Keys.F2), loaded.Config.Bindings[2].Hotkey);
        Assert.Equal(@"D:\C", loaded.Config.Bindings[2].Directory);
    }

    [Fact]
    public void Corrupt_file_is_renamed_and_replaced_with_blank_config()
    {
        var path = PathOf("config.json");
        File.WriteAllText(path, "{");
        var loaded = new ConfigStore().Load(path);
        Assert.True(loaded.RecoveredFromCorruptFile);
        Assert.Equal(5, loaded.Config.Bindings.Count);
        Assert.False(File.Exists(path));
        Assert.NotEmpty(Directory.GetFiles(_dir, "config.json.*.bak"));
    }

    public void Dispose() => Directory.Delete(_dir, true);
}
