# ADR-0001 — Refit-generated REST client over a hand-rolled `HttpClient`

**Status:** Accepted

## Context

The gateway publishes a swagger document describing ~16 endpoints, almost all `POST` (only
`/api/Status/ping` is a `GET`). The client needs typed request and response models, `CancellationToken` on every
call, and a place to hang cross-cutting concerns — auth, retry, logging.

## Decision

Declare the transport as a **Refit interface** (`IProjectXRestApi`) registered with
`AddRefitClient<T>(_gatewaySettings)`, and expose a hand-written facade (`IProjectXApiClient`) on top of it.

The facade exists so the public surface is not the wire surface: it can rename, combine, validate arguments, and
translate gateway errors into `ProjectXApiException` without those concerns leaking into the generated
interface.

**`_gatewaySettings` uses `JsonSerializerDefaults.Web` without a string-enum converter**, so enums serialize as
integers.

## Alternatives considered

**Hand-rolled `HttpClient` calls.** Rejected: 16 endpoints of `PostAsJsonAsync` plus status handling is a lot of
identical code, and each hand-written call is somewhere the `CancellationToken` can be forgotten.

**A generated client from the swagger (NSwag/Kiota).** Rejected: the generated surface changes shape whenever
the upstream document does, including cosmetically, and this library is compiled against directly by
trading-copilot — an unstable public surface is a real cost. Refit keeps the interface hand-declared and
therefore reviewable.

**Refit's default serializer settings.** Rejected, and this one was a live bug: Refit's default writes enums as
camelCase **strings**, so an aggregate-bar unit went out as `"unit":"minute"`. Every enum in the gateway's
schema is integer-typed and it rejects strings, so `retrieveBars` failed with
`400 "The JSON value could not be converted to AggregateBarUnit"` on every request.

## Consequences

- Adding an endpoint is a method on the interface plus a facade method, and the models.
- **Do not add a `JsonStringEnumConverter`.** It re-breaks every enum-carrying request. The rationale is
  duplicated as an XML `<remarks>` on `_gatewaySettings` because that is where someone about to "fix" it will be
  looking.
- Refit's exception type is `ApiException`; the facade translates to `ProjectXApiException` so consumers do not
  take a Refit dependency to catch errors.
- The interface must stay faithful to `swagger.json`. When the two disagree, **the swagger wins** and the
  interface is the defect — field names have moved before (`startTimestamp`/`endTimestamp`).

## Follow-ups

None.
