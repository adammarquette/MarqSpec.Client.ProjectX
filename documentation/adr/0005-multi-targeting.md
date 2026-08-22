# ADR-0005 — Multi-target `net8.0` and `net10.0`

**Status:** Accepted

## Context

The PRD originally asked for ".NET 10 or later", and noted under non-functional requirements that
multi-targeting should be considered "for broader adoption". trading-copilot is on `net10.0`. But this is a
published NuGet package, and a package that requires the newest LTS excludes every consumer who has not moved
yet.

## Decision

Multi-target **`net8.0;net10.0`** — the two supported LTS releases.

The library holds no `net10.0`-only API dependency: `Refit`, `Microsoft.Extensions.*`, and
`Microsoft.AspNetCore.SignalR.Client` all support `net8.0`. So the second target costs a build, not a code path.

**Both targets are first-class.** A change must compile clean under both, with warnings-as-errors, and CI builds
both. There are no `#if NET10_0_OR_GREATER` blocks today, and adding one needs a reason in the PR — divergent
behaviour per target framework is the cost this decision is trying not to pay.

## Alternatives considered

**`net10.0` only.** Rejected: excludes `net8.0` consumers for no benefit, since nothing here needs .NET 10.

**`netstandard2.0` as well.** Rejected: `Microsoft.Extensions.Http.Resilience` and the modern SignalR client do
not target it, and supporting it would mean a materially different resilience implementation for a consumer
base that does not exist.

**Add `net9.0`.** Rejected: STS releases go out of support quickly and each target multiplies the build and test
matrix.

## Consequences

- Build and pack produce two lib folders; consumers get the right one automatically.
- **CodeQL currently builds with only the 10.0 SDK** while the library targets both. This works via the 10 SDK's
  targeting packs but disagrees with `ci.yml`, which installs both. Reconciled in the CI work.
- Adding a dependency requires checking it supports `net8.0`, not just that it restores.
- When `net8.0` leaves support, dropping it is a **major** version bump under R-10.5, recorded by superseding
  this ADR.

## Decision log

### Update 2026-08-22 — the targets are now *tested*, not just built

"Both targets are first-class" was true of the build and false of the tests. Both test projects targeted
`net10.0` only, so `lib/net8.0/MarqSpec.Client.ProjectX.dll` shipped in `2.0.0` having **never executed a single
test** (gh#66). Compiling is not the same as running: `System.Text.Json` defaults, `SocketsHttpHandler` /
`PooledConnectionLifetime` — which the singleton auth registration now relies on — and the SignalR client all
behave against a different runtime per target.

`MarqSpec.Client.ProjectX.Tests` now targets `net8.0;net10.0`, so the unit suite runs twice, once per runtime.

**The integration tier deliberately stays `net10.0`-only.** It project-references `FakeGateway`, an ASP.NET Core
app, and a `net8.0` test project cannot reference a `net10.0` one — so the fake would have to multi-target too.
That builds, but running the `net8.0` leg needs the **`Microsoft.AspNetCore.App 8.x` shared framework**, which
neither a typical developer machine nor the pinned `mcr.microsoft.com/dotnet/sdk:10.0` dev container has. It
would pass in CI and fail for everyone locally, and `docker-compose.dev.yml` exists to state the opposite
principle: *a local check that disagrees with CI is worse than no local check.*

Buying `net8.0` transport coverage at the price of a `dotnet test` that no longer runs locally is the wrong
trade at this size. The unit tier is where the library's own logic lives, and it now covers both runtimes.

## Follow-ups

- Cover the `net8.0` transport path in the integration tier without breaking local `dotnet test` — most likely
  by giving the dev container both shared frameworks, then multi-targeting `FakeGateway` and the integration
  project together. Tracked in gh#67.
