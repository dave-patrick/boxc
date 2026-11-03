namespace BoxC;

internal static class Settings
{
    public static string Hotkey = HotkeyParser.Default;
    public static bool Lasso;               // default selection mode: false = drag box, true = draw around
    public static bool ShiftToggle = true;  // holding Shift while selecting switches to the other mode
    public static bool JoinLines;           // merge wrapped lines into paragraphs
    public static bool CleanIcons = true;   // drop OCR junk from row icons in tables

    public static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "boxc", "boxc.ini");

    public static void Load()
    {
        try
        {
            foreach (var line in File.ReadAllLines(Path))
            {
                var i = line.IndexOf('=');
                if (i <= 0) continue;
                var k = line[..i].Trim().ToLowerInvariant();
                var v = line[(i + 1)..].Trim();
                if (k == "hotkey" && v.Length > 0) Hotkey = v;
                else if (k == "mode") Lasso = v.Equals("lasso", StringComparison.OrdinalIgnoreCase);
                else if (k == "shifttoggle") ShiftToggle = v is "1" or "true";
                else if (k == "joinlines") JoinLines = v is "1" or "true";
                else if (k == "cleanicons") CleanIcons = v is "1" or "true";
            }
        }
        catch { }
    }

    public static void Save()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllLines(Path, new[]
        {
            $"hotkey={Hotkey}", $"mode={(Lasso ? "lasso" : "box")}", $"shifttoggle={(ShiftToggle ? 1 : 0)}",
            $"joinlines={(JoinLines ? 1 : 0)}", $"cleanicons={(CleanIcons ? 1 : 0)}"
        });
    }
}
