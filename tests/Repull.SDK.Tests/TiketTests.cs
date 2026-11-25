using Microsoft.Kiota.Abstractions.Serialization;
using Repull.SDK.Models;
using Xunit;

namespace Repull.SDK.Tests;

/// <summary>tiket.com channel: the request on the wire and the decoded response.</summary>
public class TiketTests
{
    [Fact]
    public async Task ListReservations_SendsFilters_AndDecodesRows()
    {
        var handler = new CapturingHandler
        {
            ResponseJson = """{"data":[{"reservationId":"42","tiketReservationId":"T1","paymentMethod":"Bank Transfer","status":"confirmed"}],"pagination":{"limit":50,"offset":0,"hasMore":false}}""",
        };
        var res = await handler.CreateClient().V1.Channels.Tiket.Reservations.GetAsync(q => q.QueryParameters.PropertyId = "123");

        var r = Assert.Single(handler.Requests);
        Assert.Equal(new CapturedRequest("GET", "/v1/channels/tiket/reservations", "?property_id=123", null), r);
        Assert.NotNull(res);
        var row = Assert.Single(res!.Data!);
        Assert.Equal("42", row.ReservationId);
        Assert.Equal("Bank Transfer", row.PaymentMethod);
    }

    [Fact]
    public async Task UpdateAvailability_SendsRatesWrite_AndDecodesReadBack()
    {
        var handler = new CapturingHandler
        {
            ResponseJson = """{"propertyId":"123","type":"rates","updates":1,"requests":1,"echoTokens":["e1"],"warnings":[],"applied":"verified"}""",
        };
        var body = await KiotaJsonSerializer.DeserializeAsync(
            """{"property_id":"123","type":"rates","updates":[{"roomTypeCode":"R1","ratePlanCode":"P1","start":"2026-12-01","end":"2026-12-02","price":25000000}]}""",
            TiketAvailabilityUpdateRequest.CreateFromDiscriminatorValue);
        var res = await handler.CreateClient().V1.Channels.Tiket.Availability.PutAsync(body!);

        var r = Assert.Single(handler.Requests);
        Assert.Equal("PUT", r.Method);
        Assert.Equal("/v1/channels/tiket/availability", r.Path);
        Assert.Contains("\"property_id\":\"123\"", r.Body);
        Assert.Contains("\"price\":25000000", r.Body);
        Assert.Contains("\"start\":\"2026-12-01\"", r.Body);
        Assert.Equal(TiketAriWriteResponse_applied.Verified, res!.Applied);
    }
}
