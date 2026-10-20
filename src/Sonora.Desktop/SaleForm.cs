using Sonora.Core;

namespace Sonora.Desktop;

public sealed class SaleForm : Form
{
    private readonly SaleStore store;
    private readonly Dictionary<string, int> cart = [];
    private readonly ToolTip tips = new();
    private readonly Panel page = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(28, 24, 28, 24) };
    private readonly TableLayoutPanel document = new() { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
    private readonly TableLayoutPanel workspace = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
    private readonly TableLayoutPanel catalogArea = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0, 0, 22, 0) };
    private readonly TableLayoutPanel filters = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
    private readonly FlowLayoutPanel catalog = new() { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly TextBox search = new() { Name = "CatalogSearch", Dock = DockStyle.Fill, AutoSize = false, PlaceholderText = "Buscar instrumento ou marca…", AccessibleName = "Buscar instrumento ou marca", Margin = new Padding(0, 0, 10, 0), TabIndex = 0 };
    private readonly ComboBox category = new() { Name = "CatalogCategory", Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Filtrar por categoria", Margin = Padding.Empty, TabIndex = 1 };
    private readonly Label catalogCount = Ui.Text("", 9);
    private readonly Panel order = Ui.Card();
    private readonly FlowLayoutPanel orderLines = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = Padding.Empty, Margin = new Padding(0, 12, 0, 12), TabIndex = 2 };
    private readonly TextBox customer = new() { Name = "CustomerName", Dock = DockStyle.Fill, AutoSize = false, MaxLength = 80, PlaceholderText = "Nome do cliente", AccessibleName = "Nome do cliente, obrigatório", Margin = Padding.Empty, TabIndex = 0 };
    private readonly ComboBox payment = Ui.Payments();
    private readonly NumericUpDown discount = new() { Name = "DiscountPercent", Maximum = 30, DecimalPlaces = 2, Increment = 1, TextAlign = HorizontalAlignment.Right, Dock = DockStyle.Fill, AccessibleName = "Desconto percentual de zero a trinta", Margin = new Padding(0, 5, 0, 5), TabIndex = 0 };
    private readonly Label quantity = Ui.Text("", 9);
    private readonly Label subtotalValue = Ui.Text("", 10);
    private readonly Label discountValue = Ui.Text("", 10);
    private readonly Label totalValue = Ui.Text("", 19, true);
    private readonly Button finish;
    private readonly LinkLabel clear;
    private bool arranging;

    public bool HasPendingOrder => cart.Count > 0;
    public event EventHandler? SaleCompleted;

    public SaleForm(SaleStore store)
    {
        this.store = store;
        Text = "Nova venda";
        Font = new Font("Segoe UI", 10);
        BackColor = Ui.Paper;
        AutoScaleMode = AutoScaleMode.Dpi;

        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var title = Ui.Text("Encontre o próximo som.", 24, true);
        title.AutoSize = false;
        title.Dock = DockStyle.Fill;
        title.AutoEllipsis = true;
        title.Margin = Padding.Empty;
        var subtitle = FieldLabel("Escolha os instrumentos. A próxima história é por conta do cliente.");
        subtitle.ForeColor = Ui.Muted;
        subtitle.AutoEllipsis = true;
        subtitle.TextAlign = ContentAlignment.TopLeft;
        heading.Controls.Add(title, 0, 0);
        heading.Controls.Add(subtitle, 0, 1);

        category.Items.Add("Todas as categorias");
        foreach (var name in Catalog.All.Select(item => item.Category).Distinct()) category.Items.Add(name);
        category.SelectedIndex = 0;
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 178));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        filters.Controls.Add(search, 0, 0);
        filters.Controls.Add(category, 1, 0);
        catalogCount.Dock = DockStyle.Fill;
        catalogCount.AutoSize = false;
        catalogCount.ForeColor = Ui.Muted;
        catalogCount.TextAlign = ContentAlignment.MiddleLeft;
        catalogCount.Margin = Padding.Empty;
        catalogArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        catalogArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        catalogArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        catalogArea.Controls.Add(filters, 0, 0);
        catalogArea.Controls.Add(catalogCount, 0, 1);
        catalogArea.Controls.Add(catalog, 0, 2);
        catalogArea.TabIndex = 0;
        catalog.TabIndex = 2;

        order.Dock = DockStyle.Fill;
        order.Margin = Padding.Empty;
        order.Padding = new Padding(20);
        order.TabIndex = 1;
        var orderLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Margin = Padding.Empty, Padding = Padding.Empty };
        orderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        orderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        orderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
        orderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        orderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 245));
        var orderHeading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        orderHeading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        orderHeading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        var orderTitle = FieldLabel("Seu pedido");
        orderTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        quantity.Dock = DockStyle.Fill;
        quantity.AutoSize = false;
        quantity.ForeColor = Ui.Muted;
        quantity.TextAlign = ContentAlignment.MiddleRight;
        quantity.Margin = Padding.Empty;
        orderHeading.Controls.Add(orderTitle, 0, 0);
        orderHeading.Controls.Add(quantity, 1, 0);

        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Margin = Padding.Empty, TabIndex = 1 };
        foreach (var height in new[] { 24, 34, 12, 24, 34 }) fields.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        fields.Controls.Add(FieldLabel("Cliente *"), 0, 0);
        fields.Controls.Add(customer, 0, 1);
        fields.Controls.Add(FieldLabel("Forma de pagamento"), 0, 3);
        payment.Dock = DockStyle.Fill;
        payment.Name = "PaymentMethod";
        payment.Margin = Padding.Empty;
        payment.AccessibleName = "Forma de pagamento";
        payment.TabIndex = 1;
        fields.Controls.Add(payment, 0, 4);

        var summary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9, Margin = Padding.Empty, TabIndex = 3 };
        foreach (var height in new[] { 1, 42, 25, 25, 1, 48, 44, 32, 27 }) summary.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        summary.Controls.Add(Separator(), 0, 0);
        var discountRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 5, 0, 0), TabIndex = 0 };
        discountRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        discountRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
        var discountLabel = FieldLabel("Desconto no pedido (%)");
        discountLabel.ForeColor = Ui.Muted;
        discountLabel.Font = new Font("Segoe UI", 9);
        discountRow.Controls.Add(discountLabel, 0, 0);
        discountRow.Controls.Add(discount, 1, 0);
        summary.Controls.Add(discountRow, 0, 1);
        summary.Controls.Add(AmountRow("Subtotal", subtotalValue), 0, 2);
        summary.Controls.Add(AmountRow("Desconto", discountValue), 0, 3);
        summary.Controls.Add(Separator(), 0, 4);
        summary.Controls.Add(AmountRow("Total", totalValue, true), 0, 5);
        finish = Ui.Button("✓  Finalizar venda", CompleteSale, true);
        finish.Name = "CompleteSale";
        finish.AutoSize = false;
        finish.MinimumSize = Size.Empty;
        finish.Dock = DockStyle.Fill;
        finish.Margin = Padding.Empty;
        finish.TabIndex = 1;
        tips.SetToolTip(finish, "Salvar a venda · Ctrl+Enter");
        summary.Controls.Add(finish, 0, 6);
        var hint = FieldLabel("Pedido salvo com segurança neste dispositivo.");
        hint.ForeColor = Ui.Muted;
        hint.Font = new Font("Segoe UI", 8);
        hint.TextAlign = ContentAlignment.MiddleCenter;
        summary.Controls.Add(hint, 0, 7);
        clear = new LinkLabel { Name = "ClearOrder", Text = "Limpar pedido", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, LinkColor = Ui.Muted, ActiveLinkColor = Ui.Orange, VisitedLinkColor = Ui.Muted, Margin = Padding.Empty, TabIndex = 2 };
        clear.Click += (_, _) => ClearOrder();
        summary.Controls.Add(clear, 0, 8);

        orderLayout.Controls.Add(orderHeading, 0, 0);
        orderLayout.Controls.Add(fields, 0, 1);
        orderLayout.Controls.Add(Separator(), 0, 2);
        orderLayout.Controls.Add(orderLines, 0, 3);
        orderLayout.Controls.Add(summary, 0, 4);
        order.Controls.Add(orderLayout);
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspace.Controls.Add(catalogArea, 0, 0);
        workspace.Controls.Add(order, 1, 0);
        document.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        document.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        document.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        document.Controls.Add(heading, 0, 0);
        document.Controls.Add(workspace, 0, 1);
        page.Controls.Add(document);
        Controls.Add(page);
        subtotalValue.Name = "OrderSubtotal";
        discountValue.Name = "OrderDiscount";
        totalValue.Name = "OrderTotal";

        search.TextChanged += (_, _) => RenderCatalog();
        category.SelectedIndexChanged += (_, _) => RenderCatalog();
        discount.ValueChanged += (_, _) => UpdateAmounts();
        page.ClientSizeChanged += (_, _) => ArrangePage();
        catalog.ClientSizeChanged += (_, _) => ArrangeCatalogCards();
        orderLines.ClientSizeChanged += (_, _) => ArrangeOrderLines();
        RenderCatalog();
        RenderCart();
        ArrangePage();
    }

    private static Label FieldLabel(string text)
    {
        var label = Ui.Text(text, 10);
        label.Dock = DockStyle.Fill;
        label.AutoSize = false;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Margin = Padding.Empty;
        return label;
    }

    private static Panel Separator() => new() { Dock = DockStyle.Fill, BackColor = Ui.Line, Margin = Padding.Empty };

    private static TableLayoutPanel AmountRow(string title, Label value, bool prominent = false)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, prominent ? 25 : 43));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, prominent ? 75 : 57));
        var label = FieldLabel(title);
        label.ForeColor = prominent ? Ui.Ink : Ui.Muted;
        if (prominent) label.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        value.AutoSize = false;
        value.Dock = DockStyle.Fill;
        value.Margin = Padding.Empty;
        value.TextAlign = ContentAlignment.MiddleRight;
        value.ForeColor = prominent ? Ui.Ink : Ui.Muted;
        value.AutoEllipsis = true;
        row.Controls.Add(label, 0, 0);
        row.Controls.Add(value, 1, 0);
        return row;
    }

    private void ArrangePage()
    {
        if (arranging) return;
        arranging = true;
        try
        {
            var stacked = page.ClientSize.Width - page.Padding.Horizontal < 760;
            workspace.SuspendLayout();
            workspace.Controls.Remove(order);
            workspace.ColumnStyles.Clear();
            workspace.RowStyles.Clear();
            workspace.ColumnCount = stacked ? 1 : 2;
            workspace.RowCount = stacked ? 2 : 1;
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            if (stacked)
            {
                workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 670));
                workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 720));
                workspace.Controls.Add(order, 0, 1);
                catalogArea.Margin = new Padding(0, 0, 0, 22);
            }
            else
            {
                workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
                workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                workspace.Controls.Add(order, 1, 0);
                catalogArea.Margin = new Padding(0, 0, 22, 0);
            }
            document.Height = stacked ? 1478 : Math.Max(708, page.ClientSize.Height - page.Padding.Vertical);
            workspace.ResumeLayout(true);
            ArrangeFilters();
            ArrangeCatalogCards();
            ArrangeOrderLines();
        }
        finally { arranging = false; }
    }

    private void ArrangeFilters()
    {
        var narrow = catalogArea.ClientSize.Width < 440;
        if (narrow == (filters.RowCount == 2)) return;
        filters.SuspendLayout();
        filters.Controls.Remove(category);
        filters.ColumnStyles.Clear();
        filters.RowStyles.Clear();
        filters.ColumnCount = narrow ? 1 : 2;
        filters.RowCount = narrow ? 2 : 1;
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        if (narrow)
        {
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            filters.Controls.Add(category, 0, 1);
            search.Margin = new Padding(0, 0, 0, 6);
            catalogArea.RowStyles[0].Height = 76;
        }
        else
        {
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 178));
            filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            filters.Controls.Add(category, 1, 0);
            search.Margin = new Padding(0, 0, 10, 0);
            catalogArea.RowStyles[0].Height = 38;
        }
        filters.ResumeLayout(true);
    }

    private void RenderCatalog()
    {
        catalog.SuspendLayout();
        DisposeChildren(catalog);
        var found = Catalog.All.Where(item => (category.SelectedIndex == 0 || item.Category == category.Text)
            && $"{item.Name} {item.Brand}".Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();
        catalogCount.Text = $"{found.Length} {(found.Length == 1 ? "instrumento disponível" : "instrumentos disponíveis")} · Curadoria Sonora";
        foreach (var instrument in found)
        {
            var card = Ui.Card();
            card.Padding = new Padding(8);
            card.Height = 276;
            card.TabIndex = catalog.Controls.Count;
            card.Tag = instrument;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Margin = Padding.Empty };
            foreach (var height in new[] { 144, 30, 24, 44 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            var art = new InstrumentArt(instrument) { Dock = DockStyle.Fill, Margin = Padding.Empty, TabStop = false };
            var categoryLabel = Ui.Text(instrument.Category.ToUpperInvariant(), 7);
            categoryLabel.ForeColor = Ui.Muted;
            categoryLabel.Location = new Point(10, 10);
            categoryLabel.BackColor = Color.Transparent;
            art.Controls.Add(categoryLabel);
            var name = FieldLabel(instrument.Name);
            name.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            name.AutoEllipsis = true;
            name.Margin = new Padding(6, 4, 6, 0);
            tips.SetToolTip(name, instrument.Name);
            var brand = FieldLabel(instrument.Brand);
            brand.Font = new Font("Segoe UI", 9);
            brand.ForeColor = Ui.Muted;
            brand.Margin = new Padding(6, 0, 6, 0);
            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(6, 2, 6, 0) };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            var price = FieldLabel(Ui.Money(instrument.Price));
            price.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            var add = SmallButton("+", () => AddInstrument(instrument), $"Adicionar {instrument.Name} ao pedido");
            add.Name = "Add_" + instrument.Id;
            add.BackColor = Color.FromArgb(250, 235, 227);
            add.ForeColor = Ui.Orange;
            add.FlatAppearance.BorderSize = 0;
            add.Margin = new Padding(0, 3, 0, 3);
            bottom.Controls.Add(price, 0, 0);
            bottom.Controls.Add(add, 1, 0);
            layout.Controls.Add(art, 0, 0);
            layout.Controls.Add(name, 0, 1);
            layout.Controls.Add(brand, 0, 2);
            layout.Controls.Add(bottom, 0, 3);
            card.Controls.Add(layout);
            catalog.Controls.Add(card);
        }
        if (found.Length == 0)
        {
            var empty = Ui.Text("Nenhum instrumento encontrado.\nTente outro nome, marca ou categoria.", 11);
            empty.ForeColor = Ui.Muted;
            empty.Margin = new Padding(0, 24, 0, 0);
            catalog.Controls.Add(empty);
        }
        catalog.ResumeLayout(true);
        ArrangeCatalogCards();
    }

    private void ArrangeCatalogCards()
    {
        if (catalog.IsDisposed) return;
        var available = Math.Max(1, catalog.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
        var columns = available >= 440 ? 2 : 1;
        var width = Math.Max(1, (available - (columns - 1) * 16) / columns);
        var index = 0;
        catalog.SuspendLayout();
        foreach (Control control in catalog.Controls)
        {
            if (control is Panel card && card.Tag is Instrument)
            {
                card.Width = width;
                card.Margin = new Padding(0, 0, index++ % columns == columns - 1 ? 0 : 16, 16);
            }
            else control.MaximumSize = new Size(available, 0);
        }
        catalog.ResumeLayout(true);
    }

    private static Button SmallButton(string text, Action action, string accessibleName)
    {
        var button = Ui.Button(text, action);
        button.AutoSize = false;
        button.MinimumSize = Size.Empty;
        button.Padding = Padding.Empty;
        button.Dock = DockStyle.Fill;
        button.Margin = Padding.Empty;
        button.AccessibleName = accessibleName;
        return button;
    }

    private void AddInstrument(Instrument instrument)
    {
        var current = cart.GetValueOrDefault(instrument.Id);
        if (current >= 100)
        {
            MessageBox.Show(this, "O limite é de 100 unidades por instrumento.", "Quantidade máxima", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        cart[instrument.Id] = current + 1;
        RenderCart();
    }

    private void RenderCart()
    {
        orderLines.SuspendLayout();
        DisposeChildren(orderLines);
        foreach (var item in cart)
        {
            var instrument = Catalog.All.Single(entry => entry.Id == item.Key);
            var line = new TableLayoutPanel { Name = "OrderLine_" + item.Key, Height = 115, ColumnCount = 2, RowCount = 4, Margin = new Padding(0, 0, 0, 12), TabIndex = orderLines.Controls.Count };
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            foreach (var height in new[] { 37, 22, 42, 1 }) line.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            var name = FieldLabel(instrument.Name);
            name.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            name.AutoEllipsis = true;
            var lineTotal = FieldLabel(Ui.Money(instrument.Price * item.Value));
            lineTotal.Font = new Font("Segoe UI", 9);
            lineTotal.TextAlign = ContentAlignment.MiddleRight;
            lineTotal.AutoEllipsis = true;
            line.Controls.Add(name, 0, 0);
            line.Controls.Add(lineTotal, 1, 0);
            var brand = FieldLabel($"{instrument.Brand} · {Ui.Money(instrument.Price)} / un.");
            brand.ForeColor = Ui.Muted;
            brand.Font = new Font("Segoe UI", 8);
            brand.AutoEllipsis = true;
            line.Controls.Add(brand, 0, 1);
            line.SetColumnSpan(brand, 2);
            var stepper = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 5, 0, 5), TabIndex = 0 };
            stepper.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));
            stepper.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
            stepper.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));
            var amount = new NumericUpDown { Name = "Quantity_" + item.Key, Minimum = 1, Maximum = 100, Value = item.Value, DecimalPlaces = 0, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill, Margin = new Padding(3, 0, 3, 0), AccessibleName = $"Quantidade de {instrument.Name}", TabIndex = 1 };
            var minus = SmallButton("−", () =>
            {
                if (amount.Value > 1) amount.Value--;
                else { cart.Remove(item.Key); RenderCart(); }
            }, $"Diminuir quantidade de {instrument.Name}");
            minus.TabIndex = 0;
            var plus = SmallButton("+", () => { if (amount.Value < amount.Maximum) amount.Value++; }, $"Aumentar quantidade de {instrument.Name}");
            plus.TabIndex = 2;
            plus.Enabled = item.Value < 100;
            amount.ValueChanged += (_, _) =>
            {
                cart[item.Key] = decimal.ToInt32(amount.Value);
                lineTotal.Text = Ui.Money(instrument.Price * amount.Value);
                plus.Enabled = amount.Value < amount.Maximum;
                UpdateAmounts();
            };
            stepper.Controls.Add(minus, 0, 0);
            stepper.Controls.Add(amount, 1, 0);
            stepper.Controls.Add(plus, 2, 0);
            line.Controls.Add(stepper, 0, 2);
            var remove = new LinkLabel { Name = "Remove_" + item.Key, Text = "Remover", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, LinkColor = Ui.Muted, ActiveLinkColor = Ui.Orange, VisitedLinkColor = Ui.Muted, Margin = Padding.Empty, AccessibleName = $"Remover {instrument.Name} do pedido", TabIndex = 1 };
            remove.Click += (_, _) => { cart.Remove(item.Key); RenderCart(); };
            line.Controls.Add(remove, 1, 2);
            var separator = Separator();
            line.Controls.Add(separator, 0, 3);
            line.SetColumnSpan(separator, 2);
            orderLines.Controls.Add(line);
        }
        if (cart.Count == 0)
        {
            var empty = FieldLabel("Seu próximo som começa aqui.\nAdicione instrumentos ao pedido.");
            empty.Dock = DockStyle.None;
            empty.Height = 100;
            empty.ForeColor = Ui.Muted;
            empty.TextAlign = ContentAlignment.MiddleCenter;
            orderLines.Controls.Add(empty);
        }
        orderLines.ResumeLayout(true);
        ArrangeOrderLines();
        UpdateAmounts();
    }

    private void ArrangeOrderLines()
    {
        var width = Math.Max(1, orderLines.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 1);
        foreach (Control line in orderLines.Controls) line.Width = width;
    }

    private void UpdateAmounts()
    {
        // Sale owns the rounding rule, shared by this preview, persisted sales and reports.
        var lines = cart.Select(item =>
        {
            var instrument = Catalog.All.Single(entry => entry.Id == item.Key);
            return new SaleLine(instrument.Id, instrument.Name, instrument.Brand, item.Value, instrument.Price);
        }).ToArray();
        var preview = new Sale("preview", DateTimeOffset.UtcNow, customer.Text, (PaymentMethod)payment.SelectedIndex, discount.Value, lines);
        quantity.Text = $"{preview.Quantity} {(preview.Quantity == 1 ? "instrumento" : "instrumentos")}";
        subtotalValue.Text = Ui.Money(preview.Subtotal);
        discountValue.Text = $"− {Ui.Money(preview.Discount)}";
        totalValue.Text = Ui.Money(preview.Total);
        finish.Enabled = HasPendingOrder;
        clear.Enabled = HasPendingOrder;
    }

    private void ClearOrder()
    {
        if (!HasPendingOrder) return;
        if (MessageBox.Show(this, "Limpar todos os itens e dados deste pedido?", "Limpar pedido", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        ResetOrder();
        search.Focus();
    }

    private void ResetOrder()
    {
        cart.Clear();
        customer.Clear();
        payment.SelectedIndex = 0;
        discount.Value = 0;
        RenderCart();
    }

    private void CompleteSale()
    {
        if (!HasPendingOrder || !finish.Enabled) return;
        Sale sale;
        finish.Enabled = false;
        try
        {
            // Value confirma o texto ainda em edição. Ctrl+Enter pode chegar antes
            // de LostFocus e, sem isso, cart ainda conteria a quantidade anterior.
            foreach (var id in cart.Keys.ToArray())
                _ = orderLines.Controls.Find("Quantity_" + id, true).OfType<NumericUpDown>().Single().Value;
            _ = discount.Value;
            finish.Enabled = false;
            sale = store.Create(new SaleRequest(customer.Text, (PaymentMethod)payment.SelectedIndex, discount.Value,
                cart.Select(item => new CartItem(item.Key, item.Value)).ToArray()));
        }
        catch (Exception error)
        {
            // A rejected request or failed write must leave the complete order available to retry.
            UpdateAmounts();
            Ui.Error(error);
            customer.Focus();
            return;
        }
        ResetOrder();
        MessageBox.Show(this, $"Venda registrada!\n\nCliente: {sale.Customer}\nTotal: {Ui.Money(sale.Total)}\nPedido: {sale.Id}\n\nConfira os detalhes no relatório.", "Tudo em harmonia", MessageBoxButtons.OK, MessageBoxIcon.Information);
        SaleCompleted?.Invoke(this, EventArgs.Empty);
    }

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F)) { search.Focus(); search.SelectAll(); return true; }
        if (keyData == (Keys.Control | Keys.Enter)) { CompleteSale(); return true; }
        return base.ProcessCmdKey(ref message, keyData);
    }

    private static void DisposeChildren(Control parent)
    {
        foreach (var control in parent.Controls.Cast<Control>().ToArray()) control.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) tips.Dispose();
        base.Dispose(disposing);
    }
}
