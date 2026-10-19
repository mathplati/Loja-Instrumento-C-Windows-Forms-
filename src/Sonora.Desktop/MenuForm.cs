using Sonora.Core;

namespace Sonora.Desktop;

public sealed class MenuForm : Form
{
    private readonly SaleStore store;
    private readonly FlowLayoutPanel content = Ui.Stack();
    public MenuForm(SaleStore store)
    {
        this.store = store; Ui.Window(this, "Visão geral"); Controls.Add(content);
        Shown += (_, _) => RefreshDashboard();
    }
    private void RefreshDashboard()
    {
        content.Controls.Clear();
        content.Controls.Add(Ui.Text("sonora.  /  SUA LOJA, EM HARMONIA", 14, true));
        content.Controls.Add(Ui.Text("O próximo som começa aqui.", 32, true));
        content.Controls.Add(Ui.Text("Tudo o que você precisa para transformar instrumentos em novas histórias.", 12));
        var nav = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        nav.Controls.Add(Ui.Button("+  Nova venda", () => { using var form = new SaleForm(store); form.ShowDialog(this); RefreshDashboard(); }, true));
        nav.Controls.Add(Ui.Button("Relatório de vendas  →", () => { using var form = new ReportForm(store); form.ShowDialog(this); }));
        content.Controls.Add(nav);
        try
        {
            var sales = store.List(StoreClock.Today, StoreClock.Today); var summary = Reports.Summarize(sales);
            content.Controls.Add(Ui.Text("HOJE NA SONORA", 11, true));
            content.Controls.Add(Ui.Text($"{Ui.Money(summary.Revenue)} em vendas     ·     {summary.Sales} pedidos     ·     {summary.Instruments} instrumentos", 20, true));
            content.Controls.Add(Ui.Text($"Ticket médio: {Ui.Money(summary.AverageTicket)}", 12));
            content.Controls.Add(Ui.Text("Últimos pedidos", 19, true));
            var recent = store.List().Take(5).ToArray();
            if (recent.Length == 0) content.Controls.Add(Ui.Text("Sua primeira venda merece um bis. Comece em Nova venda."));
            foreach (var sale in recent) content.Controls.Add(Ui.Text($"{sale.Customer}   ·   {sale.Quantity} instrumento(s)   ·   {Reports.PaymentLabel(sale.Payment)}   ·   {Ui.Money(sale.Total)}"));
        }
        catch (Exception error) { content.Controls.Add(Ui.Text(error.Message)); }
        content.Controls.Add(Ui.Text("UM CATÁLOGO CHEIO DE POSSIBILIDADES", 11, true));
        foreach (var instrument in Catalog.All.Take(4)) content.Controls.Add(Ui.Text($"{instrument.Name}  /  {instrument.Brand}  /  {Ui.Money(instrument.Price)}"));
    }
}
