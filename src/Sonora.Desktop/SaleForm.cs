using Sonora.Core;

namespace Sonora.Desktop;

public sealed class SaleForm : Form
{
    private readonly SaleStore store;
    private readonly Dictionary<string, int> cart = [];
    private readonly FlowLayoutPanel catalog = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
    private readonly TextBox search = new() { Width = 290, PlaceholderText = "Buscar instrumento ou marca…" };
    private readonly ComboBox category = new() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox customer = new() { Width = 300, MaxLength = 80, PlaceholderText = "Nome do cliente" };
    private readonly ComboBox payment = Ui.Payments();
    private readonly NumericUpDown discount = new() { Maximum = 30, DecimalPlaces = 2, Width = 100 };
    private readonly DataGridView grid = Ui.Grid();
    private readonly Label total = Ui.Text("", 15, true);
    private readonly Button finish;

    public SaleForm(SaleStore store)
    {
        this.store = store; Ui.Window(this, "Nova venda");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));
        var left = new Panel { Dock = DockStyle.Fill }; var header = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 130, Padding = new Padding(24) };
        header.Controls.Add(Ui.Text("Encontre o próximo som.", 24, true)); header.SetFlowBreak(header.Controls[0], true);
        header.Controls.Add(search); header.Controls.Add(category);
        category.Items.AddRange(["Todas as categorias", "Cordas", "Teclas", "Percussão"]); category.SelectedIndex = 0;
        search.TextChanged += (_, _) => RenderCatalog(); category.SelectedIndexChanged += (_, _) => RenderCatalog();
        left.Controls.Add(catalog); left.Controls.Add(header);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20), RowCount = 3 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 205)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
        var fields = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        fields.Controls.Add(Ui.Text("Seu pedido", 22, true)); fields.Controls.Add(Ui.Text("Cliente")); fields.Controls.Add(customer);
        fields.Controls.Add(Ui.Text("Pagamento")); fields.Controls.Add(payment);
        grid.Columns.Add("name", "Instrumento"); grid.Columns.Add("quantity", "Qtd."); grid.Columns[1].FillWeight = 30;
        grid.Columns.Add(new DataGridViewButtonColumn { Name = "remove", HeaderText = "", Text = "− 1", UseColumnTextForButtonValue = true, FillWeight = 35 });
        grid.CellContentClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 2) return;
            var id = (string)grid.Rows[e.RowIndex].Tag!; if (--cart[id] == 0) cart.Remove(id); RenderCart();
        };
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        bottom.Controls.Add(Ui.Text("Desconto (%) · máximo 30%")); bottom.Controls.Add(discount); bottom.Controls.Add(total);
        finish = Ui.Button("✓  Finalizar venda", CompleteSale, true); bottom.Controls.Add(finish);
        bottom.Controls.Add(Ui.Button("Limpar pedido", () =>
        {
            if (cart.Count == 0 || MessageBox.Show("Limpar todos os itens deste pedido?", "Limpar pedido", MessageBoxButtons.YesNo) == DialogResult.Yes)
            { cart.Clear(); RenderCart(); }
        }));
        discount.ValueChanged += (_, _) => RenderCart();
        right.Controls.Add(fields, 0, 0); right.Controls.Add(grid, 0, 1); right.Controls.Add(bottom, 0, 2);
        layout.Controls.Add(left, 0, 0); layout.Controls.Add(right, 1, 0); Controls.Add(layout);
        RenderCatalog(); RenderCart();
    }

    private void RenderCatalog()
    {
        catalog.Controls.Clear();
        var found = Catalog.All.Where(x => (category.SelectedIndex == 0 || x.Category == category.Text)
            && $"{x.Name} {x.Brand}".Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();
        foreach (var instrument in found)
        {
            var card = new FlowLayoutPanel { Width = 260, Height = 185, Padding = new Padding(16), BackColor = Color.White, Margin = new Padding(0, 0, 16, 16), FlowDirection = FlowDirection.TopDown, WrapContents = false };
            card.Controls.Add(Ui.Text(instrument.Category.ToUpperInvariant(), 9)); card.Controls.Add(Ui.Text(instrument.Name, 13, true));
            card.Controls.Add(Ui.Text(instrument.Brand));
            card.Controls.Add(Ui.Button($"+  {Ui.Money(instrument.Price)}", () =>
            {
                var quantity = cart.GetValueOrDefault(instrument.Id);
                if (quantity >= 100) { MessageBox.Show("Limite de 100 unidades por instrumento."); return; }
                cart[instrument.Id] = quantity + 1; RenderCart();
            }));
            catalog.Controls.Add(card);
        }
        if (found.Length == 0) catalog.Controls.Add(Ui.Text("Nenhum instrumento encontrado. Tente outra busca."));
    }

    private void RenderCart()
    {
        grid.Rows.Clear(); decimal subtotal = 0;
        foreach (var item in cart)
        {
            var instrument = Catalog.All.Single(x => x.Id == item.Key); subtotal += instrument.Price * item.Value;
            var row = grid.Rows.Add(instrument.Name, item.Value); grid.Rows[row].Tag = item.Key;
        }
        var reduction = decimal.Round(subtotal * discount.Value / 100, 2, MidpointRounding.AwayFromZero);
        total.Text = $"Total: {Ui.Money(subtotal - reduction)}"; finish.Enabled = cart.Count > 0;
    }

    private void CompleteSale()
    {
        finish.Enabled = false;
        try
        {
            var sale = store.Create(new(customer.Text, (PaymentMethod)payment.SelectedIndex, discount.Value,
                cart.Select(item => new CartItem(item.Key, item.Value)).ToArray()));
            MessageBox.Show($"Venda registrada!\n\nCliente: {sale.Customer}\nTotal: {Ui.Money(sale.Total)}\nPedido: {sale.Id}\n\nConfira os detalhes no relatório.", "Tudo em harmonia", MessageBoxButtons.OK, MessageBoxIcon.Information);
            cart.Clear(); customer.Clear(); discount.Value = 0; payment.SelectedIndex = 0;
        }
        catch (Exception error) { Ui.Error(error); }
        finally { RenderCart(); }
    }
}
