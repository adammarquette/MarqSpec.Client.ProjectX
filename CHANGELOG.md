# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `MessageSendFailed` event on `IProjectXWebSocketClient` for reporting failed WebSocket message sends
- `WebSocketMessageFailedEventArgs` event args class with `HubName`, `MethodName`, `Arguments`, `Exception`, and `Timestamp`
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

### Security
- Removed hardcoded API credentials from `appsettings.integration.json`

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
