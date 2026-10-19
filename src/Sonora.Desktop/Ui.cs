using Sonora.Core;

namespace Sonora.Desktop;

internal static class Ui
{
    internal static readonly Color Ink = Color.FromArgb(32, 35, 34);
    internal static readonly Color Orange = Color.FromArgb(230, 103, 62);
    internal static readonly Color Paper = Color.FromArgb(247, 247, 242);
    internal static string Money(decimal value) => value.ToString("C2");
    internal static Label Text(string text, int size = 11, bool bold = false) => new()
    {
        Text = text, AutoSize = true, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = Ink, Margin = new Padding(0, 0, 12, 12)
    };
    internal static Button Button(string text, Action action, bool primary = false)
    {
        var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(120, 42), Padding = new Padding(12, 6, 12, 6),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Orange : Color.White, ForeColor = primary ? Color.White : Ink,
            Cursor = Cursors.Hand, Margin = new Padding(0, 0, 12, 12), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        button.FlatAppearance.BorderColor = primary ? Orange : Color.FromArgb(220, 223, 218);
        button.Click += (_, _) => action();
        return button;
    }
    internal static FlowLayoutPanel Stack() => new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(28), BackColor = Paper };
    internal static void Window(Form form, string title)
    {
        form.Text = $"Sonora · {title}"; form.Size = new Size(1240, 820); form.MinimumSize = new Size(1050, 700);
        form.StartPosition = FormStartPosition.CenterScreen; form.BackColor = Paper; form.Font = new Font("Segoe UI", 10);
        form.AutoScaleMode = AutoScaleMode.Dpi;
    }
    internal static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None, RowTemplate = { Height = 40 }, ColumnHeadersHeight = 42,
        EnableHeadersVisualStyles = false, ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Ink, ForeColor = Color.White },
        DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(250, 227, 214), SelectionForeColor = Ink, Padding = new Padding(6) }
    };
    internal static void Error(Exception error) => MessageBox.Show(error.Message, "Confira os dados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    internal static ComboBox Payments(bool all = false)
    {
        var combo = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 12, 16) };
        if (all) combo.Items.Add("Todos os pagamentos");
        foreach (var payment in Enum.GetValues<PaymentMethod>()) combo.Items.Add(Reports.PaymentLabel(payment));
        combo.SelectedIndex = 0; return combo;
    }
}
