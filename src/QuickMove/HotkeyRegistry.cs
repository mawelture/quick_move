using System.Runtime.InteropServices;

namespace QuickMove;

public sealed class HotkeyRegistry : NativeWindow, IDisposable
{
    const int WmHotkey = 0x0312;
    const uint PmRemove = 0x0001;
    static readonly IntPtr HwndMessage = new(-3);

    public event Action<int>? Pressed;

    public HotkeyRegistry()
    {
        CreateHandle(new CreateParams { Parent = HwndMessage });
    }

    public IReadOnlyList<int> Register(IReadOnlyList<Binding> bindings)
    {
        UnregisterAll();
        var failed = new List<int>();
        for (var i = 0; i < bindings.Count && i < AppConfig.RowCount; i++)
        {
            var row = bindings[i];
            if (!row.Enabled || row.Hotkey == null || string.IsNullOrWhiteSpace(row.Directory))
                continue;
            var ok = RegisterHotKey(
                Handle,
                i + 1,
                HotkeyNative.ToModifiers(row.Hotkey.Modifiers),
                HotkeyNative.ToVirtualKey(row.Hotkey.Key));
            if (!ok)
                failed.Add(i);
        }

        return failed;
    }

    public void UnregisterAll()
    {
        if (Handle == IntPtr.Zero)
            return;
        for (var id = 1; id <= AppConfig.RowCount; id++)
            UnregisterHotKey(Handle, id);
    }

    public void SwallowPendingHotkeys()
    {
        if (Handle == IntPtr.Zero)
            return;
        while (PeekMessage(out _, Handle, WmHotkey, WmHotkey, PmRemove))
        {
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
        {
            var row = m.WParam.ToInt32() - 1;
            if (row >= 0 && row < AppConfig.RowCount)
                Pressed?.Invoke(row);
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterAll();
        DestroyHandle();
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Msg
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    static extern bool PeekMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);
}
