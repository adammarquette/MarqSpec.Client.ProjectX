# PRD — MarqSpec.Client.ProjectX

A .NET client library for the ProjectX gateway: REST over Refit, real-time over SignalR. It is consumed by
[trading-copilot](https://github.com/adammarquette/trading-copilot) and published to nuget.org.

**Target audience:** .NET developers building trading applications and market-data integrations.

**Requirement ids are stable.** `R-#` is this repo's symbol table — cite one from a PR, an ADR, a test name or an
XML doc comment, and resolve it here. Requirements are **appended, never renumbered**; a requirement that no
longer holds is marked **Withdrawn** with a pointer to what replaced it, so an old citation still resolves.

---

## R-1 — Configurable, secure authentication

Credentials are supplied by configuration, never compiled in.

- **R-1.1** Credentials bind through the Options pattern from the `ProjectX` configuration section.
- **R-1.2** Credentials can be supplied by environment variable. The canonical form is the ASP.NET
  double-underscore convention — `ProjectX__ApiKey`, `ProjectX__ApiSecret` — so a standard configuration host
  picks them up with no bespoke code. The legacy flat names `PROJECTX_API_KEY` / `PROJECTX_API_SECRET` remain
  supported for compatibility.
- **R-1.3** Credentials are **never** logged, and never appear in an exception message, a `ToString()`, or a
  logged request header.
- **R-1.4** A missing or malformed credential fails **at DI resolution**, not at first call — a client that
  constructs successfully and then fails on the first order is worse than one that refuses to start.
- **R-1.5** Authentication failures surface a clear, actionable error distinguishable from a transport failure.

> Naming trap, preserved because the gateway defines it: `ApiKey` is sent as the gateway's `userName` field and
> `ApiSecret` as its `apiKey` field — so the wire field called `apiKey` carries the operator's *secret*.
> See [ADR-0003](adr/0003-jwt-acquisition-and-cache.md).

## R-2 — Market data reads

- **R-2.1** Contract search, search-by-id, and available-contract listing.
- **R-2.2** Historical aggregate bars over a bounded window, with the unit and window supplied by the caller.
- **R-2.3** Responses deserialize into typed models under `Api/Models`, one public type per file.
- **R-2.4** Gateway errors surface as `ProjectXApiException` carrying the HTTP status, not as a raw
  `HttpRequestException`.

## R-3 — Order management

- **R-3.1** Place, modify, cancel, search, and search-open.
- **R-3.2** Responses deserialize into typed models.
- **R-3.3** **`POST /api/Order/place` is never automatically retried.** The gateway offers no idempotency key, so
  a retried request is a second request, and for placement that is a second live order. See
  [ADR-0002](adr/0002-resilience-and-idempotency.md).
- **R-3.4** A timeout or cancellation on an order-placing call is reported as an **unknown outcome**, never as a
  failure. The caller must be able to tell "did not reach the venue" from "may be live".

## R-4 — Position and trade reads

- **R-4.1** Search open positions; close and partially close a position.
- **R-4.2** Trade search over a bounded window.
- **R-4.3** Account search, with an active-only filter.

## R-5 — Real-time streaming

- **R-5.1** Connect to both hubs — user (`/hubs/user`) and market (`/hubs/market`).
- **R-5.2** Surface account, order, position, trade, price, order-book and trade-notification updates as .NET
  events (observer pattern), deserialized into typed models.
- **R-5.3** **Reconnect automatically within 5 seconds** of a disconnection, re-acquiring a fresh token on each
  attempt rather than replaying an expired one. After the new connection id is established, restore every
  recorded hub subscription **before** reporting `Connected`. A restore failure is raised on
  `MessageSendFailed` (R-5.5) and the hub is reported `Failed`, not `Connected` (gh#87).
- **R-5.4** Sustain **1000 events/second per stream** without message loss or degradation.
- **R-5.5** A message that fails to send is reported to observers rather than swallowed
  (`MessageSendFailed`).
- **R-5.6** Report connection-state transitions so a consumer can distinguish connected, reconnecting and
  faulted.
- **R-5.7** Market-hub events (`GatewayQuote`, `GatewayDepth`, `GatewayTrade`) surface the hub
  `contractId` on the raised update. The payload symbol is a product root; two expiries of one root
  must remain distinguishable (gh#86).
- **R-5.8** An absent or unrecognised `TradeLogType` is representable (`null`) and is never coerced to
  `Buy`. The live wire keeps `0 = Buy`, `1 = Sell` (gh#86).

## R-6 — Resilience

- **R-6.1** Transient faults — network errors, 5xx, 429 — are retried with exponential backoff, capped at 3
  attempts, **on idempotent endpoints only** (R-3.3).
- **R-6.2** A 429 honours the `Retry-After` header in **both** its delta-seconds and its HTTP-date forms.
- **R-6.3** The client degrades gracefully under partial gateway outage.

## R-7 — Library surface and ergonomics

- **R-7.1** A single registration extension — `AddProjectXApiClient(IConfiguration)` — is the whole public
  registration surface.
- **R-7.2** Every public async method accepts a `CancellationToken`, threaded to the transport.
- **R-7.3** The client is thread-safe and supports concurrent calls.
- **R-7.4** Logging goes through `ILogger<T>` so the consumer chooses the provider.
- **R-7.5** Every public type and member carries XML documentation matching the gateway's swagger description.
- **R-7.6** New endpoints can be added without breaking existing consumers.

## R-8 — The client decides nothing

The library is a transport. Risk limits, sizing, eligibility, and the decision to flatten belong to the
consumer.

- **R-8.1** No risk limit, position cap, or trading-policy rule is held or enforced here.
- **R-8.2** Refusals are transport-level only — a malformed request, an absent credential. A *permitted* request
  is never refused on judgement.
- **R-8.3** No trading-session or wall-clock semantics. Timestamps are UTC on the wire; the consumer owns
  session interpretation.

> Rationale: an enforcement point here would sit **below** trading-copilot's risk gate, in a different
> repository, where that gate can neither see nor audit it.

## R-9 — Security

- **R-9.1** HTTPS for all REST calls; WSS for all hub connections.
- **R-9.2** SSL certificates validated by default.
- **R-9.3** No credential-shaped value is tracked in source control. This is a **public** repository.

## R-10 — Distribution

- **R-10.1** Published to nuget.org as `MarqSpec.Client.ProjectX`.
- **R-10.2** Multi-targets `net8.0` and `net10.0`.
- **R-10.3** Ships a **symbol package** (`.snupkg`) and a deterministic, SourceLink-enabled build.
- **R-10.4** Versioned per **SemVer 2.0**, derived from the git tag — no version is declared in a file. See
  [ADR-0006](adr/0006-tag-driven-versioning.md).
- **R-10.5** A breaking change to the public surface requires a major bump **and** an ADR, because
  trading-copilot compiles against this assembly directly.

## R-11 — Verification

- **R-11.1** Unit tests cover every public method, fully mocked, with **no I/O**. Target ≥ 95% line and ≥ 90%
  branch coverage.
- **R-11.2** Integration tests cover every public method and **run with no credentials**, against the local
  fake gateway. See [ADR-0007](adr/0007-local-test-environment.md).
- **R-11.3** Live-gateway tests are opt-in, separately tagged, and never required for a green build.
- **R-11.4** Analyzer warnings stay at zero — enforced by `TreatWarningsAsErrors`, not by asking.
- **R-11.5** A test's skip condition must be able to become false. A permanently-skipped test is not coverage.

## R-12 — Code organisation

- **R-12.1** One public class, struct or enum per file.
- **R-12.2** SOLID; constructor injection; immutability by default.
- **R-12.3** Money, prices and tick sizes are `decimal` — never `float` or `double`.
- **R-12.4** Wire enums serialize as **integers**. The gateway rejects string enums; `_gatewaySettings`
  deliberately omits a string-enum converter.

## R-13 — Performance and stability

- **R-13.1** REST latency p95 < 500 ms, p99 < 1000 ms under normal network conditions.
- **R-13.2** Hub delivery latency p99 < 100 ms from server send to callback invocation.
- **R-13.3** No memory growth across 24 hours of continuous hub operation.
- **R-13.4** Gateway success rate > 99.9% excluding 4xx client errors.

---

## External interfaces

| Interface | Endpoint | Notes |
|---|---|---|
| REST | `https://api.topstepx.com` | Contract mirrored in `swagger.json` at the repo root — **when a model and the swagger disagree, the swagger wins** |
| Auth | `POST /api/Auth/loginKey` | Returns a JWT; subsequent calls use `Authorization: Bearer` |
| User hub | `https://rtc.topstepx.com/hubs/user` | SignalR Core, JSON protocol |
| Market hub | `https://rtc.topstepx.com/hubs/market` | SignalR Core, JSON protocol |

**Rate limits** (drive R-6): `POST` 50 requests / 30 s — primarily order operations; `GET`/`DELETE` 200 requests
/ 60 s. A 429 carries `Retry-After`.

## Technology stack

`Refit` + `Refit.HttpClientFactory` · `Microsoft.Extensions.Http.Resilience` + `Polly.Core` ·
`Microsoft.AspNetCore.SignalR.Client` · `Microsoft.Extensions.Options` / `.Logging.Abstractions`.
Tests: `xUnit` · `FakeItEasy` · `FluentAssertions` · `coverlet`.

Versions are **not** listed here — they live in `Directory.Packages.props`, and a version in two places is a
version that drifts.

## Out of scope

UI components or visualization · backtesting and historical analysis · execution algorithms and trading
strategies · multi-account orchestration · **any risk, sizing or policy decision** (R-8).

---

## Change log

| Date | Change |
|---|---|
| 2026-08-28 | Added R-5.7 (hub `contractId` on market events) and R-5.8 (trade direction is nullable; wire `0` stays Buy). |
| 2026-08-05 | Restructured from the root `PRD.md` into stable `R-#` ids. Added R-8 (the client decides nothing) and R-3.3/R-3.4 (idempotency and unknown outcomes), which were established practice and merged fixes but had never been written as requirements. Moved dependency versions out to `Directory.Packages.props`. |
