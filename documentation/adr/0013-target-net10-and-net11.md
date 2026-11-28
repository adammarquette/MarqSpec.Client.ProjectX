# ADR-0013 — Target `net10.0` and `net11.0`; drop `net8.0`

**Status:** Accepted (2026-11-28, gh#119). Supersedes [ADR-0005](0005-multi-targeting.md).

## Context

ADR-0005 multi-targeted `net8.0;net10.0`, and said that dropping `net8.0` when it left support would be a
**major** bump recorded by superseding it. Two things happened in November 2026:

- **.NET 8 left support on 2026-11-10**, and **.NET 11.0.0 went GA** the same day (SDK 11.0.100).
- **The .NET 11 package line has no `net8.0` build.** `Microsoft.Extensions.Options` 11.0.0 ships `net10.0`,
  `net11.0`, `netstandard2.0/2.1` and `net462`. `Microsoft.AspNetCore.SignalR.Client` 11.0.0 ships `net11.0`,
  `netstandard2.0` and `net462` only.
- **The Dependabot bumps could not go green under `net8.0`.** gh#112 / gh#115 (`Microsoft.Extensions.*` 11) hung
  the `net8.0` test host ("Failed to negotiate protocol … timed out after 90 seconds") while `net10.0` passed.
  gh#114 / gh#117 (SignalR.Client 11) needed the whole extensions family at 11 to restore at all (NU1109).

The maintainer ruled: *"Drop net8.0 and take the .NET 11 bumps."*

## Decision

**Multi-target `net10.0;net11.0`** and depend on the .NET 11 package line on both targets.

- **`net10.0` stays.** It is the current LTS, and the consumers run on it: trading-copilot, the
  futures-liquidity dashboard and the TopstepX MCP. Dropping it would strand every one of them.
- **`net11.0` is added** so the package has a first-class build for the runtime its dependencies are built for.
- **Both targets are first-class**, as before. A change compiles clean under both, with warnings-as-errors. There
  are no `#if NET11_0_OR_GREATER` blocks, and adding one needs a reason in the PR.
- **Tests: every tier runs on both targets.** The unit suite, the integration suite and FakeGateway all target
  `net10.0;net11.0`.
  - `net10.0` is the consumers' runtime, and it is the target where SignalR.Client resolves its `netstandard2.0`
    asset (see *Consequences*), the binding most worth exercising over a real socket.
  - `net11.0` is the target the dependencies are built for.
  - ADR-0005 left the integration tier single-target because the dev container lacked the older shared
    framework. `sdk:11.0` plus the 10.0 layer below has both, so that gap is closed rather than carried forward.
- **Toolchain:**
  - `global.json` pins SDK **11.0.100**.
  - CI, CodeQL and release install `10.0.x` and `11.0.x`: the 11.0 SDK builds both targets, and the 10.0 install
    supplies the runtime the `net10.0` legs execute on.
  - The dev container is `sdk:11.0` with the 10.0 shared frameworks layered in from `aspnet:10.0`.
  - The FakeGateway image builds on `sdk:11.0` and publishes its `net10.0` build onto `aspnet:10.0`.

## Consequences

- **A major version: 4.0.0** (R-10.5). A `net8.0` consumer stays on 3.x.
- **`Microsoft.Extensions.*` 11.x flows onto `net10.0` consumers transitively**, over their 10.x shared
  framework. NuGet lifts those assemblies app-local, which works: the integration tier and FakeGateway are an
  ASP.NET Core 10 app on exactly that shape. But a consumer that centrally pins the family at 10.x gets NU1109 and
  must move the pin to 11.x. This is the cost of the decision, and the release notes say so.
- **On `net10.0`, SignalR.Client binds its `netstandard2.0` build.** That build is a supported surface, but it is
  not the `net10.0`-optimised one the 10.x package shipped. The integration tier runs on `net10.0` as well as
  `net11.0`, so the hub path consumers actually load is the one under test.
- **A machine with only the 10.0 SDK can no longer build the solution.** CONTRIBUTING states the prerequisite:
  the 11.0 SDK plus the 10.0 runtimes. The dev container carries both.
- **`net11.0` is an STS release.** When it leaves support, the next decision (move to `net12.0`, or back to an
  LTS-only pair) is a new ADR, not an edit to this one.
- gh#67 (integration coverage of `net8.0`) is moot. Its underlying point, that every shipped target runs the
  integration tier, is met for `net10.0` and `net11.0`.

## Alternatives considered

**Stay on the 10.x package line with a Dependabot ignore rule for the majors.** This was recommended, because
it avoids pushing 11.x onto `net10.0` consumers. It was declined by the maintainer, and it leaves the package on
a dependency line that stops receiving feature work.

**`net11.0` only.** Rejected: it strands every current consumer, all of which are on `net10.0`.

**Keep `net8.0` and suppress the unsupported-TFM fallback.** Rejected: `net8.0` is out of support, and the
11.x packages do not ship a build for it, so the shipped `net8.0` assembly would run against a fallback no one
tests. The test-host hang is the symptom.
