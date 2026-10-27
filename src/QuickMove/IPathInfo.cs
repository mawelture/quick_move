namespace QuickMove;

public interface IPathInfo
{
    bool Exists(string path);
    bool IsDirectory(string path);
}

public sealed class DiskPathInfo : IPathInfo
{
    public bool Exists(string path) => File.Exists(path) || Directory.Exists(path);
    public bool IsDirectory(string path) => Directory.Exists(path);
}
