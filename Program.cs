using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BoxC;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(true, @"Local\boxc-single-instance", out var first);
        if (!first) return;

        ApplicationConfiguration.Initialize();
        using var app = new TrayApp();
        if (args.Contains("--settings")) // open the settings window right away (also handy for testing)
            Application.Idle += OpenOnce;
        if (args.Contains("--about")) // testing aid
            Application.Idle += AboutOnce;
        if (args.Contains("--menu")) // testing aid: pop the tray menu mid-screen
            Application.Idle += MenuOnce;
        Application.Run();

        void MenuOnce(object? s, EventArgs e) { Application.Idle -= MenuOnce; app.ShowMenuAt(new Point(Screen.PrimaryScreen!.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2)); }
        void AboutOnce(object? s, EventArgs e) { Application.Idle -= AboutOnce; app.About(); }
        void OpenOnce(object? s, EventArgs e) { Application.Idle -= OpenOnce; app.ShowSettings(); }
    }
}

internal sealed class TrayApp : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 1;
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr h);

    private sealed class HotWindow : NativeWindow
    {
        public Action? Pressed;
        public HotWindow() => CreateHandle(new CreateParams());
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && m.WParam == HotkeyId) Pressed?.Invoke();
            else base.WndProc(ref m);
        }
    }

    private readonly NotifyIcon _tray;
    private readonly HotWindow _win = new();
    private readonly ToolStripMenuItem _startup;
    private readonly Icon _icon;
    private bool _busy;

    public TrayApp()
    {
        _icon = MakeIcon();
        _startup = new ToolStripMenuItem("Start with Windows") { CheckOnClick = true, Checked = IsStartup() };
        _startup.CheckedChanged += (_, _) => SetStartup(_startup.Checked);

        Settings.Load();
        var menu = new ContextMenuStrip();
        _captureItem = new ToolStripMenuItem("", null, (_, _) => Capture());
        menu.Items.Add(_captureItem);
        menu.Items.Add("Settings...", null, (_, _) => ShowSettings());
        menu.Items.Add(_startup);
        menu.Items.Add("About", null, (_, _) => About());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Application.Exit());
        DarkMenu.Apply(menu);

        _tray = new NotifyIcon { Icon = _icon, Text = "boxc – screen text grabber", ContextMenuStrip = menu, Visible = true };
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) Capture(); };

        _win.Pressed = Capture;
        RegisterCurrent();
    }

    private ToolStripMenuItem _captureItem = null!;
    private string _hotkeyText => Settings.Hotkey;

    private bool RegisterCurrent()
    {
        UnregisterHotKey(_win.Handle, HotkeyId);
        _captureItem.Text = $"Capture text ({_hotkeyText})";
        if (!HotkeyParser.TryParse(_hotkeyText, out var mods, out var vk))
        { Toast($"Invalid hotkey '{_hotkeyText}'.", ToolTipIcon.Error); return false; }
        if (!RegisterHotKey(_win.Handle, HotkeyId, mods, vk))
        { Toast($"Hotkey {_hotkeyText} is in use by another app. Pick another in Settings.", ToolTipIcon.Error); return false; }
        return true;
    }

    public void ShowMenuAt(Point p) => _tray.ContextMenuStrip!.Show(p);

    public void ShowSettings()
    {
        var f = new SettingsWindow();
        if (f.ShowDialog() != true) return;
        var oldKey = Settings.Hotkey;
        Settings.Hotkey = f.Hotkey;
        Settings.Lasso = f.Lasso;
        Settings.ShiftToggle = f.ShiftToggle;
        Settings.JoinLines = f.JoinLines;
        Settings.CleanIcons = f.CleanIcons;
        if (!RegisterCurrent())
        {
            Settings.Hotkey = oldKey;   // revert to the working hotkey
            RegisterCurrent();
        }
        try { Settings.Save(); } catch (Exception ex) { Toast("Could not save settings: " + ex.Message, ToolTipIcon.Error); }
    }

    private async void Capture()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            Bitmap? crop;
            using (var overlay = new SelectionOverlay())
            {
                overlay.ShowDialog();
                crop = overlay.Result;
            }
            if (crop == null) return;

            using (crop)
            {
                var text = await OcrService.RecognizeAsync(crop, Settings.JoinLines);
                if (text == null) Toast("No Windows OCR language is installed (Settings > Time & language > Language).", ToolTipIcon.Error);
                else if (text.Length == 0) Toast("No text found.", ToolTipIcon.Info);
                else
                {
                    Clipboard.SetText(text);
                    Toast($"Copied {text.Length} characters.", ToolTipIcon.Info);
                }
            }
        }
        catch (Exception ex)
        {
            Toast("Capture failed: " + ex.Message, ToolTipIcon.Error);
        }
        finally { _busy = false; }
    }

    private void Toast(string msg, ToolTipIcon icon) => _tray.ShowBalloonTip(2500, "boxc", msg, icon);

    public void About() => new AboutWindow().ShowDialog();

    private static bool IsStartup()
    {
        using var k = Registry.CurrentUser.OpenSubKey(RunKey);
        return k?.GetValue("boxc") != null;
    }

    private static void SetStartup(bool on)
    {
        using var k = Registry.CurrentUser.OpenSubKey(RunKey, true);
        if (k == null) return;
        if (on) k.SetValue("boxc", $"\"{Environment.ProcessPath}\"");
        else k.DeleteValue("boxc", false);
    }

    private static Icon MakeIcon()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("boxc.ico")!;
        return new Icon(s, SystemInformation.SmallIconSize); // picks the best-fit size for the tray DPI
    }

    public void Dispose()
    {
        UnregisterHotKey(_win.Handle, HotkeyId);
        _win.DestroyHandle();
        _tray.Visible = false;
        _tray.Dispose();
        _icon.Dispose();
    }
}
