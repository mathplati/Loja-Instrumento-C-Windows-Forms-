namespace Sonora.Core;

public sealed record Instrument(string Id, string Name, string Brand, string Category, decimal Price, string Symbol, string Color);

public static class Catalog
{
    public static IReadOnlyList<Instrument> All { get; } = Array.AsReadOnly(new[]
    {
        new Instrument("guitar-strato", "Guitarra Stratocaster", "Tagima TG-530", "Cordas", 1290m, "guitar", "sand"),
        new Instrument("acoustic-folk", "Violão Folk", "Yamaha F310", "Cordas", 1099m, "acoustic", "peach"),
        new Instrument("keyboard-61", "Teclado 61 teclas", "Casio CT-S300", "Teclas", 1590m, "keyboard", "sage"),
        new Instrument("drums-acoustic", "Bateria acústica", "Pearl Roadshow", "Percussão", 4890m, "drums", "lavender"),
        new Instrument("bass-jazz", "Contrabaixo Jazz Bass", "Tagima TW-73", "Cordas", 1790m, "bass", "sage"),
        new Instrument("ukulele-concert", "Ukulele Concert", "Kalani 24\"", "Cordas", 349m, "acoustic", "sand"),
        new Instrument("cajon", "Cajón acústico", "FSA Design", "Percussão", 459m, "cajon", "peach"),
        new Instrument("piano-digital", "Piano digital", "Yamaha P-145", "Teclas", 4290m, "keyboard", "lavender")
    });
}
