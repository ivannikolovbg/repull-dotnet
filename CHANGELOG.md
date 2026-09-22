# Changelog

All notable changes to `Repull.SDK` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.13] - 2026-09-22

### Added
- **Regenerated against the live spec (191 → 199 operations).** 8 new operations, nothing removed.
- **Inquiries** — `client.V1.Inquiries.GetAsInquiriesGetResponseAsync(...)` (`GET /v1/inquiries`; `Status` defaults to `open`, `all` for every state; `ListingId`, `ConversationId`, cursor pagination).
- **Pre-approval** — `client.V1.Conversations[id].PreApproval.PostAsync(...)` (`POST /v1/conversations/{id}/pre-approval`, optional `BlockInstantBooking`).
- **Special offers** — `client.V1.Conversations[id].SpecialOffers.PostAsync(...)`, `.SpecialOffers[offerId].GetAsync()` / `.DeleteAsync()` (`POST`/`GET`/`DELETE /v1/conversations/{id}/special-offers[/{offerId}]`), plus `client.V1.Channels.Airbnb.Offers.GetAsync(q => q.QueryParameters.OfferId = ...)`.
- **Booking requests** — `client.V1.Reservations[id].Accept.PostAsync()` / `.Decline.PostAsync(body)` (`POST /v1/reservations/{id}/accept|decline`).
- **Message attachments** — `SendMessageRequest.Attachments` (1–5 `SendMessageAttachment`s by public `https://` URL) on `client.V1.Conversations[id].Messages.PostAsync`; the response carries `SentAttachment`s.
- **Webhooks** — `WebhookEventType` gains `reservation.request.created`, `reservation.request.updated`, `inquiry.created`, `inquiry.updated`; models `ReservationRequestCreatedEvent`, `ReservationRequestUpdatedEvent`, `InquiryCreatedEvent`, `InquiryUpdatedEvent`, `InquiryWebhookObject`.
- `Reservation` gains `StatusDetail` (`request_expired`) and `RespondBy`.

### Changed
- Several existing Airbnb endpoints now declare typed responses and parameters in the spec, so their builders changed: `Channels.Airbnb.Messaging[threadId].Messages.GetAsync` takes `Cursor`/`All` query parameters and returns `MessagesResponse` (the previous `MessageListResponse` shape is `GetAsMessagesGetResponseAsync`); `Messaging[threadId].Messages.PostAsync`, `Offers.PostAsync`/`DeleteAsync` and `Reservations[code].PostAsync` now return typed responses instead of `Task`; `Reservations[code].PostAsync` takes a `WithCodePostRequestBody`.

## [0.2.12] - 2026-09-18

### Added
- **Regenerated against the live spec (175 → 191 operations, 133 paths).** 16 new operations, nothing removed.
- **Airbnb listing content write surface** — full read/write control of a listing's Airbnb-side content, under `client.V1.Channels.Airbnb.Listings[id]`:
  - `BookingSettings` (`GET`/`PUT`) — instant book, advance notice, booking window, check-in/check-out windows, preparation time, and cancellation policy including `nonRefundable` and `shortStayPolicy`.
  - `Details` (`GET`/`PUT`) — property type (category + group), room type, person capacity, bedrooms/beds/bathrooms, check-in option (`category` + `instruction`), and quiet hours. Responses carry `lockedFields` — attributes Airbnb won't let this listing change.
  - `Permits` (`GET`/`PUT`) — permit answers, with a `cached`/`cached.permitData` shape for previously-submitted values.
  - `SafetyDisclosures` (`GET`/`PUT`).
  - `Photos.PatchAsync` — update photo metadata (caption, category, room) without re-uploading; `Photos.Order.PutAsync` — reorder the gallery; `Photos.Cover.PutAsync` — set the cover photo.
  - `Rooms.PutAsync` — update an existing room (beds, room type, room amenities), alongside the existing `Rooms.PostAsync` create.
  - `Amenities.PutAsync` — bulk-write amenities and accessibility amenities.
  - `Descriptions.PutAsync` — per-locale content writes now return `blockedFields` (fields Airbnb dropped as locked) distinct from a clean `[]`.
- **`AirbnbAlterationsItemRequestBuilder.Cancel.PostAsync`** — `POST /v1/channels/airbnb/alterations/{id}/cancel`.
- **`ListingsItemRequestBuilder.Pull.Airbnb.PostAsync`** — `POST /v1/listings/{id}/pull/airbnb`, optionally scoped to one connection via `AirbnbConnectionId` (`ListingPullAirbnbRequest`) when a listing carries several Airbnb connections.
- **`?include=thumbnail`** now supported on both listing lists — `GET /v1/listings` and `GET /v1/channels/airbnb/listings` — guaranteeing `thumbnailUrl` on every row, including reduced inactive ones; combine with `?include=content,thumbnail`.
- **`AirbnbConnection`** gains `SyncCategory`, `Writable`, `LockedFields`, `AccountId`, `AccountName`, `HostName`.
- **`AirbnbDataFreshness.Accounts`** (`List<AirbnbAccountFreshness>`) — per-account freshness breakdown.
- **`Reservation.CheckInTime` / `CheckOutTime`**.
- **New error code `listing_not_api_connected`** — the existing Airbnb error family now also covers the new content-write routes.
- **`AirbnbPublishResult`** — typed publish outcome (`Published`, `Sections`, `Errors` as `List<PublishSectionError>`, `LockedFields`, `Reason`) for a publish that Airbnb applies as up to eight independent, individually-failable sections.

### Changed (BREAKING)
- **`Listings.Item.Publish.Airbnb.PostAsync` return type** changed from `Models.ListingPublishResponse` to the new `Models.ListingPublishAirbnbResponse`. `ListingPublishResponse` is unchanged and still used by `Listings.Item.Publish.Booking`.
- **`Channels.Airbnb.Alterations.PostAsync`** — request body type renamed from the nested `Alterations.AlterationsPostRequestBody` to the shared `Models.AirbnbAlterationCreateRequest`, and the return type changed from `Task` (void) to `Task<AirbnbAlteration>`.
- **`Channels.Airbnb.Listings.Item.PostAsync`** (listing state action: `delete`/`push`/`publish`/`force`) return type changed from raw `Stream` to a typed composed response (`AirbnbListingLifecycleResponse` / member types) — Kiota keeps the old `PostAsync` signature as `[Obsolete]` and adds `PostAsListingsPostResponseAsync` as the preferred typed call.

## [0.2.11] - 2026-09-15

### Added
- **Regenerated against the live spec (174 → 175 operations).**
- **Bulk listing status** — `client.V1.Listings.Status.PostAsync(ListingStatusBatchRequest)` (`POST /v1/listings/status`). Activate or deactivate up to 500 listings in one all-or-nothing call; returns `ListingStatusBatchResponse` (`Active`, `Updated`, `Unchanged`).
- **Disconnect one account** — `client.V1.Connect[provider].DeleteAsync(c => c.QueryParameters.AccountId = "...")` sends the optional `accountId` query param (required when a workspace has more than one account for the provider). The account's listings are deactivated, not deleted.
- `ConnectStatus.Accounts` (`ConnectStatus_accounts`) on `GET /v1/connect/{provider}` — every Airbnb account the workspace has connected.
- New `403 listing_inactive` error response, declared on 83 operations.
- Airbnb calendar operations gain `BusySubtype`; `AirbnbPricingWriteRequest.ModelType` is now an enum.

### Changed
- **`V1.Connect[provider].DeleteAsync` now returns a typed response** (`Disconnected`, `Provider`, `AccountId`, `ListingsDeactivated`) instead of `Stream`, and its configuration lambda takes `WithProviderItemRequestBuilderDeleteQueryParameters`. Callers that consumed the raw `Stream` need updating.
- Lists default to active listings: `GET /v1/listings` accepts `status=active|inactive|archived|all`, `GET /v1/properties` accepts `status=active|inactive|all`. Inactive rows carry identity fields only; reading or writing an inactive listing returns `403 listing_inactive`.
- Airbnb calendar writes (`PUT .../pricing`, `PUT .../availability`) validate more strictly (unknown fields such as `price` are refused with `422 invalid_params`) and declare new errors: `422 airbnb_rejected`, `403 connection_reauth_required`, `429 airbnb_rate_limited`.
- Sending `accessType` to `POST /v1/connect/airbnb` now locks the consent screen to that tier; omit it to let the host choose.

### Deprecated
- Booking.com webhooks endpoints (`GET`/`POST`/`DELETE /v1/channels/booking/webhooks`) are deprecated and always return `403`.

## [0.2.10] - 2026-09-11

### Fixed
- **Regenerated against 19 schema corrections merged into the live spec.** Path/operation inventory unchanged (124 paths / 174 operations) — only shapes changed:
  - **10 fields renamed snake_case → camelCase**, matching what the live API actually serializes: `AirbnbDataFreshness.dataFreshness`/`fixUrl`/`lastSyncedAt`, `AirbnbConnectionSummary.fixUrl`, `Pagination.nextCursor`/`hasMore`, `UsageSummary`/`UsageTier` fields `monthlyRequests`, `dailyAiRequests`, `dailyAi`, `dynamicPricingListings`, `resetsAt`.
  - **3 list responses collapsed from `{data, pagination}` envelopes to bare arrays** — `BookingPropertyListResponse`, `BookingConversationListResponse`, `VrboListingListResponse` are gone; `PropertiesRequestBuilder.GetAsync`, `MessagingRequestBuilder.GetAsync`, and `ListingsRequestBuilder.GetAsync` (Vrbo) now return `List<T>` directly.
  - **4 `id` fields changed integer → string**: `AirbnbAlteration.Id`/`ReservationId`, `AirbnbConnection.Id`, `AirbnbListing.ListingId`.
  - **`Property.Latitude`/`Longitude` changed number → string.**
- `scripts/check-spec-freshness.py` now compares schema shapes (not just path/operation counts) and gates CI on drift.

## [0.2.9] - 2026-09-11

### Added
- **Four new write operations**, regenerated from the live spec (170 → 174 operations, path count unchanged — new methods on existing paths): `POST /v1/guests` (`GuestsRequestBuilder.PostAsync`, find-or-create with contact normalisation), `POST /v1/reservations` (`ReservationsRequestBuilder.PostAsync`, creates a reservation plus guest/thread/dashboard-item/calendar-block/automations fan-out), `PATCH /v1/reservations/{id}` (`ReservationsItemRequestBuilder.PatchAsync`), `POST /v1/conversations/{id}/messages` (`MessagesRequestBuilder.PostAsync`). New request/response models: `GuestCreateRequest`, `GuestCreateResponse`, `ReservationCreateRequest`, `ReservationCreateResponse`, `ReservationUpdateRequest`, `ReservationUpdateResponse`, `SendMessageRequest`, `SendMessageResponse`, plus their nested enum/sub-object types.

## [0.2.8] - 2026-09-11

### Fixed
- **Regenerated against the live spec (89 → 124 paths).** The client was badly behind: it advertised ten `/api/studio/*` operations for a different product on a different host (all 404 on `api.repull.dev`), plus four stubs that return `501`/`404` (`POST /v1/ai`, `POST /v1/channels/airbnb/sync`, `POST /v1/channels/booking/sync`, `GET /v1/channels/vrbo/listings/{id}/pricing`). All fourteen are gone from the generated client — verified zero `Studio*` types/builders and zero references to the four stub route segments. Added the 49 paths that were missing, bringing the SDK's surface in line with the live API (availability batch writes, Airbnb alterations, PMS connect-credentials endpoints, health checks, listing photos, quotes, review replies, and more — see the regenerated `V1/` request builders for the full list).

### Notes
- Regeneration required two upstream spec fixes (both landed in `vanio-repull-api`, not yet deployed as of this release): a dangling `$ref` to a nonexistent `ValidationError` response component on 14 paths, and a missing `id` path parameter declaration on `/v1/reviews/{id}/reply`. Neither changes the path set — `openapi/v1.json` in this repo is still byte-identical to what `api.repull.dev` serves today.
- Dropped `sk_test_`/sandbox mentions from the README and `RepullClientFactory.cs` doc comment — the sandbox no longer exists; use `sk_live_*`.

## [0.2.5] - 2026-06-25

### Added
- **Booking.com hosted connect flow.** `POST /v1/connect/{provider}` with `provider=booking` now accepts `redirectUrl` (no `accessType`) and returns a hosted `url` — send the user there to designate FantasticStay in their Booking.com Extranet and paste their Hotel ID. Same response shape as Airbnb (`url`, `sessionId`, `expiresAt`). The `WithProviderPostRequestBody.RedirectUrl` doc now covers both Airbnb and Booking.com.
- **`Property.Channels` array.** `GET /v1/properties` responses now include `channels` on each `Property` — the OTAs/channels the property is actively published on (e.g. `airbnb`, `booking`, `vrbo`). Empty array when the property has no active channel links.
- **`channel` query parameter on `GET /v1/properties`.** Filter to properties with an active link on a given OTA/channel. New `GetChannelQueryParameterType` enum (`Airbnb`, `Booking`, `Vrbo`); omit to include every channel.

## [0.2.4] - 2026-06-24

### Added
- **`messaging` Airbnb Connect access scope** — `POST /v1/connect/airbnb` now accepts `accessType: "messaging"` (read + send guest messages, no property management). Unlike `full_access`, the messaging scope grants read scopes plus message read/send but NOT the exclusive property-management scope, so it can coexist with another app (e.g. an existing PMS) that already holds property management on the same Airbnb account. The `WithProviderPostRequestBody_accessType` enum gains a `Messaging` member.

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
