using System.Reflection;
using System.Runtime.InteropServices;

namespace QuickMove;

public static class ExplorerSelection
{
    static readonly Guid ShellWindowsClsid = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");

    public static IReadOnlyList<string> ReadSelectedPaths()
    {
        var paths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var type = Type.GetTypeFromCLSID(ShellWindowsClsid, throwOnError: true)!;
        var windows = Activator.CreateInstance(type)!;
        try
        {
            dynamic shellWindows = windows;
            int count = shellWindows.Count;
            for (var i = 0; i < count; i++)
            {
                dynamic window = shellWindows.Item(i);
                try
                {
                    ReadExplorerWindow(window, paths, seen);
                }
                finally
                {
                    Release(window);
                }
            }

            ReadDesktop(windows, paths, seen);
        }
        finally
        {
            Release(windows);
        }

        return paths;
    }

    static void ReadExplorerWindow(dynamic window, List<string> paths, HashSet<string> seen)
    {
        string? fullName;
        try
        {
            fullName = (string)window.FullName;
        }
        catch (COMException)
        {
            return;
        }

        if (fullName == null || !fullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
            return;
        ReadDocument(window, paths, seen);
    }

    static void ReadDesktop(object windows, List<string> paths, HashSet<string> seen)
    {
        object loc = 0;
        object locRoot = 0;
        object hwnd = 0;
        var args = new[] { loc, locRoot, (object)8, hwnd, 1 };
        var modifiers = new[] { new ParameterModifier(5) };
        modifiers[0][0] = true;
        modifiers[0][1] = true;
        modifiers[0][3] = true;
        object? dispatch;
        try
        {
            dispatch = windows.GetType().InvokeMember(
                "FindWindowSW",
                BindingFlags.InvokeMethod,
                null,
                windows,
                args,
                modifiers,
                null,
                null);
        }
        catch (Exception ex) when (ex is COMException or MissingMethodException or TargetInvocationException)
        {
            return;
        }

        if (dispatch == null)
            return;
        try
        {
            ReadDocument((dynamic)dispatch, paths, seen);
        }
        finally
        {
            Release(dispatch);
        }
    }

    static void ReadDocument(dynamic window, List<string> paths, HashSet<string> seen)
    {
        try
        {
            dynamic selected = window.Document.SelectedItems();
            int count = selected.Count;
            for (var i = 0; i < count; i++)
            {
                dynamic item = selected.Item(i);
                try
                {
                    string? path = (string)item.Path;
                    if (!string.IsNullOrWhiteSpace(path) && seen.Add(path))
                        paths.Add(path);
                }
                finally
                {
                    Release(item);
                }
            }

            Release(selected);
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
        }
    }

    static void Release(object? com)
    {
        if (com != null && Marshal.IsComObject(com))
            Marshal.FinalReleaseComObject(com);
    }
}
