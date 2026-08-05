# Architecture — MarqSpec.Client.ProjectX

How the library is put together, and why. Requirements are in the [PRD](projectx-client-prd.md) (`R-#`);
decisions and their alternatives are in [`adr/`](adr/). This document describes the *shape* — read it before
changing the request pipeline, the auth flow, or hub lifetimes.

## The one-paragraph version

A consumer calls `AddProjectXApiClient(configuration)`. That registers three things: a Refit-generated REST
client wrapped in an auth handler and a resilience pipeline, a JWT authentication service with an in-memory
token cache, and a SignalR client holding two hub connections. `ProjectXApiClient` is a thin facade over the
Refit interface; `ProjectXWebSocketClient` turns hub callbacks into .NET events. Nothing above transport lives
here (R-8).

## Composition

```
IServiceCollection
├── Configure<ProjectXOptions>          section "ProjectX"
├── Configure<WebSocketOptions>         section "ProjectX:WebSocket"
├── AddHttpClient<IAuthenticationService, AuthenticationService>()      typed client
├── AddRefitClient<IProjectXRestApi>(_gatewaySettings)
│     ├── ConfigureHttpClient          BaseAddress from ProjectXOptions.BaseUrl
│     ├── AddHttpMessageHandler        AuthenticationHandler   (outer)
│     └── AddStandardResilienceHandler retry / 429 / 5xx       (inner)
├── AddScoped<IProjectXApiClient, ProjectXApiClient>()
└── AddSingleton<IProjectXWebSocketClient, ProjectXWebSocketClient>()
```

**The lifetimes are load-bearing, not defaults.** `IProjectXWebSocketClient` is a **singleton** because it owns
two long-lived `HubConnection`s with their own reconnect state — resolving a second one would open a second pair
of connections and double every event. trading-copilot's ADR-0015 depends on this being true. The REST facade is
**scoped** because it is stateless and cheap. See [ADR-0004](adr/0004-hub-client-is-a-singleton.md).

## The request pipeline

Delegating handlers wrap in registration order, first-registered outermost:

```
ProjectXApiClient
  → IProjectXRestApi            (Refit-generated)
    → AuthenticationHandler     attaches Authorization: Bearer <jwt>
      → ResilienceHandler       retry, backoff, Retry-After
        → HttpClientHandler     the socket
```

Two consequences of that order are worth knowing before you change it:

- **The token is attached once, outside the retry loop.** A retry burst reuses the header set on the first
  attempt. That is safe today because the token carries a refresh buffer far larger than the retry window
  (3 attempts, 30 s cap) and because **401 is not in the retry set** — an expired token surfaces to the caller
  rather than being retried with the same stale value.
- **Moving the resilience handler outward would re-authenticate per attempt** — and would also move the
  idempotency guard's view of the request. Do not reorder these without reading
  [ADR-0002](adr/0002-resilience-and-idempotency.md).

### Retry — and the one endpoint that is excluded

`ShouldRetryOutcome` is factored out of the registration lambda specifically so it can be unit-tested against
real request and outcome shapes. It retries on `HttpRequestException`, `429`, and `>= 500` — **except** when
`IsOrderPlacement(request)` is true.

**`POST /api/Order/place` is never retried.** The gateway offers no idempotency key. If the first attempt
reached the venue and booked the order but the acknowledgement was lost, a retry places a *second live order*.
So the fault surfaces to the caller, which classifies it as an **indeterminate** outcome and reconciles, rather
than the transport quietly deciding to try again.

The guard is deliberately asymmetric: an unidentifiable request (a defensive null) is treated as *not* a
placement, so the guard only ever **removes** retries from a confirmed placement and can never add double-place
risk relative to the pipeline without it.

`Retry-After` on a 429 is honoured in both encodings the header allows — `Delta` (delta-seconds) and `Date` (an
HTTP-date), the latter converted against `DateTimeOffset.UtcNow` and ignored if it lands in the past. A missing
or unusable header falls through to exponential backoff.

### Serialization

`_gatewaySettings` uses `JsonSerializerDefaults.Web` **without** a string-enum converter, so enums travel as
integers. This is deliberate and was a bug fix: Refit's default writes enums as camelCase strings, the gateway's
schema types every enum as an integer, and `retrieveBars` failed with
`400 "The JSON value could not be converted to AggregateBarUnit"` on every request. Re-adding a
`JsonStringEnumConverter` re-breaks it.

## Authentication

```
POST /api/Auth/loginKey   { "userName": <ApiKey>, "apiKey": <ApiSecret> }   →   JWT
POST /api/Auth/validate                                                     →   optional newToken
POST /api/Auth/logout                                                       →   clears the cache
```

**The field names are the gateway's, and they are misleading.** `ProjectXOptions.ApiKey` is transmitted as
`userName`; `ProjectXOptions.ApiSecret` is transmitted as `apiKey` — so the wire field named `apiKey` carries
the value the operator knows as their *secret*. The option names match what a user is given in the ProjectX UI;
the wire names match the gateway. Neither can be renamed unilaterally — see
[ADR-0003](adr/0003-jwt-acquisition-and-cache.md).

The casing is load-bearing and has been wrong before: the request sent `username`/`apikey` until it was
corrected to match the schema's `LoginApiKeyRequest`. Check `swagger.json`, not this document, if it matters.

The token is cached in memory behind a `SemaphoreSlim` with a double-checked read, so concurrent callers make
one login rather than N. **Expiry is assumed, not parsed**: the cache stamps `UtcNow + 55 minutes` and refreshes
with a one-minute buffer, rather than reading the `exp` claim. ADR-0003 records why, and what would have to be
true to change it.

`AuthenticationService`'s constructor calls `_options.Validate()`, so a missing credential throws at **DI
resolution** rather than at first call (R-1.4).

## Real-time

`ProjectXWebSocketClient` holds two `HubConnection`s:

| Hub | URL | Bound methods |
|---|---|---|
| User | `https://rtc.topstepx.com/hubs/user` | `GatewayUserAccount`, `GatewayUserOrder`, `GatewayUserPosition`, `GatewayUserTrade` |
| Market | `https://rtc.topstepx.com/hubs/market` | price, order-book and trade streams |

Callbacks are re-surfaced as .NET events — `PriceUpdateReceived`, `OrderBookUpdateReceived`,
`TradeUpdateReceived`, `AccountUpdateReceived`, `OrderUpdateReceived`, `PositionUpdateReceived`,
`TradeNotificationReceived` — plus `ConnectionStatusChanged` and `MessageSendFailed`.

`AccessTokenProvider` re-fetches a **fresh** token on every reconnect rather than closing over the one captured
at construction; replaying an expired token is how a reconnect loop turns into an auth-failure loop.
Auto-reconnect backs off 1 s → 5 s, satisfying R-5.3.

The type is `IAsyncDisposable`. A consumer that abandons it without disposing leaks both connections.

## The consumer boundary

trading-copilot consumes this library through `MarqSpec.TradingCopilot.Integration.ProjectX`, which maps wire
models to its own domain types in `ProjectXMapping.cs`. That mapping layer is the seam: **this library's models
never reach the parent's domain directly**, so a wire-shape change is absorbed in one file on their side.

What crosses the boundary, and in which direction:

| This library provides | The consumer provides |
|---|---|
| Transport, serialization, auth, reconnection, retry-on-idempotent | Risk limits, sizing, eligibility, flatten decisions |
| Typed wire models and gateway errors | Domain types, trading-session semantics, wall-clock policy |
| An **indeterminate** outcome on an order-path fault | The reconciliation that resolves it |

The third row is the one that matters. This library does not guess whether a timed-out placement is live; it
says it does not know, and the consumer owns the recovery.

## Testing shape

| Tier | Project | Backing | Runs |
|---|---|---|---|
| Unit | `MarqSpec.Client.ProjectX.Tests` | FakeItEasy, no I/O | always, in seconds |
| Integration | `MarqSpec.Client.ProjectX.IntegrationTests` | `FakeGateway` over compose | always, **no credentials** |
| Live | same project, `Category=Live` | the real gateway | opt-in only |

The fake gateway serves the REST surface *and* both hubs, and issues a real signed JWT so the token cache and
reconnect paths are genuinely exercised rather than stubbed past.
[ADR-0007](adr/0007-local-test-environment.md) records why a request-stubbing tool was not enough.

## Known shape issues

Recorded here rather than in an issue tracker footnote, because they are architectural and a reader will meet
them:

- **`WebSocketOptions` binds a hardcoded `"ProjectX:WebSocket"` string** while `ProjectXOptions` uses a
  `SectionName` constant. Inconsistent; the constant is the right pattern.
- **Credential environment variables are read directly, not through `IConfiguration`.** `ResolveCredential`
  calls `Environment.GetEnvironmentVariable` for both the flat (`PROJECTX_API_KEY`) and the double-underscore
  (`ProjectX__ApiKey`) forms, rather than relying on a configuration provider. That is deliberate:
  `AddProjectXApiClient` accepts an arbitrary `IConfiguration`, and one built without an environment provider
  would see neither form — which is exactly how `release.yml` came to set credentials the client could not
  read. The cost is that this bypasses the normal configuration precedence chain; the flat form wins over the
  double-underscore form, and both win over anything bound from a file.
