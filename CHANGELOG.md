# Changelog

All notable changes to `Repull.SDK` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.2] - 2026-05-02

### Added
- **Custom field-mapping schemas** — 5 new endpoints under `/v1/schema/custom`:
  - `POST /v1/schema/custom` — create a workspace-scoped schema
  - `GET /v1/schema/custom` — list schemas
  - `GET /v1/schema/custom/{id}` — fetch a single schema
  - `PATCH /v1/schema/custom/{id}` — update a schema
  - `DELETE /v1/schema/custom/{id}` — hard-delete a schema
- **8 new model types** generated from the spec:
  `CustomSchema`, `CustomSchemaSummary`, `CustomSchemaCreate`, `CustomSchemaCreateResponse`,
  `CustomSchemaUpdate`, `CustomSchemaListResponse`, `CustomSchemaDeleteResponse`,
  `CustomSchemaMappings`.
- **`X-Schema` request header** is now declared on all read endpoints
  (`reservations`, `guests`, `conversations`, `reviews`, `listings`).
  Pass `X-Schema: <name>` to reshape the response payload to a custom schema.

### Changed (BREAKING)
- **`Reservation` shape aligned with the live API** — list-row and detail responses
  now return the same shape, and the SDK type matches:
  - Removed: `GuestFirstName`, `GuestLastName`, `GuestEmail`, `GuestPhone`, `GuestCount`,
    `PropertyId`, `Provider`.
  - Added: `GuestDetails` (raw per-channel guest payload), `GuestId` (Repull guest ID),
    `GuestName` (pre-resolved display name), `ListingId`, `CreatedAt`.
  - `TotalPrice` is now `string` (decimal-as-string, precision 10/scale 2) instead of `double?`,
    to preserve precision across mixed-currency totals.
  - `Platform` documented as nullable on legacy rows.
- Regenerated against the latest `https://api.repull.dev/openapi.json` via Kiota 1.31.1.

### Migration

```csharp
// Before (v0.1.1)
var firstName = r.GuestFirstName;
var email     = r.GuestEmail;
var price     = r.TotalPrice; // double?

// After (v0.1.2)
var displayName = r.GuestName;                      // pre-resolved "First Last"
var firstName   = r.GuestDetails?.FirstName;        // raw channel payload
var email       = r.GuestDetails?.Email;
var price       = r.TotalPrice;                     // string ("123.45")
var asDecimal   = decimal.Parse(r.TotalPrice ?? "0",
                      System.Globalization.CultureInfo.InvariantCulture);
```

## [0.1.1] - 2026-04-XX

- Conversations, Guests, Reviews surfaces.
- Cursor-paginated reservations.

## [0.1.0] - 2026-04-XX

- Initial public release.
