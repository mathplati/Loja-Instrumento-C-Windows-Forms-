using System.Text;
using System.Text.Json.Serialization;
using Sonora.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var dataFile = Environment.GetEnvironmentVariable("SONORA_DATA_FILE")
    ?? Path.Combine(builder.Environment.ContentRootPath, "..", "..", "data", "sales.json");
builder.Services.AddSingleton(new SaleStore(dataFile));
var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    if (HttpMethods.IsPost(context.Request.Method) && context.Request.Headers["X-Sonora-Client"] != "preview")
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsJsonAsync(new { message = "Operação permitida apenas pela interface da loja." });
        return;
    }
    try { await next(context); }
    catch (ArgumentException error) { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { message = error.Message }); }
    catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
    {
        app.Logger.LogError(error, "Falha no arquivo de vendas"); context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { message = "Não foi possível acessar os dados. Confira o arquivo de vendas e as permissões; os dados existentes foram preservados." });
    }
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/catalog", () => Catalog.All);
app.MapGet("/api/clock", () => new { today = StoreClock.Today.ToString("yyyy-MM-dd") });
app.MapGet("/api/sales", (SaleStore store, DateOnly? from, DateOnly? to, PaymentMethod? payment) => store.List(from, to, payment));
app.MapPost("/api/sales", (SaleStore store, SaleRequest request) =>
{
    var sale = store.Create(request); return Results.Created($"/api/sales/{sale.Id}", sale);
});
app.MapGet("/api/sales/{id}", (SaleStore store, string id) => store.List().FirstOrDefault(s => s.Id == id) is { } sale ? Results.Ok(sale) : Results.NotFound());
app.MapGet("/api/report.csv", (SaleStore store, DateOnly? from, DateOnly? to, PaymentMethod? payment) =>
{
    var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(Reports.Csv(store.List(from, to, payment)))).ToArray();
    return Results.File(bytes, "text/csv; charset=utf-8", $"sonora-vendas-{StoreClock.Today:yyyy-MM-dd}.csv");
});
app.Run();
