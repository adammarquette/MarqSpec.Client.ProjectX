# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `MessageSendFailed` event on `IProjectXWebSocketClient` for reporting failed WebSocket message sends
- `WebSocketMessageFailedEventArgs` event args class with `HubName`, `MethodName`, `Arguments`, `Exception`, and `Timestamp`
- `LoginErrorCode` enum mirroring the API's `LoginErrorCode` contract, used to describe authentication failures
- Unit tests for Retry-After header handling in Polly retry policy
- Unit tests for `MessageSendFailed` event behavior
- `[Trait("Category", "Integration")]` on all integration test classes for filtering
- `WebSocket` section in `appsettings.example.json` with all configurable options
- NuGet pack step in CI pipeline
- Code coverage collection in CI pipeline
- This changelog

### Changed
- Polly retry policy now respects `Retry-After` header on 429 responses (both delta-seconds and HTTP-date formats)
- WebSocket `AccessTokenProvider` now fetches a fresh token on each reconnect instead of using a stale captured token
- WebSocket hub logging now uses `ILoggerFactory` instead of a broken `ILogger` → `ILoggerProvider` cast that silently dropped all hub logs
- Integration tests now use `[Fact(Skip = "...")]` instead of silent early-return with `if (SkipReason != null) return`
- CI pipeline filters out integration tests by default and adds a `pack` job on main/master pushes

### Fixed
- `BuildHubConnection` logger was always falling back to `NullLoggerProvider` because `ILogger<T>` never implements `ILoggerProvider`
- WebSocket connections used a captured access token that became stale after token refresh or expiry
- `GatewayTrade` market hub handler now deserializes the server payload as an array (`TradeUpdate[]`) instead of a single object, matching the gateway contract; previously trade events failed to bind and were silently dropped
- Market hub array handlers (`GatewayDepth`, `GatewayTrade`) now guard against a null payload array, and `GatewayQuote` guards against a null update, preventing `NullReferenceException` on malformed messages
- Authentication failures now surface the API's `errorCode` (e.g. `InvalidCredentials`, `ApiSubscriptionNotFound`, `ApiKeyAuthenticationDisabled`) instead of collapsing every failure to `Authentication failed: Unknown error`; the `loginKey` response carries the reason in `errorCode` while `errorMessage` is usually null
- `loginKey` request now sends `userName`/`apiKey` matching the Swagger `LoginApiKeyRequest` schema (previously `username`/`apikey`)
- The empty-response diagnostic in `AuthenticationService` never actually fired: `JsonSerializer.Deserialize` throws on an empty body before the `null`-check that was meant to catch it could run. The empty-body case is now detected before deserialization is attempted.

### Security
- Replaced a committed API key/secret with placeholder values in `appsettings.example.json`, `MarqSpec.Client.ProjectX.Diagnostics/appsettings.json`, and `MarqSpec.Client.ProjectX.Samples/appsettings.json` after rotating the exposed credential (corrects a prior entry here that misnamed the file as `appsettings.integration.json`, which never held a real credential; the original value remains in this public repo's initial-commit history — a rotation, not a history rewrite, is its remediation)
- Stopped tracking `appsettings.json` in the Diagnostics and Samples projects and added them to `.gitignore`, so real credentials can no longer be committed there — each project's README documents the shape to place in a local (ignored) `appsettings.json`

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
