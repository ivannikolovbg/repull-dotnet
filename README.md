# Repull .NET SDK

.NET SDK for [Repull](https://repull.dev). Generated from OpenAPI via [Kiota](https://github.com/microsoft/kiota). .NET 8.0+.

Build vacation-rental tech in .NET — connect to 50+ PMS platforms and 4 OTA channels through one REST API. Built-in AI operations for guest communication, pricing, and listing optimization.

## Status

`v0.1.0-alpha` — NuGet listing pending. Install from source for now (see below).

## Install

> Not yet published to NuGet. Until then, clone and reference the project directly.

```bash
git clone https://github.com/ivannikolovbg/repull-dotnet.git
```

Add a project reference from your app:

```xml
<ItemGroup>
  <ProjectReference Include="../repull-dotnet/src/Repull.SDK/Repull.SDK.csproj" />
</ItemGroup>
```

Once published, install will be:

```bash
dotnet add package Repull.SDK
```

## Quick start

```csharp
using Repull.SDK;

var client = RepullClientFactory.Create(Environment.GetEnvironmentVariable("REPULL_API_KEY")!);

var page = await client.ListReservationsAsync(q => q.Limit = 10);

Console.WriteLine($"Total: {page?.Pagination?.Total}");
foreach (var r in page?.Data ?? new())
{
    Console.WriteLine($"{r.IdString}\t{r.CheckIn} -> {r.CheckOut}\t{r.Platform}\t{r.TotalPrice} {r.Currency}");
}
```

Full runnable copy lives in [`examples/Quickstart`](examples/Quickstart).

## Authentication

All requests require a Repull API key (`sk_test_...` for sandbox, `sk_live_...` for production):

```csharp
var client = RepullClientFactory.Create("sk_live_...");
```

Get a key at [repull.dev/dashboard](https://repull.dev/dashboard).

`RepullClientFactory.Create` wires a `Bearer` auth provider, the default Kiota request adapter, and the `https://api.repull.dev` base URL. If you need to customize either, pass a `baseUrl` argument or build the client directly:

```csharp
using Microsoft.Kiota.Http.HttpClientLibrary;

var adapter = new HttpClientRequestAdapter(yourAuthProvider) { BaseUrl = "..." };
var client = new RepullClient(adapter);
```

## Examples

| Example                                             | What it does                                              |
| --------------------------------------------------- | --------------------------------------------------------- |
| [`Quickstart`](examples/Quickstart)                 | List reservations across all connected PMS platforms      |
| [`ConnectAirbnb`](examples/ConnectAirbnb)           | OAuth-connect an Airbnb host account                      |

Run any example:

```bash
REPULL_API_KEY=sk_test_... dotnet run --project examples/Quickstart
```

## Reference

- API docs: [repull.dev/docs](https://repull.dev/docs)
- OpenAPI spec: [api.repull.dev/openapi.json](https://api.repull.dev/openapi.json)
- Snapshot of the spec used for code generation: [`openapi/v1.json`](openapi/v1.json)

## Regenerating the client

The generated client lives in `src/Repull.SDK/` (everything except `RepullClient.Partial.cs`, `RepullClientFactory.cs`, `RepullClientExtensions.cs`, `Models/Reservation.Partial.cs`, and `Models/ReservationsPage.cs`, which are hand-written extensions).

To regenerate after a spec change:

```bash
dotnet tool install --global Microsoft.OpenApi.Kiota
scripts/regen.sh --remote   # pull latest spec + regenerate
dotnet build
```

## License

MIT — see [LICENSE](LICENSE).

---

Powered by Repull. AI features powered by [Vanio AI](https://www.vanio.ai).
