# ADR-0004 — The SignalR hub client is a singleton

**Status:** Accepted

## Context

`ProjectXWebSocketClient` owns two long-lived `HubConnection`s (user and market), each with its own reconnect
state, subscription set, and event handlers. `AddProjectXApiClient` has to choose a DI lifetime for it, and the
choice is not cosmetic: a `HubConnection` is a stateful, connected resource, not a stateless helper.

The REST facade faces the same question with the opposite answer — it is stateless and cheap.

## Decision

```csharp
services.AddScoped<IProjectXApiClient, ProjectXApiClient>();
services.AddSingleton<IProjectXWebSocketClient, ProjectXWebSocketClient>();
```

**The hub client is a singleton. This is part of the library's contract, not an implementation detail** — the
consumer's own architecture depends on it. trading-copilot's ADR-0015 assumes a single connection pair per
process, and its `ProjectXConnection` and `ProjectXAccountEventStream` both take
`IProjectXWebSocketClient` by constructor injection expecting to observe the *same* stream.

The type is `IAsyncDisposable`; the container owns its disposal.

## Alternatives considered

**Scoped, matching the REST client.** Rejected, and this is the one that would look like consistency and be a
bug. Every scope would construct a second client, open a second pair of hub connections, and re-subscribe —
so every event would be delivered twice to a consumer holding two scopes, and the connection count would grow
with request volume.

**Transient.** Rejected for the same reason, more so.

**A connection pool or factory.** Rejected as unwarranted: the gateway exposes exactly two hubs and a client
needs at most one connection to each. A pool would add lifecycle complexity with nothing to allocate.

**Let the consumer choose.** Rejected: the failure mode of choosing wrong is silent double-delivery, which
surfaces as duplicated fills in the consumer's event log rather than as an error. A library should not offer a
choice whose wrong answer is invisible.

## Consequences

- One connection pair per process. Correct for an in-process client.
- **A singleton cannot depend on a scoped service.** Anything the hub client needs must be singleton-safe;
  `IAuthenticationService` is a typed `HttpClient` registration and is safe to call from it.
- A consumer that resolves it from a scope still gets the singleton — which is what they want, and why the
  lifetime is documented rather than left to be inferred.
- Changing this lifetime is a **breaking change** to the consumer's behaviour without being a breaking change to
  the compiled API surface. That is the worst kind, so it needs a superseding ADR, not an edit.

## Follow-ups

None.
