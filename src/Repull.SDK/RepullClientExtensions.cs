using Microsoft.Kiota.Abstractions;
using Repull.SDK.Models;
using Repull.SDK.V1.Reservations;

namespace Repull.SDK;

/// <summary>
/// Hand-written ergonomic extensions on top of the Kiota-generated client.
/// </summary>
public static class RepullClientExtensions
{
    /// <summary>
    /// List reservations with a strongly typed response. Prefer this over
    /// <c>client.V1.Reservations.GetAsReservationsGetResponseAsync()</c> when
    /// you want to iterate <see cref="Reservation"/> directly without the
    /// untyped-array round-trip imposed by the OpenAPI <c>allOf</c> shape.
    /// </summary>
    public static async Task<ReservationsPage?> ListReservationsAsync(
        this RepullClient client,
        Action<ReservationsRequestBuilder.ReservationsRequestBuilderGetQueryParameters>? configureQuery = null,
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));

        var requestInfo = client.V1.Reservations.ToGetRequestInformation(rc =>
        {
            if (configureQuery != null)
            {
                configureQuery(rc.QueryParameters);
            }
        });
        return await client.Adapter.SendAsync<ReservationsPage>(
            requestInfo,
            ReservationsPage.CreateFromDiscriminatorValue,
            errorMapping: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
