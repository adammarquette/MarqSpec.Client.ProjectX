# ADR-0007 — A fake gateway, not a request stub, for local and CI testing

**Status:** Accepted

## Context

The integration tests hit **`https://api.topstepx.com` — live production** — from inside the *unit-test*
project. There is no sandbox base URL, no stub, no container.

That produced a compounding set of problems:

- A plain `dotnet test` reaches for the network.
- Tests need real credentials, so **they never run in CI**. `Integration/README.md` said so explicitly.
- `IntegrationTestBase` exposes a `SkipReason` that reports missing credentials — but it is **not wired to the
  `[Fact(Skip = …)]` attributes**, which carry hardcoded strings. **22 of 43 integration facts are permanently
  skipped whether or not credentials exist**, which is coverage that looks present and is not (R-11.5).
- `run-integration-tests.ps1` exists to work around that, and its documented job is to **rewrite source files**
  to strip `Skip` attributes.
- Because the library ships no test double, **trading-copilot had to build its own** —
  `TestHost/Staging/StagingProjectXGateway.cs`. The consumer paid for the gap.

Any test that places an order against production is also placing an order.

## Decision

Build an in-repo **`MarqSpec.Client.ProjectX.FakeGateway`** — an ASP.NET Core application that stands in for the
venue — and run the integration tier against it via `docker compose`.

It serves:

- The **REST surface** described by the committed `swagger.json`.
- **Both SignalR hubs** — `/hubs/user` and `/hubs/market`.
- A **real signed JWT** from `loginKey`, so the token cache, the 55-minute expiry path, the refresh buffer and
  `AccessTokenProvider`-on-reconnect are genuinely exercised rather than stubbed past.
- A **`POST /_control/…`** surface for deterministic scenarios: emit a fill, push a price, force a `429`
  carrying `Retry-After` in either encoding, force a `5xx`, stall a request.

Integration tests point `ProjectXOptions.BaseUrl` at it and **pass with no credentials anywhere**. Live-gateway
tests remain, tagged `Category=Live`, opt-in, never required for a green build.

## Alternatives considered

**WireMock.Net with recorded mappings.** The obvious cheaper option, and rejected on one point:
**it cannot serve SignalR.** `IProjectXWebSocketClient` is 30 KB of the library and is its *most-used type in
trading-copilot* — injected into `ProjectXConnection`, `ProjectXAccountEventStream` and `ProjectXVenue`. A test
environment that cannot exercise the hubs leaves the highest-value surface untested, which is the situation
being fixed.

**Keep hitting production, but only reads.** Rejected: it still needs credentials, so it still cannot run in
CI, and the read/write boundary is one careless test away from being crossed.

**A recorded-cassette approach (VCR-style).** Rejected: cassettes go stale silently and cannot model the
scenarios that matter here — a 429 with a specific `Retry-After`, a fill arriving on the user hub *after* a
REST placement returns.

**A shared hosted staging instance.** Rejected: it is infrastructure to operate, it serializes parallel test
runs, and the gateway offers no such thing anyway.

## Consequences

- The integration suite becomes CI-runnable and credential-free, which is what makes it a **gate** rather than a
  manual ritual.
- The fake is a second implementation of the gateway contract and **can drift from it**. Mitigation: it is
  generated against and checked against the committed `swagger.json`, and the swagger remains authoritative
  when they disagree.
- **A test passing against the fake does not prove the real gateway agrees.** That is what the `Category=Live`
  tier is for; it is not deleted, it is demoted from "the only tier" to "the confirmation tier".
- `run-integration-tests.ps1` is deleted. Nothing rewrites source to run tests.
- trading-copilot's hand-rolled `StagingProjectXGateway` becomes redundant, and can eventually consume this
  instead.
- A `docker compose` dependency appears in the developer loop. It also brings a benefit: a `dotnet/sdk` service
  gives a reproducible toolchain, which sidesteps Windows intermittently blocking freshly built unsigned
  assemblies.

## Follow-ups

- Consider publishing the fake gateway as its own package so trading-copilot can retire
  `StagingProjectXGateway` rather than reimplementing it.
- A contract test that asserts the fake's routes against `swagger.json`, so drift fails a build instead of
  being discovered by a live test.
