using System.Text;
using Sonora.Core;

namespace Sonora.Desktop;

public sealed class ReportForm : Form
{
    private readonly SaleStore store;
    private readonly DateTimePicker from = new() { Name = "FromDate", Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
    private readonly DateTimePicker to = new() { Name = "ToDate", Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
    private readonly ComboBox payment = Ui.Payments(true);
    private readonly DataGridView grid = Ui.Grid();
    private readonly TableLayoutPanel metrics = new() { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty };
    private readonly Label resultStatus = Ui.Text("Carregando vendas…", 10);
    private readonly Label emptyState = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(24), ForeColor = Ui.Muted, Font = new Font("Segoe UI", 11) };
    private readonly Button exportButton;
    private readonly Button detailsButton;
    private IReadOnlyList<Sale> sales = [];
    private bool filtersDirty;

    public ReportForm(SaleStore store)
    {
        this.store = store;
        Text = "Relatório de vendas";
        BackColor = Ui.Paper;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;

        var today = StoreClock.Today;
        from.Value = new DateTime(today.Year, today.Month, 1);
        to.Value = today.ToDateTime(TimeOnly.MinValue);
        from.AccessibleName = "Data inicial do relatório";
        to.AccessibleName = "Data final do relatório";
        payment.AccessibleName = "Forma de pagamento do relatório";
        payment.Name = "ReportPayment";
        payment.Dock = DockStyle.Fill;
        payment.Margin = Padding.Empty;

        exportButton = Ui.Button("Exportar CSV", Export);
        exportButton.Name = "ExportCsv";
        exportButton.Enabled = false;
        exportButton.Margin = Padding.Empty;
        exportButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        exportButton.TabIndex = 0;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            Padding = new Padding(28, 24, 28, 24), Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var headingText = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
        headingText.Controls.Add(Ui.Text("Relatório de vendas", 25, true));
        var description = Ui.Text("Acompanhe os pedidos e os resultados da sua loja.", 10);
        description.ForeColor = Ui.Muted;
        headingText.Controls.Add(description);
        heading.Controls.Add(headingText, 0, 0);
        heading.Controls.Add(exportButton, 1, 0);
        layout.Controls.Add(heading, 0, 0);

        var filterCard = Ui.Card();
        filterCard.Dock = DockStyle.Fill;
        filterCard.Margin = new Padding(0, 0, 0, 16);
        filterCard.Padding = new Padding(16, 12, 16, 12);
        var filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Margin = Padding.Empty };
        foreach (var width in new[] { 23f, 23f, 32f, 22f }) filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, width));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var apply = Ui.Button("Aplicar filtros", () => RefreshData(), true);
        apply.Name = "ApplyFilters";
        apply.Dock = DockStyle.Fill;
        apply.Margin = Padding.Empty;
        apply.TabIndex = 3;
        from.TabIndex = 0;
        to.TabIndex = 1;
        payment.TabIndex = 2;
        AddFilter(filters, "Data inicial", from, 0);
        AddFilter(filters, "Data final", to, 1);
        AddFilter(filters, "Pagamento", payment, 2);
        filters.Controls.Add(apply, 3, 1);
        filterCard.Controls.Add(filters);
        layout.Controls.Add(filterCard, 0, 1);

        for (var i = 0; i < 4; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        SetMetrics(null);
        layout.Controls.Add(metrics, 0, 2);

        var historyCard = Ui.Card();
        historyCard.Dock = DockStyle.Fill;
        historyCard.Margin = Padding.Empty;
        var history = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
        history.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        history.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        history.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        history.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        var listHeading = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(18, 12, 18, 0), Margin = Padding.Empty };
        var title = Ui.Text("Histórico de vendas", 13, true);
        title.Margin = new Padding(0, 0, 0, 4);
        listHeading.Controls.Add(title);
        resultStatus.ForeColor = Ui.Muted;
        resultStatus.Margin = Padding.Empty;
        listHeading.Controls.Add(resultStatus);
        history.Controls.Add(listHeading, 0, 0);

        ConfigureGrid();
        var gridContainer = new Panel { Dock = DockStyle.Fill, Margin = new Padding(1, 0, 1, 0), BackColor = Color.White };
        gridContainer.Controls.Add(grid);
        gridContainer.Controls.Add(emptyState);
        history.Controls.Add(gridContainer, 0, 1);

        detailsButton = Ui.Button("Ver detalhes", OpenSelectedSale);
        detailsButton.Name = "SaleDetails";
        detailsButton.Enabled = false;
        detailsButton.Margin = Padding.Empty;
        detailsButton.TabIndex = 0;
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(14, 10, 14, 8), Margin = Padding.Empty };
        footer.Controls.Add(detailsButton);
        history.Controls.Add(footer, 0, 2);
        historyCard.Controls.Add(history);
        layout.Controls.Add(historyCard, 0, 3);
        Controls.Add(layout);

        AcceptButton = apply;
        from.ValueChanged += (_, _) => FiltersChanged();
        to.ValueChanged += (_, _) => FiltersChanged();
        payment.SelectedIndexChanged += (_, _) => FiltersChanged();
        grid.SelectionChanged += (_, _) => detailsButton.Enabled = !filtersDirty && grid.CurrentRow?.Tag is Sale;
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenSelectedSale(); };
        grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            OpenSelectedSale();
        };
    }

    private static void AddFilter(TableLayoutPanel filters, string title, Control control, int column)
    {
        var label = Ui.Text(title, 9, true);
        label.Margin = new Padding(0, 0, 12, 4);
        control.Margin = new Padding(0, 0, 12, 0);
        filters.Controls.Add(label, column, 0);
        filters.Controls.Add(control, column, 1);
    }

    private void ConfigureGrid()
    {
        grid.AccessibleName = "Vendas no período selecionado";
        grid.Name = "SalesGrid";
        grid.AccessibleDescription = "Selecione uma venda e use Ver detalhes ou pressione Enter.";
        grid.TabIndex = 0;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(246, 247, 244);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Ui.Muted;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8);
        grid.GridColor = Ui.Line;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
        grid.RowTemplate.Height = 46;
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "id", HeaderText = "PEDIDO", FillWeight = 90, MinimumWidth = 95 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "date", HeaderText = "DATA", FillWeight = 115, MinimumWidth = 135, ValueType = typeof(DateTime), DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "customer", HeaderText = "CLIENTE", FillWeight = 150, MinimumWidth = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "payment", HeaderText = "PAGAMENTO", FillWeight = 110, MinimumWidth = 100 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "quantity", HeaderText = "ITENS", FillWeight = 45, MinimumWidth = 58, ValueType = typeof(int), DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "total", HeaderText = "TOTAL", FillWeight = 90, MinimumWidth = 100, ValueType = typeof(decimal), DefaultCellStyle = new DataGridViewCellStyle { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 10, FontStyle.Bold) } });
    }

    private void FiltersChanged()
    {
        filtersDirty = true;
        exportButton.Enabled = false;
        detailsButton.Enabled = false;
        resultStatus.Text = "Filtros alterados. Aplique para atualizar os resultados.";
        resultStatus.ForeColor = Ui.Orange;
    }

    public void RefreshData() => LoadSales();

    private bool LoadSales()
    {
        try
        {
            if (from.Value.Date > to.Value.Date)
                throw new ArgumentException("A data inicial deve ser anterior ou igual à data final.");
            if (payment.SelectedIndex < 0)
                throw new ArgumentException("Selecione uma forma de pagamento.");

            var loaded = store.List(DateOnly.FromDateTime(from.Value), DateOnly.FromDateTime(to.Value), payment.SelectedIndex == 0 ? null : (PaymentMethod)(payment.SelectedIndex - 1));
            var summary = Reports.Summarize(loaded);
            grid.Rows.Clear();
            sales = loaded;
            foreach (var sale in sales)
            {
                var index = grid.Rows.Add(sale.Id[..Math.Min(sale.Id.Length, 12)].ToUpperInvariant(),
                    TimeZoneInfo.ConvertTime(sale.CreatedAt, StoreClock.Zone).DateTime,
                    sale.Customer, Reports.PaymentLabel(sale.Payment), sale.Quantity, sale.Total);
                grid.Rows[index].Tag = sale;
                grid.Rows[index].Cells[0].ToolTipText = sale.Id;
            }
            filtersDirty = false;
            SetMetrics(summary);
            resultStatus.Text = $"{sales.Count} {(sales.Count == 1 ? "venda encontrada" : "vendas encontradas")} · {from.Value:dd/MM/yyyy} a {to.Value:dd/MM/yyyy}";
            resultStatus.ForeColor = Ui.Muted;
            emptyState.Text = "Nenhuma venda encontrada\n\nAltere o período ou o pagamento para buscar outros pedidos.";
            emptyState.Visible = sales.Count == 0;
            if (emptyState.Visible) emptyState.BringToFront();
            exportButton.Enabled = sales.Count > 0;
            detailsButton.Enabled = grid.CurrentRow?.Tag is Sale;
            return true;
        }
        catch (Exception error)
        {
            sales = [];
            grid.Rows.Clear();
            filtersDirty = true;
            SetMetrics(null);
            exportButton.Enabled = false;
            detailsButton.Enabled = false;
            resultStatus.Text = "Não foi possível carregar os resultados. Confira os filtros e tente novamente.";
            resultStatus.ForeColor = Ui.Orange;
            emptyState.Text = "O relatório não foi carregado\n\nNenhum resultado está sendo exibido. Ajuste os filtros e clique em Aplicar filtros.";
            emptyState.Visible = true;
            emptyState.BringToFront();
            Ui.Error(error);
            return false;
        }
    }

    private void SetMetrics(SalesSummary? data)
    {
        foreach (Control control in metrics.Controls.Cast<Control>().ToArray()) control.Dispose();
        metrics.Controls.Clear();
        var cards = new[]
        {
            Ui.Metric("Vendas", data?.Sales.ToString() ?? "—", "Pedidos no período"),
            Ui.Metric("Instrumentos", data?.Instruments.ToString() ?? "—", "Unidades vendidas"),
            Ui.Metric("Receita total", data is null ? "—" : Ui.Money(data.Revenue), "Após os descontos"),
            Ui.Metric("Ticket médio", data is null ? "—" : Ui.Money(data.AverageTicket), "Valor médio por pedido")
        };
        for (var i = 0; i < cards.Length; i++)
        {
            cards[i].Dock = DockStyle.Fill;
            cards[i].Margin = new Padding(0, 0, i == cards.Length - 1 ? 0 : 12, 16);
            metrics.Controls.Add(cards[i], i, 0);
        }
    }

    private void OpenSelectedSale()
    {
        if (filtersDirty || grid.CurrentRow?.Tag is not Sale sale) return;
        using var dialog = new SaleDetailsDialog(sale);
        dialog.ShowDialog(this);
    }

    private void Export()
    {
        // Consulta novamente: arquivo e tabela sempre correspondem aos filtros visíveis.
        if (!LoadSales() || sales.Count == 0) return;
        using var dialog = new SaveFileDialog
        {
            Title = "Exportar relatório de vendas", Filter = "Planilha CSV (*.csv)|*.csv",
            FileName = $"sonora-vendas-{from.Value:yyyy-MM-dd}-a-{to.Value:yyyy-MM-dd}.csv",
            DefaultExt = "csv", AddExtension = true, OverwritePrompt = true, CheckPathExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            File.WriteAllText(dialog.FileName, Reports.Csv(sales), new UTF8Encoding(true));
            MessageBox.Show(this, $"{sales.Count} {(sales.Count == 1 ? "venda exportada" : "vendas exportadas")} com sucesso.", "Relatório exportado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception error) { Ui.Error(error); }
    }
}

internal sealed class SaleDetailsDialog : Form
{
    internal SaleDetailsDialog(Sale sale)
    {
        Text = "Sonora · Detalhes da venda";
        Size = new Size(860, 650);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Ui.Paper;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimizeBox = false;
        ShowInTaskbar = false;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(24), Margin = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 114));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        var heading = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
        heading.Controls.Add(Ui.Text("Detalhes da venda", 22, true));
        var identifier = Ui.Text($"Pedido {sale.Id}", 9);
        identifier.ForeColor = Ui.Muted;
        identifier.Margin = new Padding(0, 0, 0, 8);
        heading.Controls.Add(identifier);
        var date = TimeZoneInfo.ConvertTime(sale.CreatedAt, StoreClock.Zone);
        var customer = Ui.Text($"Cliente: {sale.Customer}", 11, true);
        customer.Margin = new Padding(0, 0, 0, 4);
        heading.Controls.Add(customer);
        heading.Controls.Add(Ui.Text($"{date:dd/MM/yyyy 'às' HH:mm} · {Reports.PaymentLabel(sale.Payment)} · {sale.Quantity} {(sale.Quantity == 1 ? "instrumento" : "instrumentos")}", 10));
        layout.Controls.Add(heading, 0, 0);

        var items = Ui.Grid();
        items.AccessibleName = "Instrumentos da venda";
        items.TabIndex = 0;
        items.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(239, 241, 236);
        items.ColumnHeadersDefaultCellStyle.ForeColor = Ui.Ink;
        items.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Instrumento", FillWeight = 160 });
        items.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Marca", FillWeight = 90 });
        items.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Qtd.", FillWeight = 45, ValueType = typeof(int) });
        items.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Valor unitário", FillWeight = 95, ValueType = typeof(decimal), DefaultCellStyle = new DataGridViewCellStyle { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        items.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Subtotal", FillWeight = 100, ValueType = typeof(decimal), DefaultCellStyle = new DataGridViewCellStyle { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        foreach (var item in sale.Items) items.Rows.Add(item.Name, item.Brand, item.Quantity, item.UnitPrice, item.Total);
        layout.Controls.Add(items, 0, 1);

        var totals = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(12, 12, 12, 6), BackColor = Color.White, Margin = new Padding(0, 12, 0, 8) };
        totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        totals.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddTotal(totals, "Subtotal", Ui.Money(sale.Subtotal), 0);
        AddTotal(totals, $"Desconto ({sale.DiscountPercent:0.##}%)", $"− {Ui.Money(sale.Discount)}", 1);
        AddTotal(totals, "Total da venda", Ui.Money(sale.Total), 2, true);
        layout.Controls.Add(totals, 0, 2);

        var close = Ui.Button("Fechar", () => Close(), true);
        close.DialogResult = DialogResult.OK;
        close.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        close.Margin = Padding.Empty;
        close.TabIndex = 1;
        layout.Controls.Add(close, 0, 3);
        AcceptButton = close;
        CancelButton = close;
        Controls.Add(layout);
    }

    private static void AddTotal(TableLayoutPanel totals, string title, string amount, int row, bool highlight = false)
    {
        totals.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        var label = Ui.Text(title, highlight ? 12 : 10, highlight);
        var value = Ui.Text(amount, highlight ? 12 : 10, highlight);
        label.Margin = Padding.Empty;
        value.Margin = Padding.Empty;
        value.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        if (highlight) value.ForeColor = Ui.Orange;
        totals.Controls.Add(label, 0, row);
        totals.Controls.Add(value, 1, row);
    }
}
