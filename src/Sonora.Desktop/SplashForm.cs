namespace Sonora.Desktop;

public sealed class SplashForm : Form
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    public event EventHandler? Ready;

    public SplashForm()
    {
        Name = "Splash"; Text = "Sonora · Bem-vindo"; ClientSize = new Size(640, 400);
        FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterScreen;
        BackColor = Ui.Ink; AutoScaleMode = AutoScaleMode.Dpi;
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(48, 50, 48, 35) };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var height in new[] { 24, 100, 44, 35, 4, 22, 35 }) content.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        var brand = Ui.Text("sonora.", 60, true); brand.ForeColor = Color.FromArgb(233, 165, 124); brand.Dock = DockStyle.Fill; brand.AutoSize = false; brand.TextAlign = ContentAlignment.MiddleCenter; brand.Margin = Padding.Empty;
        var slogan = Ui.Text("Cada grande história começa com uma nota.", 11); slogan.ForeColor = Color.FromArgb(190, 202, 176); slogan.Dock = DockStyle.Fill; slogan.AutoSize = false; slogan.TextAlign = ContentAlignment.MiddleCenter;
        var progress = new ProgressBar { Dock = DockStyle.Fill, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 25, Margin = new Padding(90, 0, 90, 0), AccessibleName = "Preparando sua loja" };
        var caption = Ui.Text("PREPARANDO SUA LOJA", 8); caption.ForeColor = Color.FromArgb(166, 183, 148); caption.Dock = DockStyle.Fill; caption.AutoSize = false; caption.TextAlign = ContentAlignment.MiddleCenter;
        content.Controls.Add(brand, 0, 1); content.Controls.Add(slogan, 0, 2); content.Controls.Add(progress, 0, 4); content.Controls.Add(caption, 0, 6);
        Controls.Add(content);
        Shown += (_, _) => timer.Start();
        timer.Tick += (_, _) => { timer.Stop(); Ready?.Invoke(this, EventArgs.Empty); };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}
