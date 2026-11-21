using Microsoft.Kiota.Abstractions.Serialization;
using Repull.SDK.V1.Channels.Vrbo.Listings.Item.Fees;
using Repull.SDK.V1.Channels.Vrbo.Listings.Item.HouseRules;
using Repull.SDK.V1.Channels.Vrbo.Listings.Item.Protection;
using Repull.SDK.V1.Listings.Item.Markups;
using Repull.SDK.V1.Webhooks.Item;
using Xunit;

namespace Repull.SDK.Tests;

/// <summary>
/// Asserts the exact request each call puts on the wire (method, path, query, JSON
/// body). Regression tests for two generator defects, both fixed in the generator
/// setup (scripts/regen.sh, scripts/prepare-spec.py), not in generated code:
/// an explicit null was dropped from the body, and the object branch of an
/// object|number field (Vrbo protection damageDeposit) wrote invalid JSON.
/// </summary>
public class RequestSerializationTests
{
    private static async Task<CapturedRequest> SendAsync(Func<RepullClient, Task> call)
    {
        var handler = new CapturingHandler();
        await call(handler.CreateClient());
        return Assert.Single(handler.Requests);
    }

    private static async Task<T> FromJsonAsync<T>(string json, ParsableFactory<T> factory)
        where T : IParsable
    {
        var value = await KiotaJsonSerializer.DeserializeAsync(json, factory);
        Assert.NotNull(value);
        return value!;
    }

    // ── explicit null ─────────────────────────────────────────────────────────

    [Fact]
    public async Task VrboFees_NullPetFee_FromJson_IsSent()
    {
        var body = await FromJsonAsync("""{"cleaning": {"amount": 80}, "pets": null}""", FeesPutRequestBody.CreateFromDiscriminatorValue);
        var r = await SendAsync(c => c.V1.Channels.Vrbo.Listings["6199"].Fees.PutAsFeesPutResponseAsync(body));

        Assert.Equal(
            new CapturedRequest("PUT", "/v1/channels/vrbo/listings/6199/fees", "", """{"pets":null,"cleaning":{"amount":80}}"""),
            r);
    }

    [Fact]
    public async Task VrboFees_NullPetFee_FromInitializer_IsSent_AndUnsetFieldsAreNot()
    {
        var body = new FeesPutRequestBody
        {
            Cleaning = new FeesPutRequestBody_cleaning { Amount = 80, Type = FeesPutRequestBody_cleaning_type.PER_STAY },
            Pets = null,
        };
        var r = await SendAsync(c => c.V1.Channels.Vrbo.Listings["6199"].Fees.PutAsFeesPutResponseAsync(body, q => q.QueryParameters.VrboListingNumber = "321.4.5"));

        Assert.Equal(
            new CapturedRequest("PUT", "/v1/channels/vrbo/listings/6199/fees", "?vrboListingNumber=321.4.5", """{"pets":null,"cleaning":{"amount":80,"type":"PER_STAY"}}"""),
            r);
    }

    [Fact]
    public async Task VrboFees_WithoutNulls_IsUnchanged()
    {
        var body = await FromJsonAsync(
            """{"cleaning": {"amount": 80, "taxable": true}, "custom": [{"productCode": "PARKING", "amount": 15}]}""",
            FeesPutRequestBody.CreateFromDiscriminatorValue);
        var r = await SendAsync(c => c.V1.Channels.Vrbo.Listings["6199"].Fees.PutAsFeesPutResponseAsync(body));

        Assert.Equal(
            """{"cleaning":{"amount":80,"taxable":true},"custom":[{"amount":15,"productCode":"PARKING"}]}""",
            r.Body);
    }

    [Fact]
    public async Task ListingMarkups_NullPercent_RemovesMarkup()
    {
        var body = new MarkupsPutRequestBody { Channel = MarkupsPutRequestBody_channel.Airbnb, MarkupPercent = null };
        var r = await SendAsync(c => c.V1.Listings["42"].Markups.PutAsync(body));

        Assert.Equal(new CapturedRequest("PUT", "/v1/listings/42/markups", "", """{"markupPercent":null,"channel":"airbnb"}"""), r);
    }

    [Fact]
    public async Task WebhookPatch_NullDescription_ClearsIt()
    {
        var body = await FromJsonAsync("""{"description": null}""", WebhooksPatchRequestBody.CreateFromDiscriminatorValue);
        var r = await SendAsync(c => c.V1.Webhooks["wh_1"].PatchAsync(body));

        Assert.Equal(new CapturedRequest("PATCH", "/v1/webhooks/wh_1", "", """{"description":null}"""), r);
    }

    [Fact]
    public async Task VrboHouseRules_NestedNull_FromJson_IsSent()
    {
        var body = await FromJsonAsync(
            """{"maxGuests": 6, "pets": {"allowed": true, "max": 2, "maxWeight": null}}""",
            HouseRulesPutRequestBody.CreateFromDiscriminatorValue);
        var r = await SendAsync(c => c.V1.Channels.Vrbo.Listings["6199"].HouseRules.PutAsHouseRulesPutResponseAsync(body));

        Assert.Equal(
            new CapturedRequest("PUT", "/v1/channels/vrbo/listings/6199/house-rules", "", """{"maxGuests":6,"pets":{"maxWeight":null,"allowed":true,"max":2}}"""),
            r);
    }

    [Fact]
    public async Task Reservations_List_QueryIsUnchanged()
    {
        var handler = new CapturingHandler { ResponseJson = """{"data": []}""" };
        await handler.CreateClient().V1.Reservations.GetAsync(q =>
        {
            q.QueryParameters.CheckInAfter = new Microsoft.Kiota.Abstractions.Date(2026, 12, 1);
            q.QueryParameters.Limit = 5;
        });

        Assert.Equal(new CapturedRequest("GET", "/v1/reservations", "?checkInAfter=2026-12-01&limit=5", null), Assert.Single(handler.Requests));
    }

    // ── object | number (damageDeposit) ──────────────────────────────────────────

    [Fact]
    public async Task VrboProtection_ObjectDeposit_FromJson()
    {
        var body = await FromJsonAsync("""{"damageDeposit": {"amount": 500}}""", ProtectionPutRequestBody.CreateFromDiscriminatorValue);
        var r = await SendAsync(c => c.V1.Channels.Vrbo.Listings["6199"].Protection.PutAsProtectionPutResponseAsync(body));

        Assert.Equal(new CapturedRequest("PUT", "/v1/channels/vrbo/listings/6199/protection", "", """{"damageDeposit":{"amount":500}}"""), r);
    }

    [Fact]
    public async Task VrboProtection_ObjectDeposit_FromInitializer()
    {
        var body = new ProtectionPutRequestBody
        {
            DamageDeposit = new ProtectionPutRequestBody.ProtectionPutRequestBody_damageDeposit
            {
                ProtectionPutRequestBodyDamageDepositMember1 = new ProtectionPutRequestBody_damageDepositMember1 { Amount = 500 },
            },
        };
        var r = await SendAsync(c => c.V1.Channels.Vrbo.Listings["6199"].Protection.PutAsProtectionPutResponseAsync(body));

        Assert.Equal("""{"damageDeposit":{"amount":500}}""", r.Body);
    }

    [Fact]
    public async Task VrboProtection_NumberDeposit_FromJson()
    {
        var body = await FromJsonAsync("""{"damageDeposit": 250}""", ProtectionPutRequestBody.CreateFromDiscriminatorValue);
        var r = await SendAsync(c => c.V1.Channels.Vrbo.Listings["6199"].Protection.PutAsProtectionPutResponseAsync(body));

        Assert.Equal("""{"damageDeposit":250}""", r.Body);
    }

    [Fact]
    public async Task VrboProtection_NullDeposit_RemovesIt()
    {
        var body = await FromJsonAsync("""{"damageDeposit": null}""", ProtectionPutRequestBody.CreateFromDiscriminatorValue);
        var r = await SendAsync(c => c.V1.Channels.Vrbo.Listings["6199"].Protection.PutAsProtectionPutResponseAsync(body));

        Assert.Equal("""{"damageDeposit":null}""", r.Body);
    }

    // ── reads are unaffected ────────────────────────────────────────────────────

    [Fact]
    public async Task VrboFees_Get_ReadsResponse()
    {
        var handler = new CapturingHandler
        {
            ResponseJson = """{"data": {"cleaning": {"amount": 80, "type": "PER_STAY"}, "pets": null, "custom": []}}""",
        };
        var res = await handler.CreateClient().V1.Channels.Vrbo.Listings["6199"].Fees.GetAsFeesGetResponseAsync();

        Assert.Equal(new CapturedRequest("GET", "/v1/channels/vrbo/listings/6199/fees", "", null), Assert.Single(handler.Requests));
        Assert.Equal(80, res!.Data!.Cleaning!.Amount);
        Assert.Null(res.Data.Pets);
    }
}
