using System.Runtime.InteropServices;

namespace QuickMove;

public sealed class SettingsForm : Form
{
    const int VkLWin = 0x5B;
    const int VkRWin = 0x5C;

    readonly record struct RowControls(CheckBox Enabled, TextBox Hotkey, TextBox Directory);

    readonly RowControls[] _rows = new RowControls[AppConfig.RowCount];
    readonly Label _status;
    readonly Button _save;
    readonly Func<bool> _canSave;
    readonly Func<AppConfig, IReadOnlyList<string>> _persist;

    public SettingsForm(AppConfig config, Func<bool> canSave, Func<AppConfig, IReadOnlyList<string>> persist)
    {
        _canSave = canSave;
        _persist = persist;
        Text = "快捷移动";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(720, 320);
        Font = new Font("Microsoft YaHei UI", 9F);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            RowCount = AppConfig.RowCount,
            AutoSize = true,
            Padding = new Padding(12),
            Location = new Point(0, 0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

        for (var i = 0; i < AppConfig.RowCount; i++)
        {
            var enabled = new CheckBox { Text = "启用", AutoSize = true, Anchor = AnchorStyles.Left };
            var hotkey = new TextBox { ReadOnly = true, Dock = DockStyle.Fill, ShortcutsEnabled = false };
            hotkey.KeyDown += HotkeyBox_KeyDown;
            var directory = new TextBox { Dock = DockStyle.Fill };
            var browse = new Button { Text = "浏览", Dock = DockStyle.Fill };
            var directoryBox = directory;
            browse.Click += (_, _) => Browse(directoryBox);
            table.Controls.Add(enabled, 0, i);
            table.Controls.Add(hotkey, 1, i);
            table.Controls.Add(directory, 2, i);
            table.Controls.Add(browse, 3, i);
            _rows[i] = new RowControls(enabled, hotkey, directory);
        }

        _status = new Label
        {
            AutoSize = false,
            Location = new Point(12, 230),
            Size = new Size(690, 40)
        };
        _save = new Button { Text = "保存", Location = new Point(520, 275), Size = new Size(80, 28) };
        var cancel = new Button { Text = "取消", Location = new Point(610, 275), Size = new Size(80, 28) };
        _save.Click += (_, _) => Save();
        cancel.Click += (_, _) => Close();
        AcceptButton = _save;
        CancelButton = cancel;
        Controls.Add(table);
        Controls.Add(_status);
        Controls.Add(_save);
        Controls.Add(cancel);
        LoadConfig(config);
        SetSaveEnabled(canSave());
    }

    public void SetSaveEnabled(bool enabled) => _save.Enabled = enabled;

    void LoadConfig(AppConfig config)
    {
        for (var i = 0; i < AppConfig.RowCount; i++)
        {
            var row = config.Bindings[i];
            _rows[i].Enabled.Checked = row.Enabled;
            _rows[i].Hotkey.Tag = row.Hotkey;
            _rows[i].Hotkey.Text = row.Hotkey == null ? "" : HotkeyText.Format(row.Hotkey);
            _rows[i].Directory.Text = row.Directory ?? "";
        }
    }

    void Browse(TextBox directory)
    {
        using var dialog = new FolderBrowserDialog { Description = "选择目标目录", UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            directory.Text = dialog.SelectedPath;
    }

    void HotkeyBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox box)
            return;
        e.SuppressKeyPress = true;
        if (e.KeyCode == Keys.Back)
        {
            box.Tag = null;
            box.Text = "";
            return;
        }

        if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            return;

        var modifiers = HotkeyModifiers.None;
        if (e.Control)
            modifiers |= HotkeyModifiers.Control;
        if (e.Alt)
            modifiers |= HotkeyModifiers.Alt;
        if (e.Shift)
            modifiers |= HotkeyModifiers.Shift;
        if ((GetAsyncKeyState(VkLWin) & 0x8000) != 0 || (GetAsyncKeyState(VkRWin) & 0x8000) != 0)
            modifiers |= HotkeyModifiers.Win;
        if (modifiers == HotkeyModifiers.None)
            return;

        var hotkey = new Hotkey(modifiers, e.KeyCode);
        box.Tag = hotkey;
        box.Text = HotkeyText.Format(hotkey);
    }

    void Save()
    {
        if (!_canSave())
            return;

        var rows = new Binding[AppConfig.RowCount];
        for (var i = 0; i < AppConfig.RowCount; i++)
        {
            var directory = _rows[i].Directory.Text.Trim();
            rows[i] = new Binding(
                _rows[i].Enabled.Checked,
                _rows[i].Hotkey.Tag as Hotkey,
                directory.Length == 0 ? null : directory);
        }

        ClearHighlight();
        var failure = BindingValidator.Validate(rows);
        if (failure != null)
        {
            Highlight(failure.RowIndex);
            _status.Text = failure.Message;
            return;
        }

        var messages = _persist(new AppConfig { Bindings = rows });
        _status.Text = messages.Count == 0 ? "已保存" : string.Join(Environment.NewLine, messages);
    }

    void Highlight(int rowIndex)
    {
        _rows[rowIndex].Enabled.BackColor = Color.MistyRose;
        _rows[rowIndex].Hotkey.BackColor = Color.MistyRose;
        _rows[rowIndex].Directory.BackColor = Color.MistyRose;
    }

    void ClearHighlight()
    {
        foreach (var row in _rows)
        {
            row.Enabled.BackColor = SystemColors.Control;
            row.Hotkey.BackColor = SystemColors.Window;
            row.Directory.BackColor = SystemColors.Window;
        }
    }

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);
}
