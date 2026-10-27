namespace QuickMove;

public static class MoveSummary
{
    public static string Format(int moved, int renamed, int skipped, int failed, string? firstFailure)
    {
        if (moved == 0 && failed == 0 && skipped > 0)
            return $"跳过 {skipped} 个（已在目标目录）";

        var parts = new List<string>();
        if (moved > 0)
        {
            var text = $"已移动 {moved} 个";
            if (renamed > 0)
                text += $"，其中 {renamed} 个因重名已改名";
            parts.Add(text);
        }

        if (skipped > 0)
            parts.Add($"跳过 {skipped} 个");
        if (failed > 0)
            parts.Add($"失败 {failed} 个：{firstFailure}");
        return string.Join("；", parts);
    }
}
