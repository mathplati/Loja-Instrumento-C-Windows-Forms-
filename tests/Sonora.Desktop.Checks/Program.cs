using System.Globalization;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Text;
using Sonora.Core;

namespace Sonora.Desktop.Checks;

internal static class Program
{
    private static readonly Size[] ClientSizes = [new(1320, 850), new(1080, 740)];
    private static readonly CultureInfo Brazilian = CultureInfo.GetCultureInfo("pt-BR");
    private static int passed;

    [STAThread]
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("As verificações nativas do Windows Forms precisam ser executadas no Windows com .NET Desktop Runtime 8.");
            return 1;
        }

        string? temporaryData = null;
        try
        {
            var screenshots = ParseScreenshots(args);
            CultureInfo.DefaultThreadCurrentCulture = Brazilian;
            CultureInfo.DefaultThreadCurrentUICulture = Brazilian;
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

            Check("abertura nativa aparece e sinaliza a transição", () =>
            {
                using var splash = new SplashForm();
                var ready = false;
                splash.Ready += (_, _) => ready = true;
                splash.Show(); Pump(splash);
                Expect(splash.Visible, "A tela de abertura não ficou visível.");
                if (screenshots is not null)
                {
                    using var bitmap = new Bitmap(splash.Width, splash.Height);
                    splash.DrawToBitmap(bitmap, new Rectangle(Point.Empty, splash.Size));
                    bitmap.Save(Path.Combine(screenshots, "00-abertura.png"), ImageFormat.Png);
                }
                var deadline = Stopwatch.StartNew();
                while (!ready && deadline.Elapsed < TimeSpan.FromSeconds(4))
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
                Expect(ready, "A abertura não sinalizou a transição dentro do prazo.");
                splash.Close();
            });

            temporaryData = Path.Combine(Path.GetTempPath(), "sonora-desktop-checks-" + Guid.NewGuid().ToString("N"));
            var store = new SaleStore(Path.Combine(temporaryData, "sales.json"));
            using var menu = new MenuForm(store);
            menu.StartPosition = FormStartPosition.Manual;
            menu.Location = Point.Empty;
            menu.Show();
            Pump(menu);

            Check("menu nativo abre com armazenamento temporário vazio", () =>
            {
                Expect(menu.Visible, "O menu não ficou visível.");
                Expect(store.List().Count == 0, "O armazenamento de verificação não começou vazio.");
                foreach (var name in new[] { "Nav_home", "Nav_sale", "Nav_reports" }) Find<Button>(menu, name);
            });
            Check("navegação acessível nos dois tamanhos de cliente", () =>
                InspectSizes(menu, screenshots, "01-menu", "Nav_home", "Nav_sale", "Nav_reports"));

            Click(menu, "Nav_sale");
            var salePage = Descendants(menu).OfType<SaleForm>().Single();
            var customer = Find<TextBox>(salePage, "CustomerName");
            var payment = Find<ComboBox>(salePage, "PaymentMethod");
            var discount = Find<NumericUpDown>(salePage, "DiscountPercent");
            Check("navegação abre a tela nativa de venda", () =>
            {
                Expect(salePage.Visible, "A tela de venda não ficou visível.");
                Expect(!Find<Button>(salePage, "CompleteSale").Enabled, "Pedido vazio permite finalização.");
                Find<Button>(salePage, "Add_guitar-strato");
                Find<Button>(salePage, "Add_acoustic-folk");
            });

            customer.Text = "Cliente de verificação";
            payment.SelectedIndex = (int)PaymentMethod.Pix;
            discount.Value = 10m;
            Click(salePage, "Add_guitar-strato");
            Click(salePage, "Add_acoustic-folk");
            Check("guitarra e violão com desconto de 10% totalizam R$ 2.150,10", () =>
            {
                ExpectOrderTotal(salePage, 2150.10m);
                Expect(salePage.HasPendingOrder, "O pedido com dois instrumentos não foi preservado.");
                Expect(Find<Button>(salePage, "CompleteSale").Enabled, "O pedido preenchido não habilitou a finalização.");
            });

            Click(menu, "Nav_home");
            Click(menu, "Nav_sale");
            Check("voltar ao menu e reabrir venda preserva o pedido", () =>
            {
                Expect(salePage.Visible && salePage.HasPendingOrder, "A navegação descartou o pedido.");
                Expect(customer.Text == "Cliente de verificação", "A navegação descartou o cliente.");
                Expect(discount.Value == 10m, "A navegação descartou o desconto.");
                ExpectOrderTotal(salePage, 2150.10m);
            });
            Check("controles essenciais da venda acessíveis nos dois tamanhos", () =>
                InspectSizes(menu, screenshots, "02-venda", "CatalogSearch", "CatalogCategory", "CustomerName",
                    "PaymentMethod", "DiscountPercent", "OrderTotal", "CompleteSale", "ClearOrder"));

            Check("busca e categoria filtram o catálogo sem alterar o pedido", () =>
            {
                var search = Find<TextBox>(salePage, "CatalogSearch");
                var category = Find<ComboBox>(salePage, "CatalogCategory");
                search.Text = "TG-530";
                Pump(menu);
                Find<Button>(salePage, "Add_guitar-strato");
                Expect(salePage.Controls.Find("Add_acoustic-folk", true).Length == 0, "A busca manteve um produto sem correspondência.");
                search.Clear();
                category.SelectedItem = "Teclas";
                Pump(menu);
                Find<Button>(salePage, "Add_keyboard-61");
                Expect(salePage.Controls.Find("Add_guitar-strato", true).Length == 0, "A categoria manteve um instrumento de cordas.");
                category.SelectedIndex = 0;
                Pump(menu);
                ExpectOrderTotal(salePage, 2150.10m);
            });

            Sale recorded = null!;
            Check("botão Finalizar venda confirma o pedido real e limpa o formulário", () =>
            {
                var expectedCustomer = customer.Text;
                NativeDialogs.Run(() => Click(salePage, "CompleteSale"), new DialogStep("Tudo em harmonia", dialog =>
                {
                    var message = NativeDialogs.Describe(dialog);
                    Expect(message.Contains("Venda registrada!", StringComparison.Ordinal), "A confirmação não anunciou o registro da venda.");
                    Expect(message.Contains(expectedCustomer, StringComparison.Ordinal), "A confirmação não contém o cliente informado.");
                    Expect(message.Contains(2150.10m.ToString("C2", Brazilian), StringComparison.Ordinal), "A confirmação não contém o total esperado.");
                    NativeDialogs.Capture(dialog, screenshots is null ? null : Path.Combine(screenshots, "05-venda-confirmada.png"));
                    NativeDialogs.Confirm(dialog);
                }));
                recorded = store.List().Single();
                Expect(recorded.Customer == expectedCustomer, "A venda persistiu com outro cliente.");
                Expect(recorded.Total == 2150.10m && recorded.Quantity == 2, "O pedido gravado divergiu dos instrumentos e do total exibidos.");
                var reopened = new SaleStore(Path.Combine(temporaryData, "sales.json")).List().Single();
                Expect(reopened.Id == recorded.Id && reopened.Total == recorded.Total, "A venda não persistiu corretamente.");
                Expect(!salePage.HasPendingOrder, "A venda concluída manteve instrumentos no carrinho.");
                Expect(customer.Text.Length == 0 && discount.Value == 0 && payment.SelectedIndex == 0, "A venda concluída não limpou os campos do pedido.");
                Expect(!Find<Button>(salePage, "CompleteSale").Enabled, "A venda concluída manteve a finalização habilitada.");
                ExpectOrderTotal(salePage, 0m);
            });

            Click(menu, "Nav_reports");
            var reportPage = Descendants(menu).OfType<ReportForm>().Single();
            var from = Find<DateTimePicker>(reportPage, "FromDate");
            var to = Find<DateTimePicker>(reportPage, "ToDate");
            var reportPayment = Find<ComboBox>(reportPage, "ReportPayment");
            var salesGrid = Find<DataGridView>(reportPage, "SalesGrid");
            var saleDay = StoreClock.Date(recorded.CreatedAt).ToDateTime(TimeOnly.MinValue);
            from.Value = saleDay;
            to.Value = saleDay;
            reportPayment.SelectedIndex = 0;
            Click(reportPage, "ApplyFilters");
            Check("relatório nativo mostra a venda real no dia de São Paulo", () =>
            {
                Expect(reportPage.Visible, "A tela de relatório não ficou visível.");
                Expect(salesGrid.Rows.Count == 1, "O relatório deveria conter exatamente a venda de verificação.");
                var row = salesGrid.Rows[0];
                Expect(row.Tag is Sale loaded && loaded.Id == recorded.Id, "O relatório não está associado à venda gravada.");
                Expect(Equals(row.Cells["customer"].Value, recorded.Customer), "O relatório exibiu outro cliente.");
                Expect(Equals(row.Cells["total"].Value, recorded.Total), "O relatório exibiu outro total.");
                salesGrid.CurrentCell = row.Cells[0];
                Pump(menu);
                Expect(Find<Button>(reportPage, "ExportCsv").Enabled, "A exportação não foi habilitada para a venda.");
                Expect(Find<Button>(reportPage, "SaleDetails").Enabled, "Os detalhes não foram habilitados para a venda selecionada.");
            });
            Check("filtros, tabela e ações do relatório acessíveis nos dois tamanhos", () =>
                InspectSizes(menu, screenshots, "03-relatorio", "FromDate", "ToDate", "ReportPayment", "ApplyFilters", "ExportCsv", "SalesGrid", "SaleDetails"));

            Check("detalhes modais mostram o pedido, os instrumentos e os totais reais", () =>
            {
                NativeDialogs.Run(() => Click(reportPage, "SaleDetails"), new DialogStep("Sonora · Detalhes da venda", handle =>
                {
                    var dialog = Control.FromHandle(handle) as Form
                        ?? throw new InvalidOperationException("O diálogo de detalhes não é um formulário Windows Forms.");
                    dialog.Invoke(new Action(() =>
                    {
                        Expect(dialog.Modal && dialog.Visible, "Os detalhes não abriram como diálogo modal.");
                        var labels = Descendants(dialog).OfType<Label>().Select(label => label.Text).ToArray();
                        Expect(labels.Any(text => text.Contains(recorded.Id, StringComparison.Ordinal)), "Os detalhes omitiram o identificador completo do pedido.");
                        Expect(labels.Any(text => text.Contains(recorded.Customer, StringComparison.Ordinal)), "Os detalhes omitiram o cliente.");
                        foreach (var value in new[] { recorded.Subtotal, recorded.Discount, recorded.Total })
                            Expect(labels.Any(text => text.Contains(value.ToString("C2", Brazilian), StringComparison.Ordinal)), "Os detalhes omitiram um dos totais do pedido.");
                        var items = Descendants(dialog).OfType<DataGridView>().Single();
                        Expect(items.Rows.Count == recorded.Items.Count, "Os detalhes exibiram outra quantidade de linhas.");
                        for (var index = 0; index < recorded.Items.Count; index++)
                        {
                            var expected = recorded.Items[index];
                            var actual = items.Rows[index];
                            Expect(Equals(actual.Cells[0].Value, expected.Name) && Equals(actual.Cells[1].Value, expected.Brand), "Os detalhes exibiram outro instrumento ou marca.");
                            Expect(Equals(actual.Cells[2].Value, expected.Quantity) && Equals(actual.Cells[3].Value, expected.UnitPrice)
                                && Equals(actual.Cells[4].Value, expected.Total), "Os detalhes exibiram quantidade ou preços incorretos.");
                        }
                        NativeDialogs.Capture(handle, screenshots is null ? null : Path.Combine(screenshots, "06-detalhes-venda.png"));
                        var close = dialog.AcceptButton as Button ?? throw new InvalidOperationException("O diálogo não tem botão de fechamento.");
                        Expect(close.Text == "Fechar", "O botão padrão dos detalhes não é Fechar.");
                        close.PerformClick();
                    }));
                }));
                Expect(reportPage.Visible && salesGrid.Rows.Count == 1, "Fechar os detalhes alterou o relatório.");
            });

            var exportedFile = Path.Combine(temporaryData, "relatorio-validado.csv");
            Check("exportação real salva CSV com BOM e confirma a venda exportada", () =>
            {
                NativeDialogs.Run(() => Click(reportPage, "ExportCsv"),
                    new DialogStep("Exportar relatório de vendas", dialog =>
                    {
                        NativeDialogs.SetSaveFileName(dialog, exportedFile);
                        NativeDialogs.Capture(dialog, screenshots is null ? null : Path.Combine(screenshots, "07-salvar-csv.png"));
                        NativeDialogs.Confirm(dialog);
                    }),
                    new DialogStep("Relatório exportado", dialog =>
                    {
                        Expect(NativeDialogs.Describe(dialog).Contains("1 venda exportada com sucesso.", StringComparison.Ordinal), "A confirmação da exportação não informou uma venda.");
                        NativeDialogs.Capture(dialog, screenshots is null ? null : Path.Combine(screenshots, "08-exportacao-confirmada.png"));
                        NativeDialogs.Confirm(dialog);
                    }));
                Expect(File.Exists(exportedFile), "O diálogo de salvar não criou o arquivo CSV solicitado.");
                Expect(File.ReadAllBytes(exportedFile).AsSpan().StartsWith(Encoding.UTF8.GetPreamble()), "O CSV não contém BOM UTF-8 para leitura no Excel.");
                Expect(File.ReadAllText(exportedFile, Encoding.UTF8) == Reports.Csv([recorded]), "O CSV salvo diverge da venda exibida no relatório.");
            });

            Check("cancelar o diálogo de exportação preserva os arquivos e os dados", () =>
            {
                var canceledFile = Path.Combine(temporaryData, "exportacao-cancelada.csv");
                var previousExport = File.ReadAllBytes(exportedFile);
                NativeDialogs.Run(() => Click(reportPage, "ExportCsv"), new DialogStep("Exportar relatório de vendas", dialog =>
                {
                    NativeDialogs.SetSaveFileName(dialog, canceledFile);
                    NativeDialogs.Cancel(dialog);
                }));
                Expect(!File.Exists(canceledFile), "Cancelar a exportação criou um arquivo.");
                Expect(previousExport.SequenceEqual(File.ReadAllBytes(exportedFile)), "Cancelar a exportação alterou o CSV existente.");
                Expect(store.List().Count == 1 && salesGrid.Rows.Count == 1, "Cancelar a exportação alterou os dados da venda.");
                Expect(Find<Button>(reportPage, "ExportCsv").Enabled, "Cancelar a exportação bloqueou uma nova tentativa.");
            });

            Check("filtro por dinheiro exclui a venda Pix e bloqueia ações sem resultados", () =>
            {
                reportPayment.SelectedIndex = (int)PaymentMethod.Dinheiro + 1;
                Pump(menu);
                Expect(!Find<Button>(reportPage, "ExportCsv").Enabled, "A exportação continuou habilitada com filtros pendentes.");
                Expect(!Find<Button>(reportPage, "SaleDetails").Enabled, "Os detalhes continuaram habilitados com filtros pendentes.");
                Click(reportPage, "ApplyFilters");
                Expect(salesGrid.Rows.Count == 0, "O filtro de dinheiro não excluiu a venda Pix.");
                Expect(!Find<Button>(reportPage, "ExportCsv").Enabled, "A exportação foi habilitada sem resultados.");
                Expect(!Find<Button>(reportPage, "SaleDetails").Enabled, "Os detalhes foram habilitados sem resultados.");
                InspectSizes(menu, screenshots, "04-relatorio-vazio", "FromDate", "ToDate", "ReportPayment", "ApplyFilters", "SalesGrid");
            });
            Check("filtros por pagamento e período voltam a incluir a venda", () =>
            {
                reportPayment.SelectedIndex = (int)PaymentMethod.Pix + 1;
                Click(reportPage, "ApplyFilters");
                Expect(salesGrid.Rows.Count == 1, "O filtro Pix não encontrou a venda gravada.");
                from.Value = saleDay.AddDays(1);
                to.Value = saleDay.AddDays(1);
                Click(reportPage, "ApplyFilters");
                Expect(salesGrid.Rows.Count == 0, "O filtro de data incluiu uma venda de outro dia.");
                from.Value = saleDay;
                to.Value = saleDay;
                Click(reportPage, "ApplyFilters");
                Expect(salesGrid.Rows.Count == 1, "O filtro do dia da venda não voltou a incluí-la.");
            });

            Check("Ctrl+Enter salva a quantidade digitada sem sair do campo numérico", () =>
            {
                Click(menu, "Nav_sale");
                customer.Text = "Cliente Atalho";
                Click(salePage, "Add_guitar-strato");
                var amount = Find<NumericUpDown>(salePage, "Quantity_guitar-strato");
                ExpectUnclipped(amount);
                var editor = amount.Controls.OfType<TextBox>().Single();
                menu.Activate();
                editor.Focus();
                Pump(menu);
                Expect(editor.Focused, "O editor de quantidade não recebeu foco para o atalho.");
                // O TextBox interno mantém UserEdit pendente. Ler amount.Value ou
                // definir amount.Text validaria o número antes do atalho e mascararia a regressão.
                editor.Text = "3";
                Expect(editor.Focused && editor.Text == "3", "A edição perdeu foco antes de Ctrl+Enter.");
                ExpectOrderTotal(salePage, 1290m);
                NativeDialogs.Run(() => SendKeys.SendWait("^{ENTER}"), new DialogStep("Tudo em harmonia", dialog =>
                {
                    var message = NativeDialogs.Describe(dialog);
                    Expect(message.Contains("Cliente Atalho", StringComparison.Ordinal), "O atalho não concluiu o pedido informado.");
                    Expect(message.Contains(3870m.ToString("C2", Brazilian), StringComparison.Ordinal), "A confirmação do atalho perdeu a quantidade digitada.");
                    NativeDialogs.Capture(dialog, screenshots is null ? null : Path.Combine(screenshots, "09-atalho-quantidade-confirmada.png"));
                    NativeDialogs.Confirm(dialog);
                }));
                var saved = store.List().Single(sale => sale.Customer == "Cliente Atalho");
                Expect(saved.Quantity == 3 && saved.Items.Single().Quantity == 3 && saved.Total == 3870m,
                    "Ctrl+Enter gravou a quantidade anterior à edição pendente.");
                Expect(store.List().Count == 2 && !salePage.HasPendingOrder, "O atalho não registrou uma única venda e limpou o carrinho.");
            });

            Console.WriteLine($"\n{passed} verificações nativas aprovadas.");
            if (screenshots is not null) Console.WriteLine($"Capturas reais do Windows Forms: {screenshots}");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL após {passed} verificações nativas: {error}");
            return 1;
        }
        finally
        {
            if (temporaryData is not null && Directory.Exists(temporaryData)) Directory.Delete(temporaryData, true);
        }
    }

    private static string? ParseScreenshots(string[] args)
    {
        if (args.Length == 0) return null;
        if (args.Length != 2 || args[0] != "--screenshots" || string.IsNullOrWhiteSpace(args[1]))
            throw new ArgumentException("Uso: dotnet run --project tests/Sonora.Desktop.Checks -c Release -- [--screenshots <pasta>]");
        var directory = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void Check(string name, Action run)
    {
        run();
        passed++;
        Console.WriteLine($"PASS {name}");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static T Find<T>(Control root, string name) where T : Control =>
        root.Controls.Find(name, true).OfType<T>().SingleOrDefault()
        ?? throw new InvalidOperationException($"Controle {name} ({typeof(T).Name}) não encontrado em {root.GetType().Name}.");

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            yield return control;
            foreach (var descendant in Descendants(control)) yield return descendant;
        }
    }

    private static void Click(Control root, string name)
    {
        var button = Find<Button>(root, name);
        ExpectUnclipped(button);
        Expect(button.Visible && button.Enabled, $"O botão {name} não está visível e habilitado.");
        button.PerformClick();
        Pump(root);
    }

    private static void Pump(Control root)
    {
        root.PerformLayout();
        Application.DoEvents();
        root.Update();
        Application.DoEvents();
    }

    private static void ExpectOrderTotal(SaleForm page, decimal expected) =>
        Expect(Find<Label>(page, "OrderTotal").Text.Contains(expected.ToString("N2", Brazilian), StringComparison.Ordinal),
            $"O total visível deveria ser {expected.ToString("C2", Brazilian)}.");

    private static void InspectSizes(Form window, string? screenshots, string prefix, params string[] names)
    {
        foreach (var size in ClientSizes)
        {
            window.ClientSize = size;
            Pump(window);
            Expect(window.ClientSize == size,
                $"A sessão Windows limitou o cliente a {window.ClientSize}; é necessário acomodar {size.Width}x{size.Height}.");
            foreach (var name in names) ExpectUnclipped(Find<Control>(window, name));
            foreach (var scroll in Descendants(window).OfType<ScrollableControl>().Where(control => control.AutoScroll).Reverse())
                scroll.AutoScrollPosition = Point.Empty;
            Pump(window);
            if (screenshots is null) continue;
            using var bitmap = new Bitmap(window.Width, window.Height);
            window.DrawToBitmap(bitmap, new Rectangle(Point.Empty, window.Size));
            bitmap.Save(Path.Combine(screenshots, $"{prefix}-{size.Width}x{size.Height}.png"), ImageFormat.Png);
        }
    }

    private static void ExpectUnclipped(Control control)
    {
        ScrollIntoView(control);
        Expect(control.Visible && control.Width > 0 && control.Height > 0, $"O controle {control.Name} não está visível.");
        var bounds = control.RectangleToScreen(control.ClientRectangle);
        var visible = bounds;
        for (var parent = control.Parent; parent is not null; parent = parent.Parent)
            visible.Intersect(parent.RectangleToScreen(parent.ClientRectangle));
        visible.Inflate(2, 2); // Tolera apenas bordas nativas; conteúdo cortado continua sendo erro.
        Expect(visible.Contains(bounds), $"O controle {control.Name} continua cortado após rolagem: área {bounds}, área visível {visible}.");
    }

    private static void ScrollIntoView(Control control)
    {
        // Primeiro revela o controle no contêiner mais próximo, depois revela esse
        // trecho nos contêineres externos. O viewport continua sendo verificado.
        for (var parent = control.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is not ScrollableControl { AutoScroll: true } scroll) continue;
            scroll.ScrollControlIntoView(control);
            Pump(control.TopLevelControl ?? control);
        }
    }
}
