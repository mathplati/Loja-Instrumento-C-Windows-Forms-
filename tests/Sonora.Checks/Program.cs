using Sonora.Core;

var root = Path.Combine(Path.GetTempPath(), "sonora-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var passed = 0;
try
{
    void Check(string name, Action run)
    {
        run();
        passed++;
        Console.WriteLine($"PASS {name}");
    }
    void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Esperado {expected}; obtido {actual}");
    }
    void Reject(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new Exception("Entrada inválida foi aceita.");
    }
    var file = Path.Combine(root, "sales.json");
    var store = new SaleStore(file);
    SaleRequest Valid() => new("Cliente Teste", PaymentMethod.Pix, 10m, [new("guitar-strato", 2)]);
    Check("catálogo com IDs únicos e preços positivos", () =>
    {
        Equal(8, Catalog.All.Count);
        Equal(8, Catalog.All.Select(x => x.Id).Distinct().Count());
        Equal(true, Catalog.All.All(x => x.Price > 0));
    });
    Check("relatório vazio tem totais zero", () => Equal(new SalesSummary(0, 0, 0, 0), Reports.Summarize(store.List())));
    Check("pedido calcula desconto e usa o preço do catálogo", () =>
    {
        var sale = store.Create(Valid());
        Equal(2580m, sale.Subtotal); Equal(258m, sale.Discount); Equal(2322m, sale.Total); Equal(2, sale.Quantity);
    });
    Check("vendas sobrevivem à reabertura do armazenamento", () => Equal(2322m, new SaleStore(file).List().Single().Total));
    Check("linhas repetidas são agrupadas", () =>
    {
        var sale = store.Create(Valid() with { Items = [new("guitar-strato", 1), new("guitar-strato", 2)] });
        Equal(1, sale.Items.Count); Equal(3, sale.Items[0].Quantity);
    });
    Check("centavos são arredondados corretamente", () =>
    {
        var sale = store.Create(Valid() with { DiscountPercent = 1.5m, Items = [new("ukulele-concert", 1)] });
        Equal(5.24m, sale.Discount); Equal(343.76m, sale.Total);
    });
    Check("cliente ausente ou muito longo é recusado", () =>
    {
        Reject(() => store.Create(Valid() with { Customer = " " }));
        Reject(() => store.Create(Valid() with { Customer = new string('a', 81) }));
    });
    Check("carrinho vazio e produto desconhecido são recusados", () =>
    {
        Reject(() => store.Create(Valid() with { Items = [] }));
        Reject(() => store.Create(Valid() with { Items = [new("inexistente", 1)] }));
    });
    Check("quantidades inválidas e agrupamento acima do limite são recusados", () =>
    {
        foreach (var quantity in new[] { -1, 0, 101 }) Reject(() => store.Create(Valid() with { Items = [new("cajon", quantity)] }));
        Reject(() => store.Create(Valid() with { Items = [new("cajon", 60), new("cajon", 50)] }));
    });
    Check("desconto e pagamento inválidos são recusados", () =>
    {
        Reject(() => store.Create(Valid() with { DiscountPercent = -1 }));
        Reject(() => store.Create(Valid() with { DiscountPercent = 31 }));
        Reject(() => store.Create(Valid() with { Payment = (PaymentMethod)42 }));
    });
    Check("validações não gravam vendas", () => Equal(3, store.List().Count));
    Check("filtros de data incluem os limites e excluem outros dias", () =>
    {
        Equal(3, store.List(StoreClock.Today, StoreClock.Today).Count);
        Equal(0, store.List(null, StoreClock.Today.AddDays(-1)).Count);
        Reject(() => store.List(StoreClock.Today, StoreClock.Today.AddDays(-1)));
    });
    Check("filtro por pagamento", () => Equal(0, store.List(payment: PaymentMethod.Dinheiro).Count));
    Check("resumo soma pedidos e unidades sem perder centavos", () =>
    {
        var summary = Reports.Summarize(store.List());
        Equal(3, summary.Sales); Equal(6, summary.Instruments); Equal(6148.76m, summary.Revenue);
    });
    Check("CSV escapa aspas, separadores e fórmulas", () =>
    {
        var sale = store.Create(Valid() with { Customer = "=HYPERLINK(\"teste\");" });
        var csv = Reports.Csv([sale]);
        Equal(true, csv.Contains("\"'=HYPERLINK(\"\"teste\"\");\""));
        Equal(true, csv.Contains("2322,00"));
    });
    Check("gravações simultâneas não perdem vendas", () =>
    {
        Parallel.For(0, 20, _ => store.Create(Valid()));
        Equal(24, store.List().Count);
        Equal(24, store.List().Select(s => s.Id).Distinct().Count());
    });
    Check("instâncias distintas compartilham a proteção do arquivo", () =>
    {
        var shared = Path.Combine(root, "shared.json");
        Parallel.For(0, 30, _ => new SaleStore(shared).Create(Valid()));
        Equal(30, new SaleStore(shared).List().Count);
    });
    Check("arquivo corrompido é preservado e reportado", () =>
    {
        var bad = Path.Combine(root, "bad.json"); File.WriteAllText(bad, "{broken");
        try { new SaleStore(bad).Create(Valid()); throw new Exception("Arquivo inválido aceito"); }
        catch (InvalidDataException) { Equal("{broken", File.ReadAllText(bad)); }
    });
    Check("JSON com registros incompletos também é preservado", () =>
    {
        foreach (var content in new[] { "[{}]", "[null]", "null" })
        {
            var bad = Path.Combine(root, "incomplete.json"); File.WriteAllText(bad, content);
            try { new SaleStore(bad).Create(Valid()); throw new Exception("Registro incompleto aceito"); }
            catch (InvalidDataException) { Equal(content, File.ReadAllText(bad)); }
        }
    });
    Console.WriteLine($"\n{passed} verificações aprovadas.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"FAIL após {passed} verificações: {error}");
    return 1;
}
finally { Directory.Delete(root, true); }
