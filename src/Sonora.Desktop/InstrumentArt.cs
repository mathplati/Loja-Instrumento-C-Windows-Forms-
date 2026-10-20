using System.Drawing.Drawing2D;
using Sonora.Core;

namespace Sonora.Desktop;

public sealed class InstrumentArt : Control
{
    private readonly Instrument instrument;

    public InstrumentArt(Instrument instrument)
    {
        this.instrument = instrument;
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        AccessibleName = instrument.Name;
        BackColor = instrument.Color switch
        {
            "peach" => Color.FromArgb(246, 231, 223),
            "sage" => Color.FromArgb(230, 236, 227),
            "lavender" => Color.FromArgb(236, 233, 239),
            _ => Color.FromArgb(240, 236, 223)
        };
        Size = new Size(150, 155);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var scale = Math.Min(Width / 150f, Height / 155f);
        if (scale <= 0) return;
        var state = e.Graphics.Save();
        e.Graphics.TranslateTransform((Width - 150 * scale) / 2, (Height - 155 * scale) / 2);
        e.Graphics.ScaleTransform(scale, scale);
        Draw(e.Graphics, instrument.Symbol);
        e.Graphics.Restore(state);
    }

    internal static void Draw(Graphics g, string symbol)
    {
        using var dark = new SolidBrush(Color.FromArgb(62, 70, 59));
        using var wood = new SolidBrush(Color.FromArgb(209, 163, 103));
        using var light = new SolidBrush(Color.FromArgb(243, 234, 210));
        using var metal = new Pen(Color.FromArgb(126, 137, 118), 2);
        using var outline = new Pen(Color.FromArgb(116, 83, 48), 2);
        using var strings = new Pen(Color.FromArgb(226, 219, 191), 1);
        using var shadow = new SolidBrush(Color.FromArgb(24, 40, 44, 32));
        g.FillEllipse(shadow, 29, 137, 91, 8);

        if (symbol == "keyboard")
        {
            g.FillRectangle(dark, 10, 52, 130, 49);
            g.FillRectangle(light, 18, 77, 114, 19);
            for (var x = 27; x < 132; x += 9) g.DrawLine(metal, x, 77, x, 96);
            for (var i = 0; i < 12; i++) if (i % 7 is not 2 and not 6) g.FillRectangle(dark, 24 + i * 9, 77, 5, 12);
            using var screen = new SolidBrush(Color.FromArgb(161, 181, 143));
            g.FillRectangle(screen, 62, 60, 27, 9); g.FillEllipse(dark, 23, 60, 9, 9);
            g.DrawLine(metal, 33, 103, 108, 138); g.DrawLine(metal, 115, 103, 40, 138);
            return;
        }

        if (symbol == "drums")
        {
            using var red = new SolidBrush(Color.FromArgb(162, 94, 76));
            using var cymbal = new SolidBrush(Color.FromArgb(191, 165, 101));
            g.DrawLine(metal, 27, 42, 19, 130); g.DrawLine(metal, 121, 42, 132, 130);
            g.DrawLine(metal, 19, 130, 10, 136); g.DrawLine(metal, 132, 130, 141, 136);
            g.FillEllipse(cymbal, 4, 35, 48, 10); g.FillEllipse(cymbal, 100, 35, 47, 10);
            g.FillRectangle(red, 39, 54, 29, 27); g.FillRectangle(red, 81, 54, 29, 27);
            g.DrawRectangle(metal, 39, 54, 29, 27); g.DrawRectangle(metal, 81, 54, 29, 27);
            g.FillEllipse(red, 42, 79, 65, 65); g.DrawEllipse(metal, 42, 79, 65, 65);
            g.FillEllipse(light, 49, 86, 51, 51); g.FillEllipse(dark, 80, 119, 9, 9);
            g.DrawLine(outline, 54, 26, 97, 48); g.DrawLine(outline, 53, 48, 98, 26);
            return;
        }

        if (symbol == "cajon")
        {
            Point[] top = [new(39, 33), new(99, 38), new(116, 58), new(54, 53)];
            Point[] side = [new(39, 33), new(54, 53), new(54, 137), new(39, 117)];
            g.FillPolygon(wood, top); g.FillPolygon(dark, side);
            g.FillRectangle(wood, 54, 53, 62, 84); g.DrawRectangle(outline, 54, 53, 62, 84);
            g.FillEllipse(dark, 73, 88, 25, 25);
            g.DrawLine(outline, 61, 62, 109, 66); g.DrawLine(outline, 61, 128, 109, 132);
            return;
        }

        var transform = g.Save();
        g.TranslateTransform(72, 78); g.RotateTransform(17); g.TranslateTransform(-72, -78);
        g.FillRectangle(dark, 67, 18, 9, 76); g.FillRectangle(wood, 64, 4, 15, 23);
        using var body = new GraphicsPath();
        body.AddBezier(62, 73, 43, 61, 31, 78, 39, 91);
        body.AddBezier(39, 91, 43, 103, 22, 107, 29, 127);
        body.AddBezier(29, 127, 37, 149, 80, 153, 91, 131);
        body.AddBezier(91, 131, 100, 113, 79, 105, 87, 92);
        body.AddBezier(87, 92, 96, 77, 83, 64, 77, 73);
        body.CloseFigure();
        using var bass = new SolidBrush(Color.FromArgb(108, 132, 100));
        g.FillPath(symbol == "bass" ? bass : wood, body); g.DrawPath(outline, body);
        if (symbol == "acoustic") { g.FillEllipse(dark, 54, 94, 25, 25); g.DrawEllipse(outline, 51, 91, 31, 31); }
        else { g.FillEllipse(light, 45, 80, 33, 48); g.FillRectangle(dark, 56, 107, 21, 5); }
        g.FillRectangle(dark, 50, 128, 25, 6);
        for (var x = 68; x <= 73; x += 2) g.DrawLine(strings, x, 12, x - 8, 131);
        for (var y = 7; y <= 21; y += 7) { g.DrawLine(metal, 61, y, 65, y); g.DrawLine(metal, 78, y, 82, y); }
        g.Restore(transform);
    }
}

internal sealed class HeroBanner : Panel
{
    internal HeroBanner(Action beginSale)
    {
        DoubleBuffered = true; BackColor = Ui.Ink; Height = 207; Padding = new Padding(24);
        SetStyle(ControlStyles.ResizeRedraw, true);
        var eyebrow = Ui.Text("SUA LOJA, EM HARMONIA", 9, true); eyebrow.ForeColor = Color.FromArgb(189, 204, 172); eyebrow.Location = new Point(27, 21);
        var heading = Ui.Text("O próximo som começa aqui.", 25, true); heading.ForeColor = Color.White; heading.Location = new Point(24, 47);
        var subtitle = Ui.Text("Instrumentos que inspiram. Uma gestão que acompanha.", 10); subtitle.ForeColor = Color.FromArgb(189, 204, 172); subtitle.Location = new Point(27, 103);
        var button = Ui.Button("Explorar instrumentos  →", beginSale); button.Location = new Point(27, 145); button.MinimumSize = new Size(180, 36);
        Controls.AddRange([eyebrow, heading, subtitle, button]);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 850) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var line = new Pen(Color.FromArgb(74, 91, 64));
        for (var i = 0; i < 3; i++) e.Graphics.DrawEllipse(line, Width - 270 + i * 20, -15 + i * 20, 240 - i * 40, 240 - i * 40);
        var saved = e.Graphics.Save();
        e.Graphics.TranslateTransform(Width - 220, -37); e.Graphics.ScaleTransform(1.55f, 1.55f);
        InstrumentArt.Draw(e.Graphics, "acoustic"); e.Graphics.Restore(saved);
    }
}
