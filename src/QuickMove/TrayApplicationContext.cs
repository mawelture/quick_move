namespace QuickMove;

public sealed class TrayApplicationContext : ApplicationContext
{
    readonly NotifyIcon _icon;
    readonly Icon _trayIcon;
    readonly HotkeyRegistry _hotkeys;
    readonly ConfigStore _store = new();
    readonly Form _pump = new() { ShowInTaskbar = false };
    readonly EventWaitHandle _showEvent;
    readonly string _configPath;
    readonly Thread _listener;
    AppConfig _config;
    SettingsForm? _settings;
    int _moving;
    volatile bool _running = true;

    public TrayApplicationContext()
    {
        _ = _pump.Handle;
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\QuickMove.ShowSettings");
        _configPath = ConfigStore.DefaultPath;
        _trayIcon = LoadTrayIcon();
        _icon = new NotifyIcon
        {
            Icon = _trayIcon,
            Text = "快捷移动",
            Visible = true
        };
        var menu = new ContextMenuStrip();
        menu.Items.Add("设置", null, (_, _) => ShowSettings());
        menu.Items.Add("退出", null, (_, _) => ExitThread());
        _icon.ContextMenuStrip = menu;
        _hotkeys = new HotkeyRegistry();
        _hotkeys.Pressed += OnHotkey;
        var loaded = _store.Load(_configPath);
        _config = loaded.Config;
        if (loaded.RecoveredFromCorruptFile)
            Notify("配置文件已损坏，已恢复为空白设置");
        NotifyRegistration(_hotkeys.Register(_config.Bindings));
        _listener = new Thread(WaitForShowSettings) { IsBackground = true, Name = "QuickMove.ShowSettings" };
        _listener.Start();
    }

    void WaitForShowSettings()
    {
        while (_showEvent.WaitOne())
        {
            if (!_running)
                break;
            try
            {
                _pump.BeginInvoke(new Action(ShowSettings));
            }
            catch (InvalidOperationException)
            {
                break;
            }
        }
    }

    void ShowSettings()
    {
        if (_settings == null || _settings.IsDisposed)
        {
            _settings = new SettingsForm(_config, () => Volatile.Read(ref _moving) == 0, Apply);
            _settings.SetSaveEnabled(Volatile.Read(ref _moving) == 0);
            _settings.Show();
        }
        else
        {
            _settings.SetSaveEnabled(Volatile.Read(ref _moving) == 0);
            _settings.Activate();
        }
    }

    IReadOnlyList<string> Apply(AppConfig config)
    {
        _config = config;
        _store.Save(_configPath, config);
        return NotifyRegistration(_hotkeys.Register(config.Bindings));
    }

    IReadOnlyList<string> NotifyRegistration(IReadOnlyList<int> failedRows)
    {
        var messages = failedRows
            .Select(index => $"{HotkeyText.Format(_config.Bindings[index].Hotkey!)} 已被占用，这一行未启用")
            .ToArray();
        if (messages.Length > 0)
            Notify(string.Join(Environment.NewLine, messages));
        return messages;
    }

    void OnHotkey(int row)
    {
        if (Interlocked.CompareExchange(ref _moving, 1, 0) != 0)
            return;

        try
        {
            var binding = _config.Bindings[row];
            var occupied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(binding.Directory) && Directory.Exists(binding.Directory))
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(binding.Directory))
                {
                    var name = Path.GetFileName(entry);
                    if (!string.IsNullOrEmpty(name))
                        occupied.Add(name);
                }
            }

            var coordination = MoveCoordinator.Prepare(
                binding,
                ExplorerSelection.ReadSelectedPaths(),
                new DiskPathInfo(),
                occupied);
            if (coordination.Message != null || coordination.Plan.All(item => item.Action != MoveAction.Move))
            {
                var text = coordination.Message ?? MoveReport.Build(coordination.Plan, Array.Empty<MoveOutcome>());
                Finish(text);
                return;
            }

            _settings?.SetSaveEnabled(false);
            var moves = coordination.Plan.Where(item => item.Action == MoveAction.Move).ToArray();
            var plan = coordination.Plan;
            Task.Run(() =>
            {
                MoveOutcome[] outcomes;
                try
                {
                    outcomes = moves.Select(item => FileMover.Move(item.SourcePath, item.DestinationPath!)).ToArray();
                }
                catch (Exception ex)
                {
                    _pump.BeginInvoke(new Action(() => Finish("失败：" + ex.Message)));
                    return;
                }

                var text = MoveReport.Build(plan, outcomes);
                _pump.BeginInvoke(new Action(() => Finish(text)));
            });
        }
        catch (Exception ex)
        {
            Finish("失败：" + ex.Message);
        }
    }

    void Finish(string message)
    {
        _hotkeys.SwallowPendingHotkeys();
        Notify(message);
        Interlocked.Exchange(ref _moving, 0);
        _settings?.SetSaveEnabled(true);
    }

    void Notify(string message)
    {
        _icon.BalloonTipTitle = "快捷移动";
        _icon.BalloonTipText = message;
        _icon.ShowBalloonTip(3000);
    }

    protected override void ExitThreadCore()
    {
        _running = false;
        _showEvent.Set();
        _hotkeys.Pressed -= OnHotkey;
        _hotkeys.Dispose();
        _icon.Visible = false;
        _icon.Dispose();
        _trayIcon.Dispose();
        _settings?.Dispose();
        _pump.Dispose();
        _showEvent.Dispose();
        base.ExitThreadCore();
    }

    static Icon LoadTrayIcon() =>
        new(Path.Combine(AppContext.BaseDirectory, "logo_b.ico"));
}
