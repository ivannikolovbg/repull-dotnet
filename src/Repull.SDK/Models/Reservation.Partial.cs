namespace Repull.SDK.Models;

/// <summary>
/// Hand-written extensions to the auto-generated <see cref="Reservation"/> model.
/// The Repull API currently returns <c>id</c> as a JSON string while the OpenAPI
/// spec types it as <c>integer</c>; Kiota's strict deserializer drops the value.
/// This partial exposes a string accessor that always works regardless of which
/// shape the API returns.
/// </summary>
public partial class Reservation
{
    /// <summary>
    /// Returns the reservation id as a string, surviving the int-vs-string
    /// mismatch between the spec and the live API.
    /// </summary>
    public string? IdString
    {
        get
        {
            if (Id.HasValue) return Id.Value.ToString();
            if (AdditionalData.TryGetValue("id", out var raw)) return raw?.ToString();
            return null;
        }
    }
}
