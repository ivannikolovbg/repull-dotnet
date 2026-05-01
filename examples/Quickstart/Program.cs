using Repull.SDK;

var apiKey = Environment.GetEnvironmentVariable("REPULL_API_KEY")
    ?? throw new Exception("REPULL_API_KEY not set");

var client = RepullClientFactory.Create(apiKey);

var page = await client.ListReservationsAsync(q => q.Limit = 10);

Console.WriteLine($"Total reservations: {page?.Pagination?.Total}");
Console.WriteLine();

foreach (var r in page?.Data ?? new())
{
    Console.WriteLine($"{r.IdString}\t{r.CheckIn} -> {r.CheckOut}\t{r.Platform}\t{r.TotalPrice} {r.Currency}");
}
