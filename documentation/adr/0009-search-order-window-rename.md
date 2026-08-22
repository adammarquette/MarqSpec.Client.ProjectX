# ADR-0009 — `SearchOrderRequest` window fields are renamed, and 2.0.0 is the cost

**Status:** Superseded by [ADR-0010](0010-order-search-window-is-required.md)

> Superseded before release. The rename this record accepted was reverted: fixing the wire names never
> required a CLR rename, and the break that genuinely warrants 2.0.0 is the required search window,
> which this record listed only as a follow-up. The reasoning below is preserved unaltered.

## Context

`POST /api/Order/search` takes a time window. The gateway's schema names those fields `startTimestamp` and
`endTimestamp`, and lists **`startTimestamp` as required**:

```json
"SearchOrderRequest": {
  "required": ["accountId", "startTimestamp"],
  "properties": {
    "accountId":      { "type": "integer", "format": "int32" },
    "startTimestamp": { "type": "string",  "format": "date-time" },
    "endTimestamp":   { "type": "string",  "format": "date-time" }
  }
}
```

Through `v1.0.5` the client sent `startTime` / `endTime` instead. Neither name exists in the schema, so the
window was **silently dropped** — the required field was absent and the two that were sent were ignored. Callers
passing a window got results for some other window, with no error to tell them so. `SearchTradeRequest`, written
later, already used the correct names; only the order search was wrong.

gh#642 corrected the wire names. Correcting them meant renaming the CLR properties too, because the JSON name
and the property name are kept in step across `Api/Models` — one public type per file, mirroring `swagger.json`.
That rename **removes two public properties**:

| v1.0.5 | Now |
|---|---|
| `SearchOrderRequest.StartTime` | `SearchOrderRequest.StartTimestamp` |
| `SearchOrderRequest.EndTime` | `SearchOrderRequest.EndTimestamp` |

Source and binary compatibility both break. `CONTRIBUTING.md` > Releases and ADR-0006 agree on what that costs:
a breaking public-surface change needs a major bump **and** this record, because consumers compile against the
assembly directly (R-10.5).

## Decision

**The next release is `v2.0.0`, and the removed properties are not reinstated.**

- No `[Obsolete]` forwarding properties. A `StartTime` that still compiles but no longer reaches the gateway is
  the failure being fixed, dressed as a migration aid — the caller sets a window and silently does not get one.
  A compile error is the only signal that cannot be ignored.
- The JSON contract follows `swagger.json`, and the CLR property follows the JSON name. That rule is what makes
  a wire-contract mismatch visible in a diff rather than discoverable in production.
- `MarqSpec.Mcp.TopstepX` is the only known consumer and does not call the order search: it is a **read-only**
  MCP server whose venue boundary forbids the order surface entirely (its ADR-0002). Its bump to 2.0.0 is
  therefore a package-pin change with no code change. Note that its pin currently reads `1.0.6` — **a version
  that was never tagged and does not exist on nuget.org**; the bump repairs that too.

## Alternatives considered

**Keep `StartTime` / `EndTime` as CLR names, change only `[JsonPropertyName]`.** Non-breaking, and rejected. The
property name would then disagree with the wire field for the one request where that disagreement has already
caused a silent data fault, and `SearchTradeRequest` next to it would keep the matching names — the asymmetry is
exactly what let the bug survive review the first time.

**Add `[Obsolete]` shims that map onto the new fields.** Rejected: see above. It preserves compilation for the
callers most likely to be broken, which is the wrong group to protect.

**Ship as 1.1.0 and treat the rename as a fix**, on the grounds that nobody could have depended on fields the
gateway ignored. Rejected: they compiled, so they were depended on. SemVer describes the *surface*, not whether
the surface worked. It also contradicts the repo's own written rule, and a rule broken once for convenience is
not a rule.

## Consequences

- **A consumer upgrading from 1.0.x gets a compile error if it searched orders by window** — intended, and the
  migration is a two-property rename.
- `GetOrdersAsync(accountId, startTime, endTime)` on `IProjectXApiClient` is unchanged: the parameter names were
  always `startTime` / `endTime` and only the DTO moved. Consumers going through the client interface, which is
  the documented path, see nothing.
- The major bump takes the package to 2.0.0 while nuget.org still shows 1.0.4 as latest — `v1.0.5` was tagged
  and GitHub-released but never published, for the packaging reason ADR-0006 removed. **2.0.0 is the first
  published release since 1.0.4** and carries the 1.0.5 work with it.
- `SearchOrderRequest.ContractId` and `.Status` remain on the type and are **not** in the gateway's schema. They
  are serialized and ignored. Left in place here rather than removed in the same breath, so this ADR records one
  decision; see follow-ups.

## Follow-ups

- Decide whether `SearchOrderRequest.ContractId` / `.Status` are removed or documented as client-side-only. They
  currently read as filters and filter nothing.
- `GetOrderAsync` and `GetOrdersAsync` still send no `startTimestamp` when the caller passes none, leaving a
  required field null. Tracked separately — the rename does not fix it.
