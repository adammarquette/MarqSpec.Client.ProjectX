# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> **How this file is maintained.** The version comes from the git tag, not from a file
> ([ADR-0006](documentation/adr/0006-tag-driven-versioning.md)), so this changelog is now the only thing here
> that can go stale. Its entry moves in the **promotion PR**, not after the release — the 1.0.3–1.0.5 gap below
> was backfilled precisely because "write it up afterwards" does not survive contact with a merge.

## [Unreleased]

### Added
- **`MarqSpec.Client.ProjectX.FakeGateway`** — a stand-in for the ProjectX venue: the REST surface from
  `swagger.json`, both SignalR hubs, a real signed JWT, and a `/_control` surface for driving scenarios
  (fills, quotes, `429` with `Retry-After` in either encoding, `5xx`, stalls)
- **`MarqSpec.Client.ProjectX.IntegrationTests`** — an integration tier that runs with **no credentials and no
  Docker**, hosting the fake gateway in-process on an ephemeral Kestrel port. 21 tests covering the REST
  surface, the resilience pipeline, and both hubs over a real WebSocket
- `docker-compose.yml` (the fake gateway as a service) and `docker-compose.dev.yml` (a pinned SDK toolchain)
- `[LiveGatewayFact]`, which skips on a condition that can actually become false, and a `Category=Live` trait
  for the opt-in real-gateway tier
- **`branch-policy.yml`** — `ladder` (enforcing `main` ← `staging` ← `develop`, and that a promotion's head
  branch lives in this repository), `commit-hygiene` (Conventional subjects, no leftover `fixup!` / `wip`), and
  an advisory `issue-link` check that reads GitHub's bound closing references rather than the body text
- **A coverage gate**, replacing an artifact that was uploaded and never evaluated
- **A documentation link check** in CI, so a dead relative link fails a build instead of a reader
- `dependabot.yml` for NuGet and GitHub Actions, grouped so `Microsoft.Extensions.*` moves together, and
  capped below the FluentAssertions licence change
- `MessageSendFailed` event on `IProjectXWebSocketClient` for reporting failed WebSocket message sends
- `WebSocketMessageFailedEventArgs` event args class with `HubName`, `MethodName`, `Arguments`, `Exception`, and `Timestamp`
- `LoginErrorCode` enum mirroring the API's `LoginErrorCode` contract, used to describe authentication failures
- Unit tests for Retry-After header handling in the Polly retry policy
- Unit tests for `MessageSendFailed` event behavior
- `[Trait("Category", "Integration")]` on all integration test classes for filtering
- `WebSocket` section in `appsettings.example.json` with all configurable options
- NuGet pack step and code coverage collection in the CI pipeline
- `Directory.Build.props` and `Directory.Packages.props` — central package management, warnings-as-errors, and
  deterministic, source-linked builds
- `global.json` pinning the SDK band
- Symbol package (`.snupkg`) output, which the PRD had asked for since the beginning
- `.editorconfig` and a rewritten `.gitattributes`, pinning LF in both so `dotnet format` and git agree
- `scripts/claim.sh` and `scripts/check-doc-links.sh`
- The `documentation/` corpus — routing map, `R-#` PRD, architecture doc, eight ADRs, role contracts
- Agent contracts (`AGENTS.md` + `CLAUDE.md`) at the root, the library, and the workflows
- `CONTRIBUTING.md`, `NOTICE`, `.dockerignore`, issue forms, and a rebuilt PR template
- This changelog

### Changed
- Polly retry policy now respects the `Retry-After` header on 429 responses (both delta-seconds and HTTP-date formats)
- **`POST /api/Order/place` is never automatically retried.** The gateway offers no idempotency key, so a
  retried placement after a lost acknowledgement is a second live order
- WebSocket `AccessTokenProvider` now fetches a fresh token on each reconnect instead of using a stale captured token
- WebSocket hub logging now uses `ILoggerFactory` instead of a broken `ILogger` → `ILoggerProvider` cast that silently dropped all hub logs
- Integration tests now use `[Fact(Skip = "...")]` instead of a silent early return
- CI now targets `develop` / `staging` / `main`, runs `dotnet format --verify-no-changes` (which it never did),
  runs the integration tier as a required check, and packs without `-p:PackageVersion` so MinVer resolves the
  version from the tag
- CodeQL now installs both target frameworks' SDKs, matching `ci.yml` — it previously analysed a `net10.0`-only
  build of a library that multi-targets
- **The release workflow no longer passes live API credentials into an unfiltered `dotnet test`.** The live
  tier was in scope on the release path; that it mostly did not execute was an accident of hardcoded skip
  strings, not a design. It now runs `Category!=Live` and verifies the packed version matches the tag and that
  a `.snupkg` was produced
- **The version is now derived from the git tag by MinVer.** `<Version>` is removed from the csproj, where it
  had drifted in both directions — behind at `1.0.4` against a released `v1.0.5`, then ahead at `1.0.6` against
  a version that was never tagged
- `MarqSpec.Client.ProjectX.slnx` no longer references 20 `docs/*.md` files deleted in `3631873` / `335a769`
- The `_gatewaySettings` field and two test fixtures renamed to satisfy the naming rules now enforced at build

### Fixed
- `BuildHubConnection` logger was always falling back to `NullLoggerProvider` because `ILogger<T>` never implements `ILoggerProvider`
- WebSocket connections used a captured access token that became stale after token refresh or expiry
- `GatewayTrade` market hub handler now deserializes the server payload as an array (`TradeUpdate[]`) instead of a single object, matching the gateway contract; previously trade events failed to bind and were silently dropped
- Market hub array handlers (`GatewayDepth`, `GatewayTrade`) now guard against a null payload array, and `GatewayQuote` guards against a null update, preventing `NullReferenceException` on malformed messages
- Authentication failures now surface the API's `errorCode` (e.g. `InvalidCredentials`, `ApiSubscriptionNotFound`, `ApiKeyAuthenticationDisabled`) instead of collapsing every failure to `Authentication failed: Unknown error`
- `loginKey` request now sends `userName`/`apiKey` matching the Swagger `LoginApiKeyRequest` schema (previously `username`/`apikey`)
- The empty-response diagnostic in `AuthenticationService` never actually fired: `JsonSerializer.Deserialize` throws on an empty body before the `null`-check meant to catch it could run
- **Credentials set as `ProjectX__ApiKey` / `ProjectX__ApiSecret` now reach the options.** Only the flat
  `PROJECTX_API_KEY` form was read, via a direct `Environment.GetEnvironmentVariable` call that bypasses the
  configuration pipeline — so the double-underscore names `release.yml` sets were silently ignored. Both
  conventions are now supported, with the flat form still winning so no existing deployment changes behaviour

### Removed
- **`run-integration-tests.ps1`**, whose documented job was to rewrite source files to strip `Skip`
  attributes. Nothing edits source in order to run tests any more
- The live-API integration tests from inside the *unit-test* project, and the tracked
  `appsettings.integration.json` they read credentials from. 22 of their 43 facts carried a hardcoded skip
  string that ignored the credential detection sitting right beside it, so they were disabled whether or not
  credentials existed — coverage-shaped rather than coverage
- Committed junk: a 0-byte `swagger_full.json`, three `.cs.backup` files (~40 KB of dead near-duplicate
  source), and the root-level `DiagnoseContracts.cs` / `TestContractSearch.csx` scripts, which sat outside any
  project and were compiled by nothing
- `MarqSpec.Client.ProjectX.Tests/Integration/README.md`, which documented test classes and methods that no
  longer existed

### Security
- Replaced a committed API key/secret with placeholder values in `appsettings.example.json`, `MarqSpec.Client.ProjectX.Diagnostics/appsettings.json`, and `MarqSpec.Client.ProjectX.Samples/appsettings.json` after rotating the exposed credential (corrects a prior entry that misnamed the file as `appsettings.integration.json`, which never held a real credential; the original value remains in this public repo's initial-commit history — a rotation, not a history rewrite, is its remediation)
- Stopped tracking `appsettings.json` in the Diagnostics and Samples projects and added them to `.gitignore`
- `.gitignore` now excludes `appsettings.json` and `appsettings.*.json` **repo-wide**, keeping only
  `*.example.json` templates. The two entries above untracked their files by path; this generalises the rule so
  a new project cannot reintroduce the problem simply by being new
- Stopped tracking `MarqSpec.Client.ProjectX.Tests/appsettings.integration.json`, which the by-path rules
  missed, replacing it with `appsettings.integration.example.json`
- Every project that reads credentials declares a `UserSecretsId`, so `dotnet user-secrets` is available and a
  developer never needs to edit a tracked file
- `NuGetAudit` set to fail restore on a HIGH or CRITICAL advisory rather than warn

## [1.0.5] - 2026-06-08

### Changed
- WebSocket functionality and models updated to match the gateway's hub contracts

## [1.0.4] - 2026-04-02

### Added
- Multi-targeting for .NET 8.0 alongside .NET 10.0
- MIT `LICENSE`
- CodeQL security scanning, `CODEOWNERS`, a pull-request template, and release approval gates

### Fixed
- CodeQL workflow permissions scoped correctly; semver validation added to the release workflow

## [1.0.3] - 2026-03-29

### Changed
- Incremental refinements across the client surface

## [1.0.2] - 2026-03-28

### Added
- Initial public version with REST and WebSocket client support
- Refit-based REST API client (`IProjectXApiClient`) with 17 async methods
- SignalR-based WebSocket client (`IProjectXWebSocketClient`) with Market and User hubs
- Automatic reconnection with exponential backoff
- Polly resilience pipeline with retry, circuit breaker support
- DI registration via `AddProjectXApiClient(IConfiguration)`
- Sample console application
- Diagnostic tools project
- Unit and integration test suite
