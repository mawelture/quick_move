namespace QuickMove.Tests;

public class FileMoverTests : IDisposable
{
    readonly string _root = Directory.CreateTempSubdirectory("quickmove-move").FullName;

    string Dir(string name)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void Same_volume_move_removes_the_source_file()
    {
        var sourceDir = Dir("source");
        var targetDir = Dir("target");
        var source = Path.Combine(sourceDir, "a.txt");
        File.WriteAllText(source, "hello");
        var destination = Path.Combine(targetDir, "a.txt");

        var outcome = FileMover.Move(source, destination);

        Assert.True(outcome.Succeeded);
        Assert.False(File.Exists(source));
        Assert.Equal("hello", File.ReadAllText(destination));
    }

    [Fact]
    public void Same_volume_move_moves_a_directory()
    {
        var source = Dir("folder");
        File.WriteAllText(Path.Combine(source, "inside.txt"), "x");
        var destination = Path.Combine(_root, "moved");

        var outcome = FileMover.Move(source, destination);

        Assert.True(outcome.Succeeded);
        Assert.False(Directory.Exists(source));
        Assert.Equal("x", File.ReadAllText(Path.Combine(destination, "inside.txt")));
    }

    [Fact]
    public void Forced_copy_of_a_readonly_file_leaves_both_copies()
    {
        var sourceDir = Dir("copy-source");
        var targetDir = Dir("copy-target");
        var source = Path.Combine(sourceDir, "locked.txt");
        File.WriteAllText(source, "data");
        File.SetAttributes(source, FileAttributes.ReadOnly);
        var destination = Path.Combine(targetDir, "locked.txt");

        var outcome = FileMover.Move(source, destination, forceCopy: true);

        Assert.False(outcome.Succeeded);
        Assert.Equal("目标中已留下副本，源文件仍在", outcome.Error);
        Assert.True(File.Exists(source));
        Assert.Equal("data", File.ReadAllText(destination));
    }

    public void Dispose()
    {
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_root, true);
    }
}
