Marquette Speculations

# ProjectX API Client

## Executive Summary
The ProjectX API Client is a software library designed to facilitate seamless integration with the 
ProjectX REST API and WebSocket API. It provides developers with an easy-to-use interface for querying
market data, placing orders, and streaming real-time updates.

**Target Audience**: .NET developers building trading applications and market data integrations.

**Key Success Criteria**: 
- Enable developers to integrate with ProjectX API in under 30 minutes
- Provide production-ready reliability with auto-reconnection and retry logic
- Deliver real-time market data with minimal latency

## User Stories
1. As a developer, I want to be able to authenticate with the ProjectX API using my API key and secret. These need to be configurable via environment variables or a configuration file.
  - **Acceptance Criteria:**
    - API credentials can be configured via environment variables (PROJECTX_API_KEY, PROJECTX_API_SECRET)
    - API credentials can be configured via appsettings.json
    - Authentication failures return clear error messages
    - Credentials are never logged or exposed in error messages
    
2. As a developer, I want to be able to query market data such as current prices, order book depth, and recent trades for various futures.
  - **Acceptance Criteria:**
    - Methods are available to retrieve current prices, order book depth, and recent trades
    - API responses are properly deserialized into C# models
    - Errors from the API are handled gracefully with meaningful messages
    
3. As a developer, I want to be able to place and manage orders, including creating new orders, modifying existing orders, and canceling orders.
  - **Acceptance Criteria:**
    - Methods are available to create, modify, and cancel orders
    - API responses for order management are properly deserialized into C# models
    - Errors from the API are handled gracefully with meaningful messages
    
4. As a developer, I want to be able to stream real-time market data and order updates using WebSockets.
  - **Acceptance Criteria:**
    - The client can connect to the WebSocket API and subscribe to market data and order updates
    - Real-time updates are received and deserialized into C# models
    - The client automatically reconnects within 5 seconds if the WebSocket connection is lost
    - The client can handle a throughput of 1000 events/second per stream without performance degradation


## Technical Requirements
- The API client should be implemented in C# and target .NET 10 or later.
- The client should use asynchronous programming patterns to ensure non-blocking API calls.
- The client should allow for easy configuration of API credentials and endpoints, supporting both environment variables and configuration files.
  - This should be implemented using the Options pattern.
- The client should handle authentication securely, ensuring that API keys and secrets are not exposed in logs or error messages.
- The client should provide clear and concise error handling, returning meaningful error messages when API calls fail.
- The client should be designed for extensibility, allowing for easy addition of new API endpoints and features in the future.
- The client should include comprehensive unit tests to ensure reliability and maintainability.
- The client should be well-documented, including usage examples and API reference documentation.
  - All models and methods should have XML documentation comments that match the descriptions provided in the swagger documentation.
- All async methods must accept CancellationToken parameters to support cancellation
- The client must be thread-safe and support concurrent API calls
- Rate limiting should be handled with automatic retry using exponential backoff (via Polly)
- Failed WebSocket messages should be queued and retried or reported to observers

## Technology Stack and Dependencies
- C# with .NET 10 or later
- Refit (minimum version 7.0.0)
- Microsoft.AspNetCore.SignalR.Client (minimum version 9.0.0 for .NET 10)
- xUnit (minimum version 2.9.0)
- FakeItEasy (minimum version 8.0.0)
- FluentAssertions (minimum version 6.12.0)
- Polly for handling transient faults and retries in API calls (optional, but recommended for improved resilience)
  - Minimum version 8.6.5 
- SignalR for WebSocket integration
  - The client should use the Observer pattern to make it easy for developers to subscribe to real-time updates from the WebSocket API. 
- xUnit for unit testing
  - Tests should use FakeItEasy for mocking dependencies and should cover all public methods of the API client, including edge cases and error scenarios.
  - Test should use FluentAssertions for more readable assertions.
- Microsoft.Extensions.Options for configuration management
  - Use IOptions<T> to manage API credentials and endpoints, allowing for flexible configuration via environment variables or configuration files. 
- Microsoft.Extensions.Logging.Abstractions for logging
  - Use ILogger<T> throughout the client to allow consuming applications to use any logging provider
  - Include sample configurations for common providers (Serilog, NLog, etc.) in documentation

## External Interfaces
- REST API: https://api.topstepx.com/swagger/v1/swagger.json
  - **Base URL**: https://api.topstepx.com
  - **Authentication Method**: JSON Web Token  - **Rate Limits**:
    - POST: 50 requests / 30 seconds (primarily order operations)
    - GET/DELETE: 200 requests / 60 seconds (market data, order queries)
    - Must handle 429 responses with Retry-After header
- WebSocket User API: https://rtc.topstepx.com/hubs/user
  - **Protocol**: SignalR Core (JSON protocol recommended)
  - Reference: https://gateway.docs.projectx.com/docs/realtime/#user-hub-events
- Default WebSocket Market URL: https://rtc.topstepx.com/hubs/market
  - **Protocol**: SignalR Core (JSON protocol recommended)
  - Reference: https://gateway.docs.projectx.com/docs/realtime/#market-hub-events 

## Security Requirements
- API keys and secrets must never be logged or included in exception messages
- HTTPS must be enforced for all REST API calls
- WSS (WebSocket Secure) must be used for all WebSocket connections
- The client should validate SSL certificates by default
- Sensitive configuration should support encryption at rest

## Non-Functional Requirements
- **Compatibility**: Target .NET 10+, but consider multi-targeting for broader adoption
- **Deployment**: Distribute as NuGet package with symbol packages for debugging
- **Backward Compatibility**: Follow SemVer 2.0 for versioning
- **Memory Efficiency**: WebSocket connections should not leak memory during long-running operations
- **Graceful Degradation**: API client should handle partial service outages
- **Coding Practices**:
  - All classes, structures, and enum should be in separate files. 

## Metrics

### Development Metrics
- **Unit Test Coverage**: All public methods must achieve minimum 95% line and 90% branch coverage
- **Integration Test Coverage**: All public methods must have at least one integration test against the actual API (can be in a separate test project)
  - Configurations for integration tests should be flexible to allow running against staging or production environments. 
- **Code Quality**: Adhere to SOLID principles and maintain analyzers warnings at zero
- **Documentation Alignment**: All XML comments must match swagger documentation

### Performance Metrics
  - **REST API Latency**: p95 < 500ms, p99 < 1000ms (under normal network conditions)
  - **WebSocket Throughput**: 1000 events/second per stream without message loss
  - **WebSocket Latency**: p99 < 100ms from server send to client callback invocation
  - **Memory Stability**: No memory leaks during 24-hour continuous WebSocket operation
- ### Reliability Metrics
  - **Auto-Reconnection**: Automatically re-connect within 5 seconds of disconnection
  - **API Success Rate**: >99.9% success rate under normal conditions (excluding 4xx client errors)
  - **Retry Resilience**: Transient failures (5xx, network timeouts) automatically retried up to 3 times

## Example Usage
Provide a quick-start code snippet showing:
1. Configuration setup (DI registration)
2. Making a REST API call
3. Subscribing to WebSocket updates

This will help validate the API design early.

## Out of Scope
- UI components or visualization tools
- Backtesting or historical data analysis
- Order execution algorithms or trading strategies
- Multi-account management (if applicable)