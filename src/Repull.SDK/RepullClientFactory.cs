using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Repull.SDK;

/// <summary>
/// Convenience factory for <see cref="RepullClient"/>. Wires the Bearer-token
/// authentication provider, the default Kiota request adapter, and the Repull
/// production base URL so callers can get a working client in one line:
/// <code>var client = RepullClientFactory.Create("sk_live_...");</code>
/// </summary>
public static class RepullClientFactory
{
    /// <summary>
    /// Default Repull production base URL.
    /// </summary>
    public const string DefaultBaseUrl = "https://api.repull.dev";

    /// <summary>
    /// Build a fully-configured <see cref="RepullClient"/> from an API key.
    /// </summary>
    /// <param name="apiKey">A Repull API key (sk_test_... or sk_live_...).</param>
    /// <param name="baseUrl">Optional base URL override (defaults to https://api.repull.dev).</param>
    public static RepullClient Create(string apiKey, string? baseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("apiKey is required", nameof(apiKey));
        }

        var auth = new BearerTokenAuthenticationProvider(apiKey);
        var adapter = new HttpClientRequestAdapter(auth)
        {
            BaseUrl = baseUrl ?? DefaultBaseUrl,
        };
        return new RepullClient(adapter);
    }
}

/// <summary>
/// Authentication provider that injects <c>Authorization: Bearer {apiKey}</c>
/// on every request. Static keys (no refresh, no token exchange).
/// </summary>
internal sealed class BearerTokenAuthenticationProvider : IAuthenticationProvider
{
    private readonly string _apiKey;

    public BearerTokenAuthenticationProvider(string apiKey)
    {
        _apiKey = apiKey;
    }

    public Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        return Task.CompletedTask;
    }
}
