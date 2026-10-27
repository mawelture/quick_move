namespace QuickMove;

public sealed record Coordination(string? Message, IReadOnlyList<PlannedItem> Plan);

public static class MoveCoordinator
{
    public static Coordination Prepare(
        Binding binding,
        IReadOnlyList<string> selectedPaths,
        IPathInfo pathInfo,
        ISet<string> occupiedNames)
    {
        if (!binding.Enabled || binding.Hotkey == null || string.IsNullOrWhiteSpace(binding.Directory))
            return new Coordination("这一行未启用", Array.Empty<PlannedItem>());

        if (!pathInfo.Exists(binding.Directory))
            return new Coordination("目标目录不存在", Array.Empty<PlannedItem>());
        if (!pathInfo.IsDirectory(binding.Directory))
            return new Coordination("目标不是文件夹", Array.Empty<PlannedItem>());

        var plan = MovePlanner.Plan(selectedPaths, binding.Directory, pathInfo, occupiedNames);
        if (plan.Count == 0)
            return new Coordination("没有选中的文件或文件夹", plan);
        return new Coordination(null, plan);
    }
}
