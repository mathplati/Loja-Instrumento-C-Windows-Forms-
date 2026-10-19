using System.Globalization;
using Sonora.Core;

namespace Sonora.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => MessageBox.Show(e.Exception.Message, "Sonora · Não foi possível concluir", MessageBoxButtons.OK, MessageBoxIcon.Error);
        var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sonora", "sales.json");
        try
        {
            using var context = new SonoraApplicationContext(new SaleStore(data));
            Application.Run(context);
        }
        catch (Exception error) { MessageBox.Show(error.Message, "Sonora · Falha ao iniciar", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
