using System.Globalization;
using System.Text;

namespace Sonora.Core;

public sealed record SalesSummary(int Sales, int Instruments, decimal Revenue, decimal AverageTicket);

public static class Reports
{
    public static SalesSummary Summarize(IEnumerable<Sale> source)
    {
        var sales = source.ToArray();
        return new(sales.Length, sales.Sum(s => s.Quantity), sales.Sum(s => s.Total),
            sales.Length == 0 ? 0 : decimal.Round(sales.Average(s => s.Total), 2));
    }

    public static string PaymentLabel(PaymentMethod payment) => payment switch
    {
        PaymentMethod.Pix => "Pix", PaymentMethod.Dinheiro => "Dinheiro",
        PaymentMethod.CartaoDebito => "Cartão de débito", PaymentMethod.CartaoCredito => "Cartão de crédito",
        _ => "Desconhecido"
    };

    private static string Cell(string value)
    {
        // Evita fórmulas ao abrir nomes de clientes em uma planilha.
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+')
            || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    public static string Csv(IEnumerable<Sale> sales)
    {
        var br = CultureInfo.GetCultureInfo("pt-BR");
        var csv = new StringBuilder("Pedido;Data;Cliente;Pagamento;Instrumentos;Subtotal;Desconto;Total\r\n");
        foreach (var sale in sales)
        {
            var date = TimeZoneInfo.ConvertTime(sale.CreatedAt, StoreClock.Zone).ToString("dd/MM/yyyy HH:mm", br);
            csv.AppendJoin(';', Cell(sale.Id), Cell(date), Cell(sale.Customer), Cell(PaymentLabel(sale.Payment)),
                sale.Quantity.ToString(br), sale.Subtotal.ToString("F2", br), sale.Discount.ToString("F2", br), sale.Total.ToString("F2", br));
            csv.Append("\r\n");
        }
        return csv.ToString();
    }
}
