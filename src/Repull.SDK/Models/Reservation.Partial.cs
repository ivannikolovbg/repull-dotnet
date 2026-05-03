namespace Repull.SDK.Models;

/// <summary>
/// Hand-written extensions to the auto-generated <see cref="Reservation"/> model.
/// As of v0.2.0 the OpenAPI spec types <c>id</c>, <c>listingId</c>, and
/// <c>guestId</c> as strings (matching the live API). <see cref="IdString"/>
/// is retained as a back-compat alias for callers migrating from v0.1.x.
/// </summary>
public partial class Reservation
{
    /// <summary>
    /// Back-compat alias for <see cref="Id"/>. Prefer <c>Id</c> directly in new code.
    /// </summary>
    public string? IdString => Id;
}
