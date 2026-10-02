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

    /// <summary>
    /// Price a stay and check its availability in the PMS that manages the
    /// listing, without booking anything (<c>POST /v1/reservations/quote</c>).
    /// <c>Available == false</c> is an answer, not an error: the PMS's reasons
    /// are in <c>Restrictions</c>. A listing not managed in a PMS answers
    /// <c>422 pms_not_linked</c>; a PMS without a quote API answers
    /// <c>422 pms_write_unsupported</c>.
    /// </summary>
    public static Task<ReservationQuoteResponse?> QuoteReservationAsync(
        this RepullClient client,
        ReservationQuoteRequest body,
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (body == null) throw new ArgumentNullException(nameof(body));

        return client.V1.Reservations.Quote.PostAsync(body, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Create a reservation (<c>POST /v1/reservations</c>). On a listing
    /// managed in a PMS the booking is written to the PMS: set
    /// <c>Adults</c>/<c>Children</c>, <c>TotalPrice</c>, <c>Notes</c>,
    /// <c>UnitId</c>, <c>Status</c> and <c>SendConfirmationEmail</c> as
    /// needed, and read the <c>Pms</c> block on the response.
    /// </summary>
    public static Task<ReservationCreateResponse?> CreateReservationAsync(
        this RepullClient client,
        ReservationCreateRequest body,
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (body == null) throw new ArgumentNullException(nameof(body));

        return client.V1.Reservations.PostAsync(body, cancellationToken: cancellationToken);
    }
}
