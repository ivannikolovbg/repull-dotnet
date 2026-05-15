# Changelog

All notable changes to `Repull.SDK` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.3] - 2026-05-15

### Added
- **`listings_limit_exceeded` (402) coverage.** The API now returns `402 Payment Required` with `error.code = "listings_limit_exceeded"` when a customer is over their tier's active-listing cap (free=5, starter=50, custom=unlimited). Unlike 429, this is NOT a "wait and retry" condition — `Retry-After` is not set. Recovery paths: `DELETE` listings to fall under the cap, or upgrade at `repull.dev/dashboard/billing`. `/v1/health`, `/v1/usage/*`, and any `DELETE` are exempt. The 402 envelope mirrors `rate_limit_exceeded` and adds `tier`, `limit`, `active_listings`, `upgrade_url`. Tracks vanio-repull-api PR #66.
- **`Listing.Content` and `Listing.Details` properties** populated when caller passes `?include=content` / `?include=details`. Sourced from `listings_descriptions` (en locale) and `listings_details` respectively. Field `null` = no row stored; absent = caller did not opt in. (Catch-up regen for vanio-repull-api PRs #59 and #61, originally shipped only to other SDKs in 0.2.2.)
- **`ListingDetails` model.** New schema for the structured details payload returned by `?include=details`.

## [0.2.0] - 2026-05-03

### Changed (BREAKING)
- **All ID fields are now string-typed across the entire SDK.** The OpenAPI spec
  was tightened to match the live API: `Reservation.Id`, `Reservation.ListingId`,
  `Reservation.GuestId`, and equivalent fields on related models are now `string?`
  instead of `int?`. Update any callers that did `r.Id.HasValue` /
  `r.Id.Value.ToString()` to use `r.Id` directly. The `Reservation.IdString`
  back-compat alias is retained but deprecated — prefer `Id` in new code.
- **Pagination canonical envelope.** All list endpoints now return
  `{ data: [...], pagination: { nextCursor, hasMore, total? } }`. Required
  fields on `Pagination` are `nextCursor` (nullable string) + `hasMore`
  (boolean). `total` is present when `?include_total=true` (the default).
- **`POST /v1/connect/airbnb` response field rename**: `oauthUrl` → `url`
  on `ConnectSession`. Update callers reading the OAuth redirect URL.
- **`/v1/markets` response shape**: top-level `markets` array → `data`,
  `total_in_filter` → `total` (under the `pagination` envelope).
- **`/v1/reviews/{id}` returns a bare `Review` object** (no envelope), aligning
  with the rest of the detail endpoints.
- **`/v1/channels/airbnb/*` list endpoints now use the canonical
  `{ data, pagination }` envelope** instead of bespoke per-endpoint shapes.
- All field names are camelCase on the wire (PascalCase in C# via Kiota's
  `JsonPropertyName` attributes); verify any hand-rolled JSON code paths.

### Added
- Self-documenting `Error` shape: every error response now includes
  `error.code`, `error.message`, `error.docsUrl`, `error.support.{email,url}`,
  and per-error `requestId` for support escalation.
- Rate-limit response headers (`X-RateLimit-Limit`, `X-RateLimit-Remaining`,
  `X-RateLimit-Reset`) declared on all endpoints.
- `X-Schema` request header declared on additional detail endpoints (parity
  with the v0.1.2 list-endpoint additions).
- New detail endpoints across reservations, listings, guests, conversations,
  reviews, channels, and markets domains — see the OpenAPI spec at
  https://api.repull.dev/openapi.json for the full surface.
- Workspace API keys now have a `keyPrefix` field (`sk_live_…` / `sk_test_…`)
  for safe display in dashboards.

### Migration

```csharp
// Before (v0.1.x) — int IDs
int? rid = r.Id;
string idText = r.Id?.ToString() ?? "";

// After (v0.2.0) — string IDs
string? idText = r.Id;            // already a string
string? listingId = r.ListingId;  // also a string now
string? guestId = r.GuestId;      // also a string now

// Before — Connect Airbnb response
var oauth = session.OauthUrl;

// After
var oauth = session.Url;

// Before — Markets list
foreach (var m in resp.Markets) { /* ... */ }
int? totalCount = resp.TotalInFilter;

// After — canonical envelope
foreach (var m in resp.Data) { /* ... */ }
int? totalCount = resp.Pagination?.Total;
```

Regenerated against the latest `https://api.repull.dev/openapi.json` via Kiota 1.31.1.

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
