using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Repull.SDK.Models;

/// <summary>
/// Strongly typed page of <see cref="Reservation"/> with pagination metadata.
/// Workaround for two OpenAPI/runtime mismatches:
/// 1. The OpenAPI <c>allOf</c> shape leaves the auto-generated
///    <see cref="V1.Reservations.ReservationsGetResponse"/>.<c>Data</c> as
///    <see cref="UntypedNode"/> — fields not on the <see cref="Reservation"/>
///    schema get lost on round-trip.
/// 2. The live API returns <c>id</c> and <c>totalPrice</c> as JSON strings
///    while the spec types them as <c>integer</c>/<c>number</c>; Kiota's
///    strict parser drops them.
///
/// We work around both by reading the raw JSON bytes and projecting them onto
/// <see cref="Reservation"/> with <see cref="System.Text.Json"/>, which is
/// permissive about numeric strings.
/// </summary>
public partial class ReservationsPage : IParsable
{
    /// <summary>Reservations on the current page.</summary>
    public List<Reservation> Data { get; set; } = new();

    /// <summary>Pagination metadata.</summary>
    public PaginatedResponse_pagination? Pagination { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() =>
        new Dictionary<string, Action<IParseNode>>
        {
            { "data", n =>
                {
                    var raw = n.GetObjectValue(UntypedNode.CreateFromDiscriminatorValue);
                    if (raw is UntypedArray arr)
                    {
                        Data = arr.GetValue().Select(MapToReservation).ToList();
                    }
                }
            },
            { "pagination", n => { Pagination = n.GetObjectValue(PaginatedResponse_pagination.CreateFromDiscriminatorValue); } },
        };

    /// <inheritdoc/>
    public void Serialize(ISerializationWriter writer)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        writer.WriteCollectionOfObjectValues("data", Data);
        writer.WriteObjectValue("pagination", Pagination);
    }

    /// <summary>Discriminator factory used by Kiota's deserialization pipeline.</summary>
    public static ReservationsPage CreateFromDiscriminatorValue(IParseNode parseNode) =>
        new();

    private static Reservation MapToReservation(UntypedNode node)
    {
        // Re-serialize the untyped tree to JSON, then parse with System.Text.Json
        // because it handles "stringified" numeric primitives that Kiota's strict
        // parser drops. Cheaper than threading a second parser through the SDK.
        using var ms = new MemoryStream();
        var writerFactory = new Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory();
        using var w = writerFactory.GetSerializationWriter("application/json");
        w.WriteObjectValue(string.Empty, node);
        var json = System.Text.Encoding.UTF8.GetString((w.GetSerializedContent() as MemoryStream)!.ToArray());

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var r = new Reservation();

        if (TryGetString(root, "id", out var idStr))
        {
            r.AdditionalData["id"] = idStr!;
            if (int.TryParse(idStr, out var idInt)) r.Id = idInt;
        }
        if (root.TryGetProperty("checkIn", out var ci) && ci.ValueKind == JsonValueKind.String)
        {
            if (DateOnly.TryParse(ci.GetString(), out var d)) r.CheckIn = new Date(d.Year, d.Month, d.Day);
        }
        if (root.TryGetProperty("checkOut", out var co) && co.ValueKind == JsonValueKind.String)
        {
            if (DateOnly.TryParse(co.GetString(), out var d)) r.CheckOut = new Date(d.Year, d.Month, d.Day);
        }
        if (TryGetString(root, "confirmationCode", out var cc)) r.ConfirmationCode = cc;
        if (TryGetString(root, "currency", out var cur)) r.Currency = cur;
        if (TryGetString(root, "guestEmail", out var ge)) r.GuestEmail = ge;
        if (TryGetString(root, "guestFirstName", out var gf)) r.GuestFirstName = gf;
        if (TryGetString(root, "guestLastName", out var gl)) r.GuestLastName = gl;
        if (TryGetString(root, "guestPhone", out var gp)) r.GuestPhone = gp;
        if (TryGetString(root, "provider", out var pr)) r.Provider = pr;
        if (TryGetString(root, "platform", out var plat) && Enum.TryParse<Reservation_platform>(plat, true, out var pe)) r.Platform = pe;
        if (TryGetString(root, "status", out var st) && Enum.TryParse<Reservation_status>(st, true, out var se)) r.Status = se;
        if (TryGetInt(root, "guestCount", out var gc)) r.GuestCount = gc;
        if (TryGetInt(root, "propertyId", out var pid)) r.PropertyId = pid;
        if (TryGetDouble(root, "totalPrice", out var tp)) r.TotalPrice = tp;

        return r;
    }

    private static bool TryGetString(JsonElement root, string name, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(name, out var el)) return false;
        if (el.ValueKind == JsonValueKind.Null || el.ValueKind == JsonValueKind.Undefined) return false;
        value = el.ValueKind == JsonValueKind.String ? el.GetString() : el.GetRawText();
        return value != null;
    }

    private static bool TryGetInt(JsonElement root, string name, out int value)
    {
        value = 0;
        if (!root.TryGetProperty(name, out var el)) return false;
        if (el.ValueKind == JsonValueKind.Number) return el.TryGetInt32(out value);
        if (el.ValueKind == JsonValueKind.String) return int.TryParse(el.GetString(), out value);
        return false;
    }

    private static bool TryGetDouble(JsonElement root, string name, out double value)
    {
        value = 0;
        if (!root.TryGetProperty(name, out var el)) return false;
        if (el.ValueKind == JsonValueKind.Number) return el.TryGetDouble(out value);
        if (el.ValueKind == JsonValueKind.String) return double.TryParse(el.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);
        return false;
    }
}
