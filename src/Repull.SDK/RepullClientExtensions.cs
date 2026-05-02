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
    /// List reservations with a strongly typed response. Thin wrapper around
    /// the Kiota-generated <c>client.V1.Reservations.GetAsync()</c> for
    /// callers that prefer the extension-method shape.
    /// </summary>
    public static Task<ReservationListResponse?> ListReservationsAsync(
        this RepullClient client,
        Action<ReservationsRequestBuilder.ReservationsRequestBuilderGetQueryParameters>? configureQuery = null,
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));

        return client.V1.Reservations.GetAsync(rc =>
        {
            if (configureQuery != null)
            {
                configureQuery(rc.QueryParameters);
            }
        }, cancellationToken);
    }
}
