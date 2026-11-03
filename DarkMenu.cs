namespace BoxC;

/// <summary>Dark tray-menu theme matching the Settings window (#18191c bg, #3b8ec4 accent).</summary>
internal static class DarkMenu
{
    private static readonly Color Bg = ColorTranslator.FromHtml("#18191c");
    private static readonly Color Border = ColorTranslator.FromHtml("#2d3139");
    private static readonly Color Hover = ColorTranslator.FromHtml("#2a2e35");
    private static readonly Color Text = ColorTranslator.FromHtml("#e6e8ea");
    private static readonly Color Accent = ColorTranslator.FromHtml("#3b8ec4");

    public static void Apply(ContextMenuStrip menu)
    {
        menu.Renderer = new Renderer();
        menu.BackColor = Bg;
        menu.ForeColor = Text;
        menu.Font = new Font("Segoe UI Variable Text", 9.75f);
        if (menu.Font.Name != "Segoe UI Variable Text") menu.Font = new Font("Segoe UI", 9.75f);
        menu.ShowImageMargin = false;
        menu.ShowCheckMargin = false;
        menu.Padding = new Padding(0, 4, 0, 4);
        foreach (ToolStripItem i in menu.Items)
        {
            i.ForeColor = Text;
            i.Padding = new Padding(10, 5, 10, 5);
        }
    }

    private sealed class Colors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Bg;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Hover;
        public override Color MenuItemSelected => Hover;
        public override Color ImageMarginGradientBegin => Bg;
        public override Color ImageMarginGradientMiddle => Bg;
        public override Color ImageMarginGradientEnd => Bg;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
        public override Color CheckBackground => Hover;
        public override Color CheckSelectedBackground => Hover;
        public override Color CheckPressedBackground => Hover;
    }

    private sealed class Renderer : ToolStripProfessionalRenderer
    {
        public Renderer() : base(new Colors()) { RoundedEdges = false; }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var b = new SolidBrush(Bg);
            e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var p = new Pen(Border);
            e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled) return;
            var r = new Rectangle(4, 0, e.Item.Width - 8, e.Item.Height);
            using var b = new SolidBrush(Hover);
            e.Graphics.FillRectangle(b, r);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Text;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var p = new Pen(Border);
            int y = e.Item.Height / 2;
            e.Graphics.DrawLine(p, 10, y, e.Item.Width - 10, y);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // Accent check mark at the right edge, instead of the boxed default.
            var r = e.ImageRectangle;
            using var p = new Pen(Accent, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int x = e.Item.Width - 26, y = e.Item.Height / 2;
            e.Graphics.DrawLines(p, new[] { new Point(x, y), new Point(x + 4, y + 4), new Point(x + 11, y - 4) });
        }
    }
}
