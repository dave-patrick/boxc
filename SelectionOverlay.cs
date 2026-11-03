using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace BoxC;

/// <summary>Full-virtual-screen dimmed overlay. Drag = box, Shift+drag = freehand lasso, Esc/right-click = cancel.</summary>
internal sealed class SelectionOverlay : Form
{
    private readonly Bitmap _shot;
    private readonly Rectangle _screen;
    private bool _dragging, _lasso;
    private Point _start, _cur;
    private readonly List<Point> _path = new();

    public Bitmap? Result { get; private set; }

    public SelectionOverlay()
    {
        _screen = SystemInformation.VirtualScreen;
        _shot = new Bitmap(_screen.Width, _screen.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(_shot))
            g.CopyFromScreen(_screen.Location, Point.Empty, _screen.Size);

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = _screen;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        KeyPreview = true;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
        Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Result = null; Close(); }
        base.OnKeyDown(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right) { Result = null; Close(); return; }
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        _lasso = Settings.Lasso ^ (Settings.ShiftToggle && (ModifierKeys & Keys.Shift) != 0);
        _start = _cur = e.Location;
        _path.Clear();
        _path.Add(e.Location);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_dragging) return;
        _cur = e.Location;
        if (_lasso) _path.Add(e.Location);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!_dragging || e.Button != MouseButtons.Left) return;
        _dragging = false;
        _cur = e.Location;
        Result = Crop();
        Close();
    }

    private Rectangle SelectionRect()
    {
        if (_lasso && _path.Count > 1)
        {
            int x1 = _path.Min(p => p.X), y1 = _path.Min(p => p.Y), x2 = _path.Max(p => p.X), y2 = _path.Max(p => p.Y);
            return Rectangle.FromLTRB(x1, y1, x2, y2);
        }
        return Rectangle.FromLTRB(Math.Min(_start.X, _cur.X), Math.Min(_start.Y, _cur.Y),
                                  Math.Max(_start.X, _cur.X), Math.Max(_start.Y, _cur.Y));
    }

    private Bitmap? Crop()
    {
        var r = Rectangle.Intersect(SelectionRect(), new Rectangle(Point.Empty, _shot.Size));
        if (r.Width < 4 || r.Height < 4) return null; // treat a stray click as cancel

        var bmp = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        if (_lasso && _path.Count > 2)
        {
            g.Clear(Color.White);
            using var gp = new GraphicsPath();
            gp.AddPolygon(_path.Select(p => new Point(p.X - r.X, p.Y - r.Y)).ToArray());
            g.SetClip(gp);
        }
        g.DrawImage(_shot, new Rectangle(0, 0, r.Width, r.Height), r, GraphicsUnit.Pixel);
        return bmp;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.DrawImageUnscaled(_shot, 0, 0);
        using var dim = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
        using var pen = new Pen(Color.FromArgb(255, 0, 160, 255), 2);

        if (!_dragging) { g.FillRectangle(dim, ClientRectangle); return; }

        if (_lasso && _path.Count > 2)
        {
            using var gp = new GraphicsPath();
            gp.AddPolygon(_path.ToArray());
            var state = g.Save();
            g.ExcludeClip(new Region(gp));
            g.FillRectangle(dim, ClientRectangle);
            g.Restore(state);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawPolygon(pen, _path.ToArray());
        }
        else
        {
            var r = SelectionRect();
            var state = g.Save();
            g.ExcludeClip(r);
            g.FillRectangle(dim, ClientRectangle);
            g.Restore(state);
            g.DrawRectangle(pen, r);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _shot.Dispose();
        base.Dispose(disposing);
    }
}
