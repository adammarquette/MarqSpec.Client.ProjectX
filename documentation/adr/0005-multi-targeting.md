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

## Follow-ups

None.
