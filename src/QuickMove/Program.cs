namespace QuickMove;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var mutex = new Mutex(true, @"Local\QuickMove", out var created);
        if (!created)
        {
            SignalShowSettings();
            return;
        }

        Application.Run(new TrayApplicationContext());
    }

    static void SignalShowSettings()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var show = EventWaitHandle.OpenExisting(@"Local\QuickMove.ShowSettings");
                show.Set();
                return;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Thread.Sleep(50);
            }
        }
    }
}
