using Sonora.Core;

namespace Sonora.Desktop;

public sealed class SplashForm : Form
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    public SplashForm(SaleStore store)
    {
        Text = "Sonora · Bem-vindo"; Size = new Size(620, 390); FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen; BackColor = Ui.Ink;
        var brand = Ui.Text("sonora.", 58, true); brand.ForeColor = Ui.Orange; brand.Location = new Point(145, 75);
        var slogan = Ui.Text("Cada grande história começa com uma nota.", 12); slogan.ForeColor = Color.White; slogan.Location = new Point(115, 200);
        var progress = new ProgressBar { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 25, Location = new Point(130, 270), Size = new Size(360, 4) };
        var caption = Ui.Text("Preparando sua loja…", 10); caption.ForeColor = Color.Silver; caption.Location = new Point(230, 294);
        Controls.AddRange([brand, slogan, progress, caption]);
        Shown += (_, _) => timer.Start();
        timer.Tick += (_, _) =>
        {
            timer.Stop(); Hide();
            using var menu = new MenuForm(store); menu.ShowDialog(); Close();
        };
    }
    protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
}
