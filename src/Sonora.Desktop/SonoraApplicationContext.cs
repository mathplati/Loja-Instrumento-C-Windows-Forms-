using Sonora.Core;

namespace Sonora.Desktop;

internal sealed class SonoraApplicationContext : ApplicationContext
{
    private readonly SplashForm splash = new();
    private MenuForm? menu;

    internal SonoraApplicationContext(SaleStore store)
    {
        MainForm = splash;
        splash.Ready += (_, _) =>
        {
            try
            {
                menu = new MenuForm(store);
                MainForm = menu;
                menu.Show();
                splash.Close();
            }
            catch (Exception error)
            {
                Ui.Error(error);
                ExitThread();
            }
        };
        splash.Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { menu?.Dispose(); splash.Dispose(); }
        base.Dispose(disposing);
    }
}
