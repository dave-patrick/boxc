using System.Drawing.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace BoxC;

internal static class OcrService
{
    /// <summary>Runs Windows' built-in OCR on a bitmap. Returns null if no OCR language is available.</summary>
    public static async Task<string?> RecognizeAsync(Bitmap bmp, bool join)
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages()
                     ?? OcrEngine.AvailableRecognizerLanguages.Select(OcrEngine.TryCreateFromLanguage).FirstOrDefault(e => e != null);
        if (engine == null) return null;

        // OCR works poorly on tiny glyphs: upscale small crops, and respect the engine's max dimension.
        using var work = Prepare(bmp, (int)OcrEngine.MaxImageDimension);

        using var ms = new MemoryStream();
        work.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        using var ras = ms.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(ras);
        using var soft = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var result = await engine.RecognizeAsync(soft);

        var table = TryTable(result, Settings.CleanIcons);
        if (table != null) return table;

        if (!join) return string.Join(Environment.NewLine, result.Lines.Select(l => l.Text)).Trim();

        // Join wrapped lines into paragraphs: a big vertical gap or a bullet/number starts a new line.
        var sb = new System.Text.StringBuilder();
        double prevBottom = 0, prevHeight = 0;
        bool first = true;
        foreach (var l in result.Lines)
        {
            var text = l.Text.Trim();
            if (text.Length == 0 || l.Words.Count == 0) continue;
            double top = l.Words.Min(w => w.BoundingRect.Top), bottom = l.Words.Max(w => w.BoundingRect.Bottom);
            double height = bottom - top;
            if (!first)
            {
                double gap = top - prevBottom;
                bool item = text.Length > 1 && (("•·▪◦-*–".IndexOf(text[0]) >= 0 && text[1] == ' ') ||
                            (char.IsDigit(text[0]) && System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d{1,3}[.)]\s")));
                if (gap > Math.Max(prevHeight, height) * 0.9) sb.Append(Environment.NewLine).Append(Environment.NewLine);
                else if (item) sb.Append(Environment.NewLine);
                else sb.Append(' ');
            }
            sb.Append(text);
            prevBottom = bottom; prevHeight = height; first = false;
        }
        return sb.ToString().Trim();
    }

    private sealed record Seg(string Text, double L, double R, double T, double B);

    /// <summary>
    /// Windows OCR returns each table column as separate lines. If several lines share the same
    /// vertical bands (≥2 bands with ≥3 segments), rebuild visual rows: segments left→right, tab between cells.
    /// Returns null when the layout doesn't look like a table.
    /// </summary>
    private static string? TryTable(OcrResult result, bool cleanIcons)
    {
        var segs = result.Lines.Where(l => l.Words.Count > 0 && l.Text.Trim().Length > 0).Select(l => new Seg(
            l.Text.Trim(),
            l.Words.Min(w => w.BoundingRect.Left), l.Words.Max(w => w.BoundingRect.Right),
            l.Words.Min(w => w.BoundingRect.Top), l.Words.Max(w => w.BoundingRect.Bottom))).OrderBy(s => s.T).ToList();

        var bands = new List<List<Seg>>();
        foreach (var s in segs)
        {
            var band = bands.FirstOrDefault(b =>
            {
                double top = b.Max(x => x.T), bot = b.Min(x => x.B);       // common overlap of the band
                double overlap = Math.Min(bot, s.B) - Math.Max(top, s.T);
                return overlap > 0.5 * Math.Min(s.B - s.T, bot - top);
            });
            if (band == null) bands.Add(new List<Seg> { s }); else band.Add(s);
        }
        if (bands.Count(b => b.Count >= 3) < 2) return null;

        var rows = new List<string>();
        foreach (var b in bands.OrderBy(b => b.Average(x => x.T)))
        {
            var ordered = b.OrderBy(x => x.L).ToList();
            // Drop OCR junk from file/row icons: a tiny leading fragment like "B]" or "[]" before the first real cell.
            if (cleanIcons && ordered.Count > 1)
            {
                var f = ordered[0];
                bool tiny = f.Text.Length <= 2 || (f.Text.Length <= 3 && f.Text.Any(c => !char.IsLetterOrDigit(c)));
                if (tiny && f.R - f.L <= 2.5 * Math.Max(f.B - f.T, 1)) ordered.RemoveAt(0);
            }
            // Same junk glued onto the front of the first cell ("El boxc-0.1.0.exe"): strip a 1-2 char first word before a longer one.
            if (cleanIcons)
            {
                var f = ordered[0];
                var stripped = System.Text.RegularExpressions.Regex.Replace(f.Text, @"^\S{1,2}\s+(?=\S{4,})", "");
                if (stripped != f.Text) ordered[0] = f with { Text = stripped };
            }
            var sb = new System.Text.StringBuilder(ordered[0].Text);
            for (int i = 1; i < ordered.Count; i++)
            {
                double h = Math.Max(ordered[i].B - ordered[i].T, 1);
                double gap = ordered[i].L - ordered[i - 1].R;
                sb.Append(gap < 0.25 * h ? "" : gap < 2 * h ? " " : "\t").Append(ordered[i].Text);
            }
            rows.Add(sb.ToString());
        }
        return string.Join(Environment.NewLine, rows);
    }

    private static Bitmap Prepare(Bitmap src, int maxDim)
    {
        double scale = src.Height < 60 ? 3 : src.Height < 200 ? 2 : 1;
        scale = Math.Min(scale, Math.Min((double)maxDim / src.Width, (double)maxDim / src.Height));
        if (scale >= 0.999 && scale <= 1.001) return new Bitmap(src);
        int w = Math.Max(1, (int)(src.Width * scale)), h = Math.Max(1, (int)(src.Height * scale));
        var dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(dst);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.DrawImage(src, 0, 0, w, h);
        return dst;
    }
}
