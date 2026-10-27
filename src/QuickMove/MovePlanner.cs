namespace QuickMove;

public static class MovePlanner
{
    public static IReadOnlyList<PlannedItem> Plan(
        IReadOnlyList<string> sourcePaths,
        string targetDirectory,
        IPathInfo pathInfo,
        ISet<string> occupiedNames)
    {
        var target = PathNormalizer.Normalize(targetDirectory);
        var unique = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in sourcePaths)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            string normalized;
            try
            {
                normalized = PathNormalizer.Normalize(raw);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (seen.Add(normalized))
                unique.Add(normalized);
        }

        var collapsed = new List<string>();
        foreach (var path in unique)
        {
            var covered = false;
            foreach (var other in unique)
            {
                if (path.Equals(other, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (PathNormalizer.IsInside(path, other))
                {
                    covered = true;
                    break;
                }
            }

            if (!covered)
                collapsed.Add(path);
        }

        var results = new List<PlannedItem>();
        foreach (var source in collapsed)
        {
            if (!pathInfo.Exists(source))
            {
                results.Add(Fail(source, "文件或文件夹不存在"));
                continue;
            }

            if (source.Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(Fail(source, "不能移动目标目录本身"));
                continue;
            }

            if (pathInfo.IsDirectory(source) && PathNormalizer.IsInside(target, source))
            {
                results.Add(Fail(source, "不能把文件夹移进它自己里面"));
                continue;
            }

            var parent = PathNormalizer.Parent(source);
            if (parent != null && parent.Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new PlannedItem(source, null, MoveAction.Skip, null, false));
                continue;
            }

            var name = Path.GetFileName(source);
            var chosen = NameGenerator.Choose(name, pathInfo.IsDirectory(source), occupiedNames);
            if (chosen == null)
            {
                results.Add(Fail(source, "没有可用的文件名"));
                continue;
            }

            occupiedNames.Add(chosen);
            var renamed = !string.Equals(chosen, name, StringComparison.OrdinalIgnoreCase);
            results.Add(new PlannedItem(source, Path.Combine(target, chosen), MoveAction.Move, null, renamed));
        }

        return results;
    }

    static PlannedItem Fail(string source, string reason) =>
        new(source, null, MoveAction.Fail, reason, false);
}
