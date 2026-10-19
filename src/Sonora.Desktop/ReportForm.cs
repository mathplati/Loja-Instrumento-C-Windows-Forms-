using System.Text;
using Sonora.Core;

namespace Sonora.Desktop;

public sealed class ReportForm : Form
{
    private readonly SaleStore store;
    private readonly DateTimePicker from = new() { Format = DateTimePickerFormat.Short, Width = 135 };
    private readonly DateTimePicker to = new() { Format = DateTimePickerFormat.Short, Width = 135 };
    private readonly ComboBox payment = Ui.Payments(true);
    private readonly Label summary = Ui.Text("", 15, true);
    private readonly DataGridView grid = Ui.Grid();
    private IReadOnlyList<Sale> sales = [];

    public ReportForm(SaleStore store)
    {
        this.store = store; Ui.Window(this, "Relatório de vendas");
        var today = StoreClock.Today; from.Value = new DateTime(today.Year, today.Month, 1); to.Value = today.ToDateTime(TimeOnly.MinValue);
        var header = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 240, Padding = new Padding(28) };
        header.Controls.Add(Ui.Text("Cada venda, uma nova história.", 27, true)); header.SetFlowBreak(header.Controls[0], true);
        header.Controls.Add(Ui.Text("De")); header.Controls.Add(from); header.Controls.Add(Ui.Text("Até")); header.Controls.Add(to);
        header.Controls.Add(payment); header.Controls.Add(Ui.Button("Aplicar filtros", LoadSales));
        header.SetFlowBreak(header.Controls[^1], true); header.Controls.Add(summary); header.SetFlowBreak(summary, true);
        header.Controls.Add(Ui.Button("Exportar CSV", Export)); header.Controls.Add(Ui.Text("Dê dois cliques em uma venda para ver os instrumentos.", 10));
        foreach (var (id, title) in new[] { ("id", "Pedido"), ("date", "Data"), ("customer", "Cliente"), ("payment", "Pagamento"), ("quantity", "Qtd."), ("total", "Total") }) grid.Columns.Add(id, title);
        grid.Columns[0].FillWeight = 70; grid.Columns[4].FillWeight = 35;
        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var sale = sales[e.RowIndex];
            MessageBox.Show($"{sale.Id}\n{sale.Customer}\n\n" + string.Join("\n", sale.Items.Select(x => $"{x.Quantity} × {x.Name} · {Ui.Money(x.Total)}"))
                + $"\n\nSubtotal: {Ui.Money(sale.Subtotal)}\nDesconto: {Ui.Money(sale.Discount)}\nTotal: {Ui.Money(sale.Total)}", "Detalhes da venda", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 0, 28, 28) }; body.Controls.Add(grid);
        Controls.Add(body); Controls.Add(header); Shown += (_, _) => LoadSales();
    }

    private void LoadSales()
    {
        try
        {
            sales = store.List(DateOnly.FromDateTime(from.Value), DateOnly.FromDateTime(to.Value), payment.SelectedIndex == 0 ? null : (PaymentMethod)(payment.SelectedIndex - 1));
            grid.Rows.Clear();
            foreach (var sale in sales) grid.Rows.Add(sale.Id[..12].ToUpperInvariant(), TimeZoneInfo.ConvertTime(sale.CreatedAt, StoreClock.Zone).ToString("dd/MM/yyyy HH:mm"),
                sale.Customer, Reports.PaymentLabel(sale.Payment), sale.Quantity, Ui.Money(sale.Total));
            var data = Reports.Summarize(sales);
            summary.Text = data.Sales == 0 ? "Nenhuma venda no período selecionado." : $"{data.Sales} vendas   ·   {data.Instruments} instrumentos   ·   Receita: {Ui.Money(data.Revenue)}   ·   Ticket médio: {Ui.Money(data.AverageTicket)}";
        }
        catch (Exception error) { Ui.Error(error); }
    }

    private void Export()
    {
        // Recarrega os filtros atuais para que tabela e arquivo correspondam.
        if (from.Value.Date > to.Value.Date) { Ui.Error(new ArgumentException("Confira o intervalo de datas.")); return; }
        LoadSales();
        using var dialog = new SaveFileDialog { Filter = "Planilha CSV (*.csv)|*.csv", FileName = $"sonora-vendas-{StoreClock.Today:yyyy-MM-dd}.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllText(dialog.FileName, Reports.Csv(sales), new UTF8Encoding(true)); MessageBox.Show("Relatório exportado com sucesso."); }
        catch (Exception error) { Ui.Error(error); }
    }
}
