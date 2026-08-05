# ADR-0002 — Resilience policy, and the idempotency boundary

**Status:** Accepted

## Context

The gateway publishes rate limits — `POST` 50 requests / 30 s, `GET`/`DELETE` 200 / 60 s — and returns `429`
with a `Retry-After` header when they are exceeded (PRD external interfaces). Transient `5xx` and network faults
also occur. A client that surfaces every transient blip to the caller is unusable for an execution system, so
the library retries.

**But the gateway offers no idempotency key.** There is no request id a client can send to say "if you already
did this, don't do it again". That makes retrying a *semantic* decision, not a transport one, and the semantics
differ per endpoint:

- A retried **search** returns the same rows. Harmless.
- A retried **cancel** of an already-cancelled order is a no-op. Harmless.
- A retried **place** is a **second live order**. Not harmless — it doubles a position with real money behind
  it.

The dangerous case is not an obvious failure. It is the ambiguous one: the first attempt reaches the venue and
books the order, and then the acknowledgement is lost to a transport fault, or a `5xx` arrives *after*
acceptance. To the client those are indistinguishable from "never arrived". A blanket retry policy resolves that
ambiguity in the most expensive possible direction.

This shipped wrong first. The original pipeline applied `AddStandardResilienceHandler` uniformly to every
endpoint, including `POST /api/Order/place`.

## Decision

**Retry transient faults on idempotent endpoints only. `POST /api/Order/place` is excluded, permanently.**

- The retry predicate lives in `ServiceCollectionExtensions.ShouldRetryOutcome`, **factored out of the
  registration lambda** so it can be unit-tested against real request and outcome shapes rather than only
  through an integration test.
- Retry on `HttpRequestException`, `429`, or `>= 500`; 3 attempts, 1 s base, exponential, 30 s cap.
- `429` honours `Retry-After` in **both** encodings the header permits: `Delta` (delta-seconds) and `Date` (an
  HTTP-date, converted against `UtcNow` and ignored if it lands in the past). Anything unusable falls through
  to exponential backoff.
- **The guard is asymmetric on purpose.** `IsOrderPlacement(null)` returns `false`, so an unidentifiable request
  is treated as *not* a placement and keeps the standard retry. The guard can therefore only ever **remove**
  retries from a confirmed placement — it can never add double-place risk relative to the pipeline without it.
- A fault on a placement surfaces to the caller as an **indeterminate outcome**, not a failure. The consumer
  owns reconciliation (R-3.4, R-8).

Adding an endpoint to the retry set requires stating, in the PR, why resending it is safe.

## Alternatives considered

**Retry everything, and let the consumer deduplicate.** Rejected: the consumer cannot deduplicate what it cannot
see. By the time a duplicate fill arrives, the position is already wrong.

**Retry placement but reconcile first — re-read open orders before resending.** Rejected for now: the read is
itself subject to propagation delay, so a just-booked order may not appear yet, and the client would resend into
a false negative. This belongs in the consumer, which has durable intent to compare against; see Follow-ups.

**Client-generated idempotency key.** The right answer, and not available — the gateway does not accept one. If
it ever does, this ADR is superseded rather than amended.

**Retry only on `429`, never on `5xx`.** Rejected: too conservative for reads, and it does not help placement,
which is the only case that actually matters.

## Consequences

- An order placement that hits a transient fault fails on the first attempt. That is intended: a visible failure
  the consumer must resolve beats a silent duplicate.
- The consumer must implement reconciliation. trading-copilot does; any other consumer inherits the obligation,
  which is why R-3.4 states it as a requirement rather than leaving it implied.
- The retry predicate is now unit-testable, and is covered directly rather than through the pipeline.
- **The handler order is now load-bearing.** `AuthenticationHandler` is registered before
  `AddStandardResilienceHandler`, so auth is outermost and the retry loop sits inside it. Reordering them
  changes what the guard sees. See [architecture](../projectx-client-architecture.md#the-request-pipeline).

## Follow-ups

- A timeout is an `OperationCanceledException`/`TaskCanceledException` carrying **`HttpClient`'s internal
  token**, not the caller's. Any consumer-side `when (ct.IsCancellationRequested)` filter therefore misses it
  and falls through to the hard-failure path — which then reports "not placed" for a possibly-live order. That
  is a consumer-side trap; it is recorded in
  [`.github/copilot-instructions.md`](../../.github/copilot-instructions.md) so review catches it.
- Cancel and modify are currently retried. Cancel is safely idempotent; **modify is less obviously so** and has
  not been analysed. Worth a follow-up issue.
