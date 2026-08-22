# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> **How this file is maintained.** The version comes from the git tag, not from a file
> ([ADR-0006](documentation/adr/0006-tag-driven-versioning.md)), so this changelog is now the only thing here
> that can go stale. Its entry moves in the **promotion PR**, not after the release — the 1.0.3–1.0.5 gap below
> was backfilled precisely because "write it up afterwards" does not survive contact with a merge.

## [2.1.0] - 2026-08-22

### Changed
- **Eleven configuration options now do what they say.** All were bound from configuration, listed in the
  library README, and read by no code path: the three `RetryOptions` values (the pipeline hardcoded `3` / `1s`
  / `30s`), the four `WebSocket` timeouts and `MaxBufferSize`, and both `ProjectXOptions`-level hub URLs
  (gh#69)
- **`WebSocket.AutoReconnect: false` now actually stops reconnection.** `WithAutomaticReconnect` was applied
  unconditionally and the flag only decided whether a log line was written
- **Both hub-URL spellings resolve.** `ProjectX:WebSocketUserHubUrl` / `WebSocketMarketHubUrl` now act as a
  fallback for `ProjectX:WebSocket:UserHubUrl` / `MarketHubUrl`, with the nested form winning when both are
  set. Only the nested form was ever read, while the README documented only the outer one — so a consumer
  following the docs to reach a simulation venue edited a dead key, got no error, and **stayed connected to the
  production TopstepX default**
- The library README's configuration table now matches the code, including the live hub-URL keys it previously
  omitted

### Deprecated
- `ProjectXOptions.ValidateSslCertificates` — **has no effect; TLS validation is always on.** Left unwired
  deliberately: honouring `false` would add a supported way to disable certificate validation against a live
  trading venue. Marked `[Obsolete]`, not removed
- `WebSocketOptions.UseMessagePack` — **has no effect; the hubs always speak JSON.** Wiring it needs a
  MessagePack protocol package, and this library's dependency surface is part of its public contract (R-10.5).
  Marked `[Obsolete]`, not removed

### Added
- The unit suite runs against **`net8.0` as well as `net10.0`** (gh#66). ADR-0005 called both targets
  first-class; until now only the build was, and the published `net8.0` assembly had never executed a test
- The coverage gate enforces its floor against the **lowest** of every report rather than an arbitrary one, so
  a regression on one target framework cannot hide behind another

### Fixed
- **`branch-policy.yml` no longer cancels its own required checks.** Every `pull_request` event shares
  `refs/pull/<n>/merge`, so opening a PR with several labels emitted several runs into one concurrency group
  and they cancelled one another — leaving `ladder`, `commit-hygiene` and `issue-link` reporting `cancelled`
  and the PR unmergeable with nothing failing (gh#64)

## [2.0.0] - 2026-08-22

> **This is the first release published to nuget.org since 1.0.4.** `v1.0.5` was tagged and a GitHub release
> was cut, but no package ever reached nuget.org: the release run **failed at the `Test` step** — an unfiltered
> `dotnet test` with live gateway credentials injected — and never got as far as pack or push. It went red and
> was not acted on. The release path no longer runs the live tier or carries credentials at all; it runs
> `Category!=Live` against the fake gateway ([ADR-0007](documentation/adr/0007-local-test-environment.md)).
> **Upgrading from 1.0.4 brings the 1.0.5 work as well as everything below.**

### Breaking changes

**The order search now requires a time window**
([ADR-0010](documentation/adr/0010-order-search-window-is-required.md)). The gateway's `/api/Order/search`
schema marks `startTimestamp` **required**. The client sent it as null, the gateway applied a window of its
own, and **an order outside that window came back absent — indistinguishable from "no such order".** In a
reconciliation path that reads as *a live order was never placed*.

- **`GetOrdersAsync` throws `ArgumentException` when `startTime` is null.** It previously returned whatever the
  gateway's own window happened to contain. Pass the window that was always required.
- **`GetOrderAsync(accountId, orderId)` is `[Obsolete(error: true)]`** — replaced by
  `GetOrderAsync(accountId, orderId, startTime, endTime)`. It no longer compiles, with a message naming the
  replacement; an assembly compiled against 1.0.x that still binds to it gets `NotSupportedException` rather
  than a wrong answer.
- **No default window is substituted anywhere.** A window the client invents silently reproduces exactly the
  failure above for any order just outside it. Callers who need "is this order live *right now*" and hold no
  sensible window should use `GetOpenOrdersAsync`, which takes only an account.

**No property was renamed.** An earlier plan for this release
([ADR-0009](documentation/adr/0009-search-order-window-rename.md), now superseded) would also have renamed
`SearchOrderRequest.StartTime` / `.EndTime` to `StartTimestamp` / `.EndTimestamp`. That rename was reverted
before release: fixing the wire names needed only `[JsonPropertyName]`, and breaking every consumer's compile
over a CLR name bought nothing. **`StartTime` and `EndTime` still compile.**

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
- **`IAuthenticationService` is now a singleton.** It was registered as a typed HTTP client, which makes it
  *transient*, so every consumer received a fresh instance with an empty token cache: `ProjectXApiClient` and
  the Refit pipeline's `AuthenticationHandler` logged in independently on every scope, the double-checked lock
  inside `GetAccessTokenAsync` could not dedupe across instances, and `LogoutAsync` cleared a cache the
  transport never read. The captive `HttpClient` sets `PooledConnectionLifetime` so it cannot pin a stale DNS
  answer
- **`ValidateSessionAsync` and `LogoutAsync` now send the bearer token.** The handler that attaches
  `Authorization` is registered on the Refit client only, never on the `HttpClient` the authentication service
  owns, so both routes went out bare and the gateway answered `401`: `ValidateSessionAsync` could never return
  `true`, and `LogoutAsync` reported success while never terminating the server-side session. Neither method
  had a single test in either project
- `SearchOrderRequest.ContractId` and `.Status` are documented as **serialized and ignored** — neither appears
  in the gateway's schema, so neither filters anything. The fake gateway no longer pretends otherwise
- The fake gateway rejects a null `startTimestamp` on `/api/Order/search` with `400`, matching the schema; a
  double more permissive than the venue is how a suite goes green against a broken client
- The packaged README no longer claims ".NET 10.0 or later" for a package that multi-targets `net8.0`
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
