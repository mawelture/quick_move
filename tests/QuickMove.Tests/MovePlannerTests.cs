namespace QuickMove.Tests;

public class MovePlannerTests
{
    sealed class FakePaths : IPathInfo
    {
        public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Directories { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool Exists(string path) =>
            Files.Contains(PathNormalizer.Normalize(path)) || Directories.Contains(PathNormalizer.Normalize(path));
        public bool IsDirectory(string path) => Directories.Contains(PathNormalizer.Normalize(path));
    }

    static HashSet<string> Occupied(params string[] names) =>
        new(names, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Drive_root_keeps_its_separator()
    {
        Assert.Equal(@"C:\", PathNormalizer.Normalize(@"C:\"));
        Assert.Equal(@"C:\inbox", PathNormalizer.Normalize(@"C:\inbox\"));
    }

    [Fact]
    public void Duplicate_paths_are_planned_once()
    {
        var paths = new FakePaths();
        paths.Files.Add(@"C:\other\a.txt");
        var plan = MovePlanner.Plan(
            new[] { @"C:\other\a.txt", @"c:\other\a.txt" },
            @"C:\inbox",
            paths,
            Occupied());
        Assert.Single(plan);
        Assert.Equal(MoveAction.Move, plan[0].Action);
        Assert.Equal(@"C:\inbox\a.txt", plan[0].DestinationPath);
    }

    [Fact]
    public void Parent_folder_covers_selected_children()
    {
        var paths = new FakePaths();
        paths.Directories.Add(@"C:\data\folder");
        paths.Files.Add(@"C:\data\folder\child.txt");
        var plan = MovePlanner.Plan(
            new[] { @"C:\data\folder\child.txt", @"C:\data\folder" },
            @"C:\inbox",
            paths,
            Occupied());
        var item = Assert.Single(plan);
        Assert.Equal(@"C:\data\folder", item.SourcePath);
        Assert.Equal(@"C:\inbox\folder", item.DestinationPath);
    }

    [Fact]
    public void File_already_in_the_target_is_skipped()
    {
        var paths = new FakePaths();
        paths.Files.Add(@"C:\inbox\a.txt");
        var plan = MovePlanner.Plan(new[] { @"C:\inbox\a.txt" }, @"C:\inbox", paths, Occupied("a.txt"));
        Assert.Equal(MoveAction.Skip, Assert.Single(plan).Action);
    }

    [Fact]
    public void Nested_file_is_moved_to_the_target_root()
    {
        var paths = new FakePaths();
        paths.Files.Add(@"C:\inbox\sub\a.txt");
        paths.Directories.Add(@"C:\inbox");
        paths.Directories.Add(@"C:\inbox\sub");
        var plan = MovePlanner.Plan(new[] { @"C:\inbox\sub\a.txt" }, @"C:\inbox", paths, Occupied());
        var item = Assert.Single(plan);
        Assert.Equal(MoveAction.Move, item.Action);
        Assert.Equal(@"C:\inbox\a.txt", item.DestinationPath);
        Assert.False(item.Renamed);
    }

    [Fact]
    public void Target_inside_source_folder_fails()
    {
        var paths = new FakePaths();
        paths.Directories.Add(@"C:\data\folder");
        var plan = MovePlanner.Plan(new[] { @"C:\data\folder" }, @"C:\data\folder\archive", paths, Occupied());
        var item = Assert.Single(plan);
        Assert.Equal(MoveAction.Fail, item.Action);
        Assert.Equal("不能把文件夹移进它自己里面", item.Reason);
    }

    [Fact]
    public void Selecting_the_target_directory_fails()
    {
        var paths = new FakePaths();
        paths.Directories.Add(@"C:\data\inbox");
        var plan = MovePlanner.Plan(new[] { @"C:\data\inbox\" }, @"C:\data\inbox", paths, Occupied());
        var item = Assert.Single(plan);
        Assert.Equal(MoveAction.Fail, item.Action);
        Assert.Equal("不能移动目标目录本身", item.Reason);
    }

    [Fact]
    public void Missing_source_fails()
    {
        var plan = MovePlanner.Plan(new[] { @"C:\missing.txt" }, @"C:\inbox", new FakePaths(), Occupied());
        var item = Assert.Single(plan);
        Assert.Equal(MoveAction.Fail, item.Action);
        Assert.Equal("文件或文件夹不存在", item.Reason);
    }

    [Fact]
    public void Second_same_name_is_renamed_and_claimed()
    {
        var paths = new FakePaths();
        paths.Files.Add(@"C:\a\report.pdf");
        paths.Files.Add(@"C:\b\report.pdf");
        var occupied = Occupied();
        var plan = MovePlanner.Plan(
            new[] { @"C:\a\report.pdf", @"C:\b\report.pdf" },
            @"C:\inbox",
            paths,
            occupied);
        Assert.Equal(@"C:\inbox\report.pdf", plan[0].DestinationPath);
        Assert.False(plan[0].Renamed);
        Assert.Equal(@"C:\inbox\report (1).pdf", plan[1].DestinationPath);
        Assert.True(plan[1].Renamed);
        Assert.Contains("report (1).pdf", occupied);
    }

    [Fact]
    public void No_available_name_fails()
    {
        var paths = new FakePaths();
        paths.Files.Add(@"C:\other\a.txt");
        var occupied = Occupied("a.txt");
        for (var i = 1; i <= 999; i++)
            occupied.Add($"a ({i}).txt");
        var plan = MovePlanner.Plan(new[] { @"C:\other\a.txt" }, @"C:\inbox", paths, occupied);
        var item = Assert.Single(plan);
        Assert.Equal(MoveAction.Fail, item.Action);
        Assert.Equal("没有可用的文件名", item.Reason);
    }

    [Theory]
    [InlineData(3, 1, 0, 0, null, "已移动 3 个，其中 1 个因重名已改名")]
    [InlineData(3, 1, 1, 1, "文件被占用", "已移动 3 个，其中 1 个因重名已改名；跳过 1 个；失败 1 个：文件被占用")]
    [InlineData(0, 0, 2, 0, null, "跳过 2 个（已在目标目录）")]
    [InlineData(0, 0, 1, 1, "没有权限", "跳过 1 个；失败 1 个：没有权限")]
    [InlineData(2, 0, 0, 0, null, "已移动 2 个")]
    public void Summary_uses_the_spec_wording(int moved, int renamed, int skipped, int failed, string? reason, string expected)
    {
        Assert.Equal(expected, MoveSummary.Format(moved, renamed, skipped, failed, reason));
    }
}
