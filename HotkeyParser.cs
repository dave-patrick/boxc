namespace BoxC;

internal static class HotkeyParser
{
    public const uint ModAlt = 1, ModControl = 2, ModShift = 4, ModWin = 8, ModNoRepeat = 0x4000;
    public const string Default = "Ctrl+Shift+X";

    public static bool TryParse(string text, out uint mods, out uint vk)
    {
        mods = ModNoRepeat; vk = 0;
        foreach (var raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl": case "control": mods |= ModControl; break;
                case "shift": mods |= ModShift; break;
                case "alt": mods |= ModAlt; break;
                case "win": case "windows": mods |= ModWin; break;
                default:
                    if (vk != 0) return false;
                    if (Enum.TryParse<Keys>(raw, true, out var k) && k != Keys.None) vk = (uint)k;
                    else if (raw.Length == 1 && char.IsDigit(raw[0])) vk = (uint)(Keys.D0 + (raw[0] - '0'));
                    else return false;
                    break;
            }
        }
        return vk != 0 && (mods & ~ModNoRepeat) != 0;
    }
}
