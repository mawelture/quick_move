namespace QuickMove;

public static class NameGenerator
{
    public const int MaxSuffix = 999;

    public static string? Choose(string originalName, bool isDirectory, IEnumerable<string> occupied)
    {
        if (!Contains(occupied, originalName))
            return originalName;

        for (var i = 1; i <= MaxSuffix; i++)
        {
            var candidate = isDirectory
                ? $"{originalName} ({i})"
                : $"{Path.GetFileNameWithoutExtension(originalName)} ({i}){Path.GetExtension(originalName)}";
            if (!Contains(occupied, candidate))
                return candidate;
        }

        return null;
    }

    static bool Contains(IEnumerable<string> occupied, string name)
    {
        foreach (var item in occupied)
        {
            if (string.Equals(item, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
