namespace QuickMove;

public static class FileMover
{
    public static MoveOutcome Move(string source, string destination) =>
        Move(source, destination, forceCopy: !SameVolume(source, destination));

    internal static MoveOutcome Move(string source, string destination, bool forceCopy)
    {
        try
        {
            if (!forceCopy)
            {
                if (Directory.Exists(source))
                    Directory.Move(source, destination);
                else
                    File.Move(source, destination);
                return MoveOutcome.Ok();
            }

            return CopyThenDelete(source, destination);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return MoveOutcome.Fail(ex.Message);
        }
    }

    static bool SameVolume(string source, string destination)
    {
        var sourceRoot = Path.GetPathRoot(Path.GetFullPath(source));
        var destinationRoot = Path.GetPathRoot(Path.GetFullPath(destination));
        return string.Equals(sourceRoot, destinationRoot, StringComparison.OrdinalIgnoreCase);
    }

    static MoveOutcome CopyThenDelete(string source, string destination)
    {
        var directory = Directory.Exists(source);
        if (directory)
            CopyDirectory(source, destination);
        else
            File.Copy(source, destination);

        try
        {
            if (directory)
                Directory.Delete(source, recursive: true);
            else
                File.Delete(source);
            return MoveOutcome.Ok();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return MoveOutcome.Fail("目标中已留下副本，源文件仍在");
        }
    }

    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }
}
