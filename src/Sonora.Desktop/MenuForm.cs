using Sonora.Core;

namespace Sonora.Desktop;

public sealed class MenuForm : Form
{
    private readonly SaleStore store;
    private readonly Panel host = new() { Dock = DockStyle.Fill, BackColor = Ui.Paper };
    private readonly Label breadcrumb = Ui.Text("Workspace  /  Visão geral", 10);
    private readonly Dictionary<string, Button> navigation = [];
    private readonly SaleForm salePage;
    private readonly ReportForm reportPage;
    private readonly Panel dashboard = new() { Name = "Dashboard", Dock = DockStyle.Fill, AutoScroll = true, BackColor = Ui.Paper };

    public MenuForm(SaleStore store)
    {
        this.store = store;
        Ui.Window(this, "Visão geral");
        Name = "Menu";
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 218)); shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.Controls.Add(BuildSidebar(), 0, 0);
        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
        var topbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(24, 20, 24, 0), Margin = Padding.Empty };
        breadcrumb.ForeColor = Ui.Muted; breadcrumb.Dock = DockStyle.Left;
        var date = Ui.Text($"{StoreClock.Today:dd/MM/yyyy}   ·   Equipe Sonora", 9); date.ForeColor = Ui.Muted; date.Dock = DockStyle.Right;
        topbar.Controls.Add(breadcrumb); topbar.Controls.Add(date);
        var footer = Ui.Text("Feito para quem vive de música.                                                   SONORA · GESTÃO DE INSTRUMENTOS", 8);
        footer.ForeColor = Ui.Muted; footer.Dock = DockStyle.Fill; footer.Padding = new Padding(24, 8, 0, 0); footer.Margin = Padding.Empty;
        workspace.Controls.Add(topbar, 0, 0); workspace.Controls.Add(host, 0, 1); workspace.Controls.Add(footer, 0, 2);
        shell.Controls.Add(workspace, 1, 0); Controls.Add(shell);

        salePage = new SaleForm(store);
        reportPage = new ReportForm(store);
        salePage.SaleCompleted += (_, _) => RefreshDashboard();
        foreach (var page in new Form[] { salePage, reportPage })
        {
            page.TopLevel = false; page.FormBorderStyle = FormBorderStyle.None; page.Dock = DockStyle.Fill; page.Margin = Padding.Empty;
            host.Controls.Add(page);
        }
        host.Controls.Add(dashboard);
        Shown += (_, _) => Navigate("home");
        FormClosing += ConfirmClose;
    }

    private Control BuildSidebar()
    {
        var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, BackColor = Color.White, Padding = new Padding(18, 24, 18, 14), Margin = Padding.Empty, AccessibleName = "Navegação da loja" };
        foreach (var height in new[] { 66, 89, 28, 150 }) sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 125)); sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 49));
        var brand = Ui.Text("sonora.", 34, true); brand.Margin = new Padding(5, 0, 0, 0); sidebar.Controls.Add(brand, 0, 0);
        var shop = Ui.Card(); shop.BackColor = Ui.Paper; shop.Dock = DockStyle.Fill; shop.Padding = new Padding(12); shop.Margin = new Padding(0, 0, 0, 19);
        var title = Ui.Text("Loja de instrumentos", 10, true); title.Dock = DockStyle.Top;
        var subtitle = Ui.Text("Seu espaço de música", 8); subtitle.ForeColor = Ui.Muted; subtitle.Dock = DockStyle.Bottom;
        shop.Controls.Add(title); shop.Controls.Add(subtitle); sidebar.Controls.Add(shop, 0, 1);
        var caption = Ui.Text("PRINCIPAL", 8, true); caption.ForeColor = Ui.Muted; sidebar.Controls.Add(caption, 0, 2);
        var links = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
        var index = 0;
        foreach (var (key, label) in new[] { ("home", "Visão geral"), ("sale", "+  Nova venda"), ("reports", "Relatório de vendas") })
        {
            var target = key;
            var button = Ui.Button(label, () => Navigate(target)); button.Dock = DockStyle.Fill; button.TextAlign = ContentAlignment.MiddleLeft;
            button.Name = "Nav_" + key;
            button.Font = new Font("Segoe UI", 10, FontStyle.Bold); button.Margin = new Padding(0, 0, 0, 7); button.TabIndex = index;
            navigation.Add(key, button); links.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f)); links.Controls.Add(button, 0, index++);
        }
        sidebar.Controls.Add(links, 0, 3);
        var note = Ui.Card(); note.Dock = DockStyle.Fill; note.BackColor = Color.FromArgb(239, 242, 230); note.Padding = new Padding(14); note.Margin = new Padding(0, 0, 0, 16);
        var message = Ui.Text("Grandes histórias.\nNovos acordes.", 15); message.Dock = DockStyle.Top;
        var footnote = Ui.Text("O próximo som começa aqui.", 8); footnote.ForeColor = Ui.Muted; footnote.Dock = DockStyle.Bottom;
        note.Controls.Add(message); note.Controls.Add(footnote); sidebar.Controls.Add(note, 0, 5);
        var status = Ui.Text("● Dados salvos localmente\nSONORA · VERSÃO 1.0", 8); status.ForeColor = Ui.Muted; sidebar.Controls.Add(status, 0, 6);
        return sidebar;
    }

    private void Navigate(string target)
    {
        foreach (var (key, button) in navigation)
        {
            button.BackColor = key == target ? Color.FromArgb(249, 233, 225) : Color.White;
            button.ForeColor = key == target ? Ui.Orange : Ui.Muted;
            button.FlatAppearance.BorderColor = key == target ? Color.FromArgb(249, 233, 225) : Color.White;
        }
        var name = target switch { "sale" => "Nova venda", "reports" => "Relatório de vendas", _ => "Visão geral" };
        Text = $"Sonora · {name}"; breadcrumb.Text = $"Workspace  /  {name}";
        foreach (Control control in host.Controls) control.Hide();
        switch (target)
        {
            case "sale": salePage.Show(); salePage.BringToFront(); break;
            case "reports": reportPage.Show(); reportPage.RefreshData(); reportPage.BringToFront(); break;
            default: RefreshDashboard(); dashboard.Show(); dashboard.BringToFront(); break;
        }
    }

    private void RefreshDashboard()
    {
        dashboard.SuspendLayout(); Ui.ClearControls(dashboard);
        var content = new TableLayoutPanel { Dock = DockStyle.Top, Height = 744, ColumnCount = 1, RowCount = 5, Padding = new Padding(24), Margin = Padding.Empty };
        foreach (var height in new[] { 73, 207, 43, 126, 247 }) content.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        var heading = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        var title = Ui.Text("Bom som. Bons negócios.", 25, true); title.Location = new Point(0, 0);
        var subtitle = Ui.Text("Bem-vindo à Sonora. Vamos fazer a música acontecer?", 10); subtitle.ForeColor = Ui.Muted; subtitle.Location = new Point(2, 45);
        heading.Controls.AddRange([title, subtitle]); content.Controls.Add(heading, 0, 0);
        content.Controls.Add(new HeroBanner(() => Navigate("sale")) { Dock = DockStyle.Fill, Margin = Padding.Empty }, 0, 1);
        var label = Ui.Text("Hoje na Sonora", 12, true); label.Padding = new Padding(0, 14, 0, 0); content.Controls.Add(label, 0, 2);
        try
        {
            var sales = store.List(); var today = sales.Where(s => StoreClock.Date(s.CreatedAt) == StoreClock.Today).ToArray(); var summary = Reports.Summarize(today);
            var metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty };
            var cards = new[] { Ui.Metric("Receita de vendas", Ui.Money(summary.Revenue), "No dia de hoje"), Ui.Metric("Vendas realizadas", summary.Sales.ToString("D2"), "No dia de hoje"), Ui.Metric("Instrumentos vendidos", summary.Instruments.ToString("D2"), "No dia de hoje"), Ui.Metric("Ticket médio", Ui.Money(summary.AverageTicket), "Valor médio por pedido") };
            for (var i = 0; i < cards.Length; i++) { metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25)); cards[i].Dock = DockStyle.Fill; cards[i].Margin = new Padding(0, 0, i == 3 ? 0 : 12, 14); metrics.Controls.Add(cards[i], i, 0); }
            content.Controls.Add(metrics, 0, 3);
            content.Controls.Add(BuildOverview(sales), 0, 4);
        }
        catch (Exception error)
        {
            var message = Ui.Text($"Não foi possível carregar os indicadores: {error.Message}", 10); message.ForeColor = Color.Firebrick;
            message.MaximumSize = new Size(720, 0); content.Controls.Add(message, 0, 3);
        }
        dashboard.Controls.Add(content); dashboard.ResumeLayout(true);
    }

    private Control BuildOverview(IReadOnlyList<Sale> sales)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        var recent = Ui.Card(); recent.Dock = DockStyle.Fill; recent.Padding = new Padding(14); recent.Margin = new Padding(0, 0, 14, 0);
        var header = new Panel { Dock = DockStyle.Top, Height = 39 };
        var title = Ui.Text("Últimas vendas", 12, true); title.Dock = DockStyle.Left;
        var open = Ui.Button("Ver relatório  →", () => Navigate("reports")); open.Dock = DockStyle.Right; open.MinimumSize = new Size(110, 30); open.Font = new Font("Segoe UI", 8); open.Height = 30;
        header.Controls.Add(title); header.Controls.Add(open);
        if (sales.Count == 0)
        {
            var empty = Ui.Text("Sua primeira venda merece um bis.\n\nComece em Nova venda para acompanhar os resultados.", 11); empty.AutoSize = false; empty.Dock = DockStyle.Fill; empty.TextAlign = ContentAlignment.MiddleCenter; empty.ForeColor = Ui.Muted;
            recent.Controls.Add(empty);
        }
        else
        {
            var grid = Ui.Grid(); grid.Columns.Add("customer", "Cliente"); grid.Columns.Add("payment", "Pagamento"); grid.Columns.Add("total", "Total");
            foreach (var sale in sales.Take(4)) grid.Rows.Add(sale.Customer, Reports.PaymentLabel(sale.Payment), Ui.Money(sale.Total));
            recent.Controls.Add(grid);
        }
        recent.Controls.Add(header); layout.Controls.Add(recent, 0, 0);
        var featured = Ui.Card(); featured.Dock = DockStyle.Fill; featured.Padding = new Padding(14); featured.Margin = Padding.Empty;
        var catalog = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        var caption = Ui.Text("No nosso catálogo", 12, true); catalog.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); catalog.Controls.Add(caption, 0, 0);
        var row = 1;
        foreach (var instrument in Catalog.All.Take(3))
        {
            var product = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
            product.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52)); product.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); product.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            product.Controls.Add(new InstrumentArt(instrument) { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 6, 2) }, 0, 0);
            var name = Ui.Text(instrument.Name + "\n" + instrument.Brand, 8); name.AutoSize = false; name.Dock = DockStyle.Fill; name.TextAlign = ContentAlignment.MiddleLeft; name.AutoEllipsis = true; name.Margin = Padding.Empty;
            var price = Ui.Text(Ui.Money(instrument.Price), 9, true); price.AutoSize = false; price.Dock = DockStyle.Fill; price.TextAlign = ContentAlignment.MiddleRight; price.Margin = Padding.Empty;
            product.Controls.Add(name, 1, 0); product.Controls.Add(price, 2, 0); catalog.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f)); catalog.Controls.Add(product, 0, row++);
        }
        featured.Controls.Add(catalog); layout.Controls.Add(featured, 1, 0); return layout;
    }

    private void ConfirmClose(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing || !salePage.HasPendingOrder) return;
        e.Cancel = MessageBox.Show(this, "Há um pedido em andamento. Sair e descartar os itens?", "Pedido não finalizado", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes;
    }
}
