namespace QuickMove;

public sealed record Binding(bool Enabled, Hotkey? Hotkey, string? Directory);

public sealed class AppConfig
{
    public const int RowCount = 5;

    public required IReadOnlyList<Binding> Bindings { get; init; }

    public static AppConfig Blank() => new()
    {
        Bindings = Enumerable.Range(0, RowCount).Select(_ => new Binding(false, null, null)).ToArray()
    };
}
