namespace QuickMove;

public sealed record ValidationFailure(int RowIndex, string Message);

public static class BindingValidator
{
    public static ValidationFailure? Validate(IReadOnlyList<Binding> bindings)
    {
        var seen = new Dictionary<Hotkey, int>();
        for (var i = 0; i < bindings.Count; i++)
        {
            var row = bindings[i];
            if (row.Hotkey != null && row.Hotkey.Modifiers == HotkeyModifiers.None)
                return new ValidationFailure(i, "快捷键必须包含 Ctrl、Alt、Shift 或 Win");

            if (row.Hotkey != null)
            {
                if (seen.TryGetValue(row.Hotkey, out var first))
                    return new ValidationFailure(i, $"快捷键与第 {first + 1} 行重复");
                seen.Add(row.Hotkey, i);
            }

            if (!row.Enabled)
                continue;
            if (row.Hotkey == null)
                return new ValidationFailure(i, "请填写快捷键");
            if (string.IsNullOrWhiteSpace(row.Directory) || !Directory.Exists(row.Directory))
                return new ValidationFailure(i, "请选择已存在的目录");
        }

        return null;
    }
}
