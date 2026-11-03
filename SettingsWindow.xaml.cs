using System.Windows;
using System.Windows.Input;

namespace BoxC;

public partial class SettingsWindow : Window
{
    private string _hotkey = Settings.Hotkey;

    public string Hotkey => _hotkey;
    public bool Lasso => LassoRadio.IsChecked == true;
    public bool ShiftToggle => ShiftCheck.IsChecked == true;
    public bool JoinLines => JoinCheck.IsChecked == true;
    public bool CleanIcons => IconsCheck.IsChecked == true;

    public SettingsWindow()
    {
        InitializeComponent();
        HotkeyBox.Text = Show(_hotkey);
        (Settings.Lasso ? LassoRadio : BoxRadio).IsChecked = true;
        ShiftCheck.IsChecked = Settings.ShiftToggle;
        JoinCheck.IsChecked = Settings.JoinLines;
        IconsCheck.IsChecked = Settings.CleanIcons;
        // A tray app isn't the foreground app, so force the dialog to the front.
        Loaded += (_, _) => { Topmost = true; Activate(); Topmost = false; };
    }

    private static string Show(string h) => h.Replace("+", " + ");

    private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt
            or Key.LWin or Key.RWin or Key.None or Key.DeadCharProcessed) return;

        var m = Keyboard.Modifiers;
        var parts = new List<string>();
        if (m.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (m.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (m.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (parts.Count == 0) return; // require a modifier

        var vk = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(key);
        var name = vk.ToString();
        if (name.Length == 2 && name[0] == 'D' && char.IsDigit(name[1])) name = name[1..];
        parts.Add(name);

        var text = string.Join("+", parts);
        if (!HotkeyParser.TryParse(text, out _, out _)) return;
        _hotkey = text;
        HotkeyBox.Text = Show(text);
    }

    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
