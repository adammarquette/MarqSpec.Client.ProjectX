# ADR-0010 — The order-search window is required, and the DTO rename is reverted

**Status:** Accepted · supersedes [ADR-0009](0009-search-order-window-rename.md)

## Context

[ADR-0009](0009-search-order-window-rename.md) accepted a major bump to pay for renaming two public properties:
`SearchOrderRequest.StartTime` / `.EndTime` became `StartTimestamp` / `.EndTimestamp`, following the wire names
in `swagger.json`.

Two things about that were wrong.

**The rename was not needed to fix the bug.** The defect was the *wire* names — the client sent `startTime`
while the schema requires `startTimestamp`, so the window never reached the gateway. That needs one attribute
changed. The CLR property could have stayed put:

```csharp
[JsonPropertyName("startTimestamp")]
public DateTime? StartTime { get; set; }
```

ADR-0009 argued the CLR name must track the wire name or the mismatch becomes invisible. On inspection the
opposite reads better: `IProjectXApiClient.GetOrdersAsync(accountId, startTime, endTime)` — the surface
consumers actually call — already uses `startTime`, so a DTO property named `StartTime` agrees with the public
API and disagrees only with a JSON attribute sitting two lines above it. Breaking every consumer's compile to
resolve that is a cost with no matching benefit.

**The real breaking change was somewhere else, and ADR-0009 missed it.** Renaming the fields made the window
reach the wire *when the caller supplied one*. It did nothing about the callers that supply none:

- `GetOrderAsync(accountId, orderId)` never set a window at all.
- `GetOrdersAsync(accountId)` — the natural call — passes null straight through.

`swagger.json` marks `startTimestamp` **required** on `SearchOrderRequest`. `JsonSerializerDefaults.Web` does
not drop nulls, so `"startTimestamp": null` went on the wire for a required field, and the gateway applied a
window of its own choosing. **An order outside that window is returned as absent**, and absent is
indistinguishable from "no such order".

That is the failure this repository's review contract ranks first: a caller reconciling a placement it believes
may be live reads `null` as *not placed*, and is free to place it again.

## Decision

**Revert the CLR rename. Require the window. Take the major bump for the second, not the first.**

1. `SearchOrderRequest.StartTime` and `.EndTime` keep their v1.0.5 names; only `[JsonPropertyName]` carries the
   gateway's `startTimestamp` / `endTimestamp`. **No consumer's compile breaks over a name.**
2. `GetOrdersAsync` throws `ArgumentException` when `startTime` is null, naming `startTimestamp`.
3. `GetOrderAsync(accountId, orderId)` is `[Obsolete(..., error: true)]`, replaced by
   `GetOrderAsync(accountId, orderId, startTime, endTime)`. The obsolete body throws `NotSupportedException`,
   because an assembly compiled against 1.0.x still binds to it after a package upgrade and must not silently
   keep getting wrong answers.
4. **No default window, at any layer.** Any window the client picks — 24 hours, 7 days, 30 — reproduces the
   original failure for an order just outside it, and does it invisibly. *A missing number is missing, never a
   default.*
5. The fake gateway now **rejects** a null `startTimestamp` with `400`, and no longer honours `contractId` or
   `status` as server-side filters, because the schema defines neither. A double more permissive than the venue
   is the failure mode [ADR-0007](0007-local-test-environment.md) exists to prevent.

The release stays **2.0.0**. The reason changes: not a rename, but a call that used to return a plausible wrong
answer and now refuses.

## Alternatives considered

**Keep ADR-0009's rename and ship both breaks together.** Rejected. They are not one change. Bundling a
cosmetic break with a safety break makes the changelog read as though consumers are paying for the safety fix
when half the cost is a name.

**Default the window instead of throwing** — last 24 hours, or since midnight. Rejected, and this is the whole
point of the record. It converts a loud failure into the exact quiet one being fixed, for every order older
than the default.

**Leave the windowless `GetOrderAsync` returning null.** Rejected: it is the single most dangerous method on
the surface, because `null` from it is read as "not placed".

**Make the obsolete overload a warning rather than an error.** Rejected. With `TreatWarningsAsErrors` a warning
is an error for us and merely advisory for consumers — which is backwards, since consumers are the ones who can
place a duplicate order.

**Add a windowless overload backed by `/api/Order/searchOpen`**, which needs no window. Rejected as too clever:
it would find working orders and return null for filled ones, so the method's meaning would depend on state the
caller cannot see.

## Consequences

- **Consumers upgrading from 1.0.x see no rename.** `StartTime` / `EndTime` still compile.
- **`GetOrdersAsync(accountId)` now throws**, where it previously returned an arbitrary window. Intended, and
  the fix is to pass the window that was always required.
- **`GetOrderAsync(accountId, orderId)` no longer compiles**, with a message naming the replacement. A
  pre-compiled caller gets `NotSupportedException` rather than a wrong answer.
- Callers that must ask "is this order live *right now*" and hold no sensible window should use
  `GetOpenOrdersAsync`, which takes only an account.
- The fake gateway is stricter than before, so a test that relied on its leniency fails — correctly. Three
  integration tests were passing no window where the schema always demanded one.
- ADR-0009's follow-up about `ContractId` / `Status` is resolved here: both are documented as
  serialized-and-ignored, and the fake no longer pretends otherwise.

## Follow-ups

- Ten configuration options are bound and documented but never read (`ValidateSslCertificates`, all of
  `RetryOptions`, four `WebSocketOptions` timeouts, `UseMessagePack`, `MaxBufferSize`, and
  `ProjectXOptions.WebSocketUserHubUrl` / `WebSocketMarketHubUrl`). Wire them or delete them; documenting a
  knob that turns nothing is worse than not having it.
- `WebSocketOptions.AutoReconnect` is read only to decide whether to log — `WithAutomaticReconnect` is applied
  unconditionally.
- Both test projects target `net10.0` only, so the published `net8.0` assembly has never run a test
  ([ADR-0005](0005-multi-targeting.md) calls both targets first-class).
