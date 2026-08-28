# Architecture Decision Records

Why the library is the way it is. **Never read the folder** — resolve the ADR number you need from the index
below and open that one.

## How these are written

Nygard form: **Context · Decision · Alternatives considered · Consequences · Follow-ups**, filename
`NNNN-slug.md`.

- Once **Accepted**, the *decision* is immutable. A later ADR **supersedes** it; nothing is rewritten in place.
- A record is a living trail: extend it with dated `## Update` entries under a `## Decision log`, oldest first.
- **`## Follow-ups` stays last.**
- Superseding is cross-linked in **both** directions in the Status column below.
- What must never change is the reasoning. Structural housekeeping that preserves every word is fine.

An ADR is warranted when a choice constrains future work, when a reasonable engineer would ask "why not the
obvious thing", or when a change would break a consumer. Routine implementation does not need one.

## Index

| ADR | Title | Status |
|---|---|---|
| [0001](0001-refit-typed-rest-client.md) | Refit-generated REST client over a hand-rolled `HttpClient` | Accepted |
| [0002](0002-resilience-and-idempotency.md) | Resilience policy, and the idempotency boundary | Accepted |
| [0003](0003-jwt-acquisition-and-cache.md) | JWT acquisition and in-memory token cache | Accepted |
| [0004](0004-hub-client-is-a-singleton.md) | The SignalR hub client is a singleton | Accepted |
| [0005](0005-multi-targeting.md) | Multi-target `net8.0` and `net10.0` | Accepted |
| [0006](0006-tag-driven-versioning.md) | The git tag is the version | Accepted |
| [0007](0007-local-test-environment.md) | A fake gateway, not a request stub, for local and CI testing | Accepted |
| [0008](0008-branch-ladder-and-governance.md) | Branch ladder, merge methods, and repo governance | Accepted |
| [0009](0009-search-order-window-rename.md) | `SearchOrderRequest` window fields are renamed, and 2.0.0 is the cost | Superseded by [0010](0010-order-search-window-is-required.md) |
| [0010](0010-order-search-window-is-required.md) | The order-search window is required, and the DTO rename is reverted | Accepted, supersedes [0009](0009-search-order-window-rename.md) |
| [0011](0011-trade-direction-is-nullable.md) | Trade direction stays on the wire numbers; absence is null | Accepted |

*Adding a record? Add its row here in the same PR, and add a routing entry in
[`../README.md`](../README.md) if the corpus shape changes.*
