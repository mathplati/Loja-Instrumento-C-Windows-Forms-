using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Sonora.Core;

public sealed class SaleStore
{
    private static readonly ConcurrentDictionary<string, object> FileGates = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly string path;
    private readonly object gate;
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public SaleStore(string path)
    {
        this.path = Path.GetFullPath(path);
        gate = FileGates.GetOrAdd(this.path, _ => new object());
        Directory.CreateDirectory(Path.GetDirectoryName(this.path)!);
    }

    private List<Sale> Read()
    {
        if (!File.Exists(path)) return [];
        try
        {
            var sales = JsonSerializer.Deserialize<List<Sale>>(File.ReadAllText(path), json)
                ?? throw new InvalidDataException("O arquivo de vendas está vazio ou inválido.");
            if (sales.Any(sale => sale is null || string.IsNullOrWhiteSpace(sale.Id) || string.IsNullOrWhiteSpace(sale.Customer)
                || !Enum.IsDefined(sale.Payment) || sale.DiscountPercent is < 0 or > 30
                || sale.Items is null || sale.Items.Count == 0
                || sale.Items.Any(item => item is null || string.IsNullOrWhiteSpace(item.InstrumentId)
                    || string.IsNullOrWhiteSpace(item.Name) || item.Quantity is < 1 or > 100 || item.UnitPrice <= 0))
                || sales.Select(sale => sale.Id).Distinct().Count() != sales.Count)
                throw new InvalidDataException("O arquivo de vendas contém registros inválidos. Preserve-o para recuperação.");
            return sales;
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("O arquivo de vendas está corrompido. Preserve-o para recuperação; nenhuma venda foi sobrescrita.", error);
        }
    }

    public IReadOnlyList<Sale> List(DateOnly? from = null, DateOnly? to = null, PaymentMethod? payment = null)
    {
        if (from > to) throw new ArgumentException("A data inicial deve ser anterior ou igual à data final.");
        lock (gate)
        {
            return Read().Where(sale => (!from.HasValue || StoreClock.Date(sale.CreatedAt) >= from)
                && (!to.HasValue || StoreClock.Date(sale.CreatedAt) <= to)
                && (!payment.HasValue || sale.Payment == payment)).OrderByDescending(sale => sale.CreatedAt).ToArray();
        }
    }

    public Sale Create(SaleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var customer = request.Customer?.Trim() ?? "";
        if (customer.Length is < 2 or > 80) throw new ArgumentException("Informe o nome do cliente entre 2 e 80 caracteres.");
        if (!Enum.IsDefined(request.Payment)) throw new ArgumentException("Escolha uma forma de pagamento válida.");
        if (request.DiscountPercent is < 0 or > 30) throw new ArgumentException("O desconto deve estar entre 0% e 30%.");
        if (request.Items is null || request.Items.Count is < 1 or > 100) throw new ArgumentException("Adicione pelo menos um instrumento ao pedido (máximo de 100 linhas).");
        if (request.Items.Any(item => item is null || item.Quantity is < 1 or > 100 || string.IsNullOrWhiteSpace(item.InstrumentId)))
            throw new ArgumentException("Cada item precisa de um instrumento e quantidade entre 1 e 100.");
        var lines = request.Items.GroupBy(item => item.InstrumentId).Select(group =>
        {
            var instrument = Catalog.All.FirstOrDefault(item => item.Id == group.Key)
                ?? throw new ArgumentException("Instrumento não encontrado no catálogo.");
            var quantity = group.Sum(item => item.Quantity);
            if (quantity > 100) throw new ArgumentException("O limite é de 100 unidades por instrumento.");
            return new SaleLine(instrument.Id, instrument.Name, instrument.Brand, quantity, instrument.Price);
        }).ToArray();
        var sale = new Sale($"SON-{Guid.NewGuid():N}", DateTimeOffset.UtcNow, customer, request.Payment, request.DiscountPercent, lines);
        lock (gate)
        {
            var sales = Read();
            sales.Add(sale);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonSerializer.Serialize(sales, json), new UTF8Encoding(false));
                File.Move(temp, path, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        return sale;
    }
}
