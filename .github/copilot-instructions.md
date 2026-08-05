# Review checklist — MarqSpec.Client.ProjectX

What to weigh when reviewing a change here. This file is the **substantive** checklist; the
[Code Reviewer contract](../documentation/agents/code-reviewer.md) owns *how* to report, and points here for
*what* to look for. It stays at this path because GitHub's Copilot reviewer reads it.

This is a transport client for a futures brokerage gateway. Code built on it places real orders with real money.
Weight findings accordingly.

## Idempotency is the one that bites

The venue offers **no idempotency key**. A retried request is a *second request*, and for order placement that
means a second live position.

- `POST /api/Order/place` must never be auto-retried. Check that a change to the resilience pipeline, the
  `ShouldHandle` predicate, or the Refit registration has not quietly pulled it back into the retry set.
- A **timeout is not a failure** — it is an unknown outcome. `HttpClient` surfaces one as a
  `TaskCanceledException` carrying its *internal* token, so a `catch (OperationCanceledException) when
  (ct.IsCancellationRequested)` filter **does not match it** and the code falls through to whatever handles a
  hard failure. If that path reports "not placed", it just lied about a possibly-live order.
- Cancel and modify carry the same question, one rung down: a retried cancel of an already-filled order is
  harmless, a retried modify is not necessarily.

## Fail-closed, not fail-open

The recurring defect shape is a permissive default.

- Prefer a **whitelist** to a blacklist. "Retry unless X" grows silently as endpoints are added; "retry only
  these" does not.
- **Zero-valued enums are permissive by accident.** An unset `OrderSide` or `OrderType` deserializes to
  whatever `0` means. Wire enums arriving from the gateway need exhaustive handling, and an unrecognized value
  is an error, not a default.
- A `catch` that swallows and returns a default is a fail-open. Say what happened, or let it propagate.

## The library decides nothing

Risk limits, sizing, eligibility and flattening belong to the consumer. A policy check added here sits **below**
trading-copilot's risk gate, where that gate cannot see or audit it. Reject a malformed request or a missing
credential; do not reject a *permitted* one on judgement.

## Secrets

- Never log the API key, the API secret, the bearer token, or a body containing them — including in exception
  messages and in `ToString()` overrides on options types.
- `AddHttpClient`'s request-header logging **redacts nothing by default**. A new typed client carrying a secret
  header needs `RedactLoggedHeaders`.
- This is a **public** repository with a history that once contained real credentials. A tracked
  `appsettings.json` with a credential-shaped key is a finding regardless of whether the value is a placeholder.

## Money and time

- Prices, quantities and tick sizes are **`decimal`**. A `float` or `double` on a price path is a finding.
- Timestamps are UTC on the wire. Do not introduce a local-time conversion in a transport client; the consumer
  owns trading-session semantics.
- Bar windows are half-open and the gateway's field names have changed before (`startTimestamp` /
  `endTimestamp` — see #15). Check a window change against `swagger.json`, not against the model.

## Conventions

- Multi-targets `net8.0;net10.0` — a change must compile clean under **both**, with warnings-as-errors.
- `CancellationToken` on every public async method, threaded all the way down.
- Enum serialization is **integers**, deliberately. `GatewaySettings` omits a string-enum converter because the
  gateway rejects strings; restoring one re-breaks #14.
- One public wire type per file under `Api/Models`, mirroring `swagger.json`.
- Fluent LINQ, never query-comprehension syntax.
- XML docs on every public member — `GenerateDocumentationFile` is on, so a gap is a build error.

## Tests

- **Test-first.** A new public method arriving without a test that failed first is a process finding.
- Bug fixes are **regression-first**: the test reproduces the bug before the fix lands.
- Unit tests mock everything and touch no network. A unit test that opens a socket belongs in the integration
  project.
- Integration tests run against the **fake gateway** and must pass with no credentials. A new test that needs
  live credentials to pass at all is a finding — it will never run in CI.
- A skip whose condition can never become false is dead weight pretending to be coverage.

## Traceability

Every PR cites its issue with a plain `Closes #N` (a backticked keyword does not bind). Behavior, API or
configuration changes update the matching `R-#` in the PRD, the architecture doc, the relevant ADR, and the
library README that ships inside the package — **in the same PR**. Stale documentation is a finding.
