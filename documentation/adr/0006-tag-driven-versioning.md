# ADR-0006 — The git tag is the version

**Status:** Accepted

## Context

The version was declared in `MarqSpec.Client.ProjectX.csproj` as `<Version>1.0.4</Version>` and bumped by hand.
The release workflow then overrode it with `-p:PackageVersion=` derived from the release tag.

So there were two sources of truth, one of which silently lost. The observable results:

- **The csproj said `1.0.4` while the latest tag and published release were `v1.0.5`.** The csproj value was
  already dead — the workflow overrode it — but it was the value a developer read, and the value stamped into
  the assembly when anyone built locally or when `--no-build` was used before packing.
- **Tags are inconsistently named**: `1.0.2` has no prefix; `v1.0.0`, `v1.0.3`, `v1.0.4`, `v1.0.5` do.
- **Releases are inconsistently titled**: one is `1.0.0a`.
- **`CHANGELOG.md` jumps from `[Unreleased]` to `[1.0.2]`** while 1.0.3, 1.0.4 and 1.0.5 all shipped.

None of that is carelessness in isolation; it is what happens when the version lives in a file that nothing
forces you to update.

## Decision

**No file declares a version. The nearest git tag is the version**, computed by
[MinVer](https://github.com/adamralph/minver) at build time.

- `<Version>` is removed from the csproj. `Directory.Build.props` adds MinVer with `MinVerTagPrefix=v`.
- Tags are **`vMAJOR.MINOR.PATCH`**, always prefixed. The unprefixed `1.0.2` stays as history; new tags do not
  repeat it.
- Tags are cut on **`main` only**, after promotion through the ladder (ADR-0008).
- Between tags MinVer produces a pre-release version, so a package built from `develop` is visibly not a
  release.
- `release.yml` stops passing `-p:PackageVersion`; there is nothing left to override.
- SemVer 2.0 (R-10.4). A breaking public-surface change needs a major bump **and** an ADR, because
  trading-copilot compiles against this assembly directly (R-10.5).
- **The changelog entry lands in the promotion PR**, not after the release. That is what closes the 1.0.3–1.0.5
  gap for good.

## Alternatives considered

**Keep `<Version>` and add a CI check that it matches the tag.** Rejected: it turns a class of error into a
class of red build. Removing the second source is strictly better than validating agreement between two.

**GitVersion.** Rejected as heavier than needed — it wants branch-name conventions to infer semantics, and this
repo's semantics come from an explicit tag on `main`.

**Nerdbank.GitVersioning.** Reasonable; rejected because it reintroduces a version file (`version.json`), which
is the thing being removed.

**Date-based or build-number versioning.** Rejected: R-10.5 needs a major/minor signal a consumer can read.

## Consequences

- The version cannot drift, because there is nothing to drift from.
- **A shallow clone breaks version inference** — MinVer needs tag history. CI checkouts need `fetch-depth: 0`,
  and a build without history produces `0.0.0-alpha.0` rather than failing. Worth knowing before it is confusing.
- A local build with no tags nearby yields a pre-release version. Correct, and occasionally surprising.
- Publishing is now purely: promote, tag, release. No file edit is part of cutting a release.

## Follow-ups

- Backfill `CHANGELOG.md` for 1.0.3, 1.0.4 and 1.0.5 from the merged PRs.
