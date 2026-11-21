using System.Net;
using System.Text;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Repull.SDK.Tests;

/// <summary>A captured outgoing request: what the SDK actually put on the wire.</summary>
public sealed record CapturedRequest(string Method, string Path, string Query, string? Body);

/// <summary>Records every request the SDK sends and answers 200 with a fixed JSON body.</summary>
public sealed class CapturingHandler : HttpMessageHandler
{
    public List<CapturedRequest> Requests { get; } = new();

    public string ResponseJson { get; set; } = "{\"data\":{}}";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new CapturedRequest(
            request.Method.Method,
            request.RequestUri!.AbsolutePath,
            Uri.UnescapeDataString(request.RequestUri.Query),
            body));
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ResponseJson, Encoding.UTF8, "application/json"),
        };
    }

    /// <summary>A RepullClient whose requests land in this handler.</summary>
    public RepullClient CreateClient()
    {
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: new HttpClient(this))
        {
            BaseUrl = "https://api.repull.dev",
        };
        return new RepullClient(adapter);
    }
}
