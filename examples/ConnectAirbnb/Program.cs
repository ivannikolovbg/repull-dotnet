using Repull.SDK;
using Repull.SDK.V1.Connect.Item;

var apiKey = Environment.GetEnvironmentVariable("REPULL_API_KEY")
    ?? throw new Exception("REPULL_API_KEY not set");

var client = RepullClientFactory.Create(apiKey);

// 1. List currently connected providers.
var connections = await client.V1.Connect.GetAsConnectGetResponseAsync();
foreach (var c in connections?.Data ?? new())
{
    Console.WriteLine($"Connection {c.Id}\t{c.Provider}\t{c.Status}");
}

// 2. Kick off an Airbnb OAuth connect flow.
var connection = await client.V1.Connect["airbnb"].PostAsync(new WithProviderPostRequestBody
{
    RedirectUrl = "https://your-app.example.com/oauth/callback",
});

Console.WriteLine();
Console.WriteLine($"Connection created: id={connection?.Id} status={connection?.Status}");
Console.WriteLine($"Provider: {connection?.Provider}");
