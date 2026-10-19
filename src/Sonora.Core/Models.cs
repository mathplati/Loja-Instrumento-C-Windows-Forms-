namespace Sonora.Core;

public enum PaymentMethod { Pix, Dinheiro, CartaoDebito, CartaoCredito }
public sealed record CartItem(string InstrumentId, int Quantity);
public sealed record SaleRequest(string Customer, PaymentMethod Payment, decimal DiscountPercent, IReadOnlyList<CartItem> Items);
public sealed record SaleLine(string InstrumentId, string Name, string Brand, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}
public sealed record Sale(string Id, DateTimeOffset CreatedAt, string Customer, PaymentMethod Payment,
    decimal DiscountPercent, IReadOnlyList<SaleLine> Items)
{
    public decimal Subtotal => Items.Sum(item => item.Total);
    public decimal Discount => decimal.Round(Subtotal * DiscountPercent / 100, 2, MidpointRounding.AwayFromZero);
    public decimal Total => Subtotal - Discount;
    public int Quantity => Items.Sum(item => item.Quantity);
}

public static class StoreClock
{
    public static TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    public static DateOnly Date(DateTimeOffset value) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(value, Zone).DateTime);
    public static DateOnly Today => Date(DateTimeOffset.UtcNow);
}
