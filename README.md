# boxc

Tiny standalone Windows tool: press a hotkey, drag a box (or draw a loop) around text anywhere on screen, and the recognized text is copied to the clipboard. Uses Windows' built-in offline OCR — no installs, no network, no dependencies.

## Use
- Run `boxc.exe` (lives in the system tray; single instance).
- **Ctrl+Shift+X** (or left-click the tray icon) → screen dims.
  - **Drag** → rectangle. **Shift+drag** → freehand lasso. **Esc / right-click** → cancel.
- Release → text is on your clipboard (tray toast confirms).
- Tray menu: Capture, Start with Windows, About (release + build ID), Exit.

## Settings
Tray menu → **Settings...**: click the hotkey box and press a new combination (needs Ctrl/Shift/Alt), and choose the default selection mode — *drag a box* or *draw around*. "Hold Shift to toggle mode" (on by default) lets Shift switch to the other mode while selecting. **Join wrapped lines into paragraphs** merges soft-wrapped lines with spaces, keeping breaks at bullets/numbered items and blank lines between paragraphs. **Remove icon junk from table rows** (on by default) drops stray 1–3 character OCR fragments (e.g. "B]") that appear where Explorer-style row icons are. Tables are detected automatically and copied as tab-separated rows. Applied immediately; saved to `%APPDATA%\boxc\boxc.ini` (`hotkey=Ctrl+Alt+C`, `mode=box|lasso`, `joinlines=0|1`).

## OCR languages
Recognizes the languages in your Windows language list (Settings → Time & language → Language & region).

## Build
Requires the .NET 10 SDK (build only; the output needs nothing).
```
dotnet publish -c Release -o out
```
Produces a single self-contained `out\boxc.exe` (~58 MB). Release version and build ID are set in `boxc.csproj` (`Version`, `BuildId`); `nuget.config` points restore at nuget.org.
