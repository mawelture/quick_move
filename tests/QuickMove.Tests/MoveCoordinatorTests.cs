namespace QuickMove.Tests;

public class MoveCoordinatorTests
{
    sealed class FakePaths : IPathInfo
    {
        public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Directories { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool Exists(string path) =>
            Files.Contains(PathNormalizer.Normalize(path)) || Directories.Contains(PathNormalizer.Normalize(path));
        public bool IsDirectory(string path) => Directories.Contains(PathNormalizer.Normalize(path));
    }

    static Binding Enabled(string? directory) =>
        new(true, new Hotkey(HotkeyModifiers.Control, Keys.D1), directory);

    [Fact]
    public void Disabled_or_incomplete_row_does_not_move()
    {
        var result = MoveCoordinator.Prepare(new Binding(false, null, null), Array.Empty<string>(), new FakePaths(), new HashSet<string>());
        Assert.Equal("这一行未启用", result.Message);
    }

    [Fact]
    public void Missing_target_is_reported_and_nothing_is_planned()
    {
        var paths = new FakePaths();
        paths.Files.Add(@"C:\other\a.txt");
        var result = MoveCoordinator.Prepare(Enabled(@"C:\missing-inbox"), new[] { @"C:\other\a.txt" }, paths, new HashSet<string>());
        Assert.Equal("目标目录不存在", result.Message);
        Assert.Empty(result.Plan);
    }

    [Fact]
    public void File_target_is_rejected()
    {
        var paths = new FakePaths();
        paths.Files.Add(@"C:\inbox.txt");
        var result = MoveCoordinator.Prepare(Enabled(@"C:\inbox.txt"), new[] { @"C:\other\a.txt" }, paths, new HashSet<string>());
        Assert.Equal("目标不是文件夹", result.Message);
    }

    [Fact]
    public void No_selection_has_its_own_message()
    {
        var paths = new FakePaths();
        paths.Directories.Add(@"C:\inbox");
        var result = MoveCoordinator.Prepare(Enabled(@"C:\inbox"), Array.Empty<string>(), paths, new HashSet<string>());
        Assert.Equal("没有选中的文件或文件夹", result.Message);
    }

    [Fact]
    public void Report_counts_planned_failures_before_move_failures()
    {
        var plan = new PlannedItem[]
        {
            new(@"C:\gone", null, MoveAction.Fail, "文件或文件夹不存在", false),
            new(@"C:\a\a.txt", @"C:\inbox\a.txt", MoveAction.Move, null, false),
            new(@"C:\inbox\b.txt", null, MoveAction.Skip, null, false)
        };
        var outcomes = new[] { MoveOutcome.Fail("文件被占用") };
        Assert.Equal("跳过 1 个；失败 2 个：文件或文件夹不存在", MoveReport.Build(plan, outcomes));
    }

    [Fact]
    public void Report_counts_a_renamed_success()
    {
        var plan = new[]
        {
            new PlannedItem(@"C:\a\a.txt", @"C:\inbox\a (1).txt", MoveAction.Move, null, true)
        };
        Assert.Equal("已移动 1 个，其中 1 个因重名已改名", MoveReport.Build(plan, new[] { MoveOutcome.Ok() }));
    }
}
