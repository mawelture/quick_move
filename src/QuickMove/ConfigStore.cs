using System.Text.Json;

namespace QuickMove;

public sealed record LoadResult(AppConfig Config, bool RecoveredFromCorruptFile);

public sealed class ConfigStore
{
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QuickMove",
        "config.json");

    public LoadResult Load(string path)
    {
        if (!File.Exists(path))
            return new LoadResult(AppConfig.Blank(), false);

        JsonDocument document;
        try
        {
            using var stream = File.OpenRead(path);
            document = JsonDocument.Parse(stream);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return Recover(path);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("bindings", out var bindings) ||
                bindings.ValueKind != JsonValueKind.Array)
                return Recover(path);

            var rows = new List<Binding>();
            foreach (var element in bindings.EnumerateArray())
            {
                if (rows.Count == AppConfig.RowCount)
                    break;
                rows.Add(ParseRow(element));
            }

            while (rows.Count < AppConfig.RowCount)
                rows.Add(new Binding(false, null, null));

            return new LoadResult(new AppConfig { Bindings = rows }, false);
        }
    }

    public void Save(string path, AppConfig config)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);

        var rows = new List<object?>();
        for (var i = 0; i < AppConfig.RowCount; i++)
        {
            var row = i < config.Bindings.Count ? config.Bindings[i] : new Binding(false, null, null);
            rows.Add(new
            {
                enabled = row.Enabled,
                modifiers = row.Hotkey == null
                    ? Array.Empty<string>()
                    : HotkeyText.ModifierNames(row.Hotkey.Modifiers).ToArray(),
                key = row.Hotkey?.Key.ToString(),
                directory = row.Directory
            });
        }

        var json = JsonSerializer.Serialize(new { bindings = rows }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    static Binding ParseRow(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return new Binding(false, null, null);

        var modifiers = new List<string>();
        if (element.TryGetProperty("modifiers", out var modifierElement) && modifierElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var name in modifierElement.EnumerateArray())
            {
                if (name.ValueKind == JsonValueKind.String && name.GetString() is { } text)
                    modifiers.Add(text);
            }
        }

        string? key = null;
        if (element.TryGetProperty("key", out var keyElement) && keyElement.ValueKind == JsonValueKind.String)
            key = keyElement.GetString();

        if (!HotkeyText.TryCreate(modifiers, key, out var hotkey))
            return new Binding(false, null, null);

        var enabled = element.TryGetProperty("enabled", out var enabledElement) &&
            enabledElement.ValueKind == JsonValueKind.True;
        string? directory = null;
        if (element.TryGetProperty("directory", out var directoryElement) && directoryElement.ValueKind == JsonValueKind.String)
        {
            directory = directoryElement.GetString();
            if (string.IsNullOrWhiteSpace(directory))
                directory = null;
        }

        return new Binding(enabled, hotkey, directory);
    }

    static LoadResult Recover(string path)
    {
        var parent = Path.GetDirectoryName(path) ?? ".";
        var backup = Path.Combine(parent, $"config.json.{DateTime.Now:yyyyMMdd-HHmmss}.bak");
        if (File.Exists(backup))
            backup = Path.Combine(parent, $"config.json.{DateTime.Now:yyyyMMdd-HHmmss-fff}.bak");
        File.Move(path, backup);
        return new LoadResult(AppConfig.Blank(), true);
    }
}
