# ADR-0003 — JWT acquisition and in-memory token cache

**Status:** Accepted

## Context

The gateway authenticates with a bearer JWT obtained from `POST /api/Auth/loginKey`. Every REST call and both
hub connections need one. Acquiring a token per request would be slow and would burn the `POST` rate budget
(50 / 30 s) that order operations need.

Two wrinkles come from the gateway, not from us:

1. **The field names are misleading.** `loginKey` takes `{ "userName": …, "apiKey": … }`, and the values a user
   is given in the ProjectX UI are labelled "API key" and "API secret". So `ProjectXOptions.ApiKey` is
   transmitted as `userName`, and `ApiSecret` as `apiKey` — meaning the wire field called `apiKey` carries the
   value the operator knows as their *secret*. (The exact casing matters and has already been wrong once: the
   request sent `username`/`apikey` until it was corrected to match the `LoginApiKeyRequest` schema.)
2. **The response does not state an expiry** in a form the client reads. The JWT carries an `exp` claim, but
   nothing outside the token says when it dies.

## Decision

Cache one token in memory, in `AuthenticationService`, behind a `SemaphoreSlim` with a double-checked read so
concurrent callers produce **one** login rather than N.

- **Assume a 55-minute life.** Stamp `_tokenExpiration = UtcNow.AddMinutes(55)` on acquisition and refresh when
  within one minute of it. Tokens are observed to last an hour; 55 minutes is the safety margin.
- **Keep the option names user-facing, and map at the wire.** `ApiKey`/`ApiSecret` match what the operator is
  handed; the transmitted names match the gateway. Neither side is renamed to match the other, because either
  rename makes one of the two audiences wrong.
- **Validate at construction.** `AuthenticationService`'s constructor calls `_options.Validate()`, so a missing
  credential throws at DI resolution rather than at the first call (R-1.4).
- `POST /api/Auth/validate` may return a `newToken`; adopt it and re-stamp. `POST /api/Auth/logout` clears the
  cache.
- Hub connections use an `AccessTokenProvider` that calls back into this service on **every** reconnect, so a
  reconnect never replays a token captured at construction.

## Alternatives considered

**Parse the `exp` claim.** The correct answer, and deferred rather than rejected. It needs a JWT parser —
`System.IdentityModel.Tokens.Jwt` or hand-rolled base64url decoding — and this library's dependency surface is
part of its public contract (adding one risks a version conflict in trading-copilot, which compiles against
this assembly). The assumed 55 minutes has not caused an observed failure. See Follow-ups.

**No cache; authenticate per request.** Rejected: doubles every call's latency and consumes the `POST` rate
budget that order placement needs.

**Cache in a distributed store.** Rejected: this is an in-process client library, not a service. A consumer
running multiple processes gets a token per process, which is correct.

**Rename the options to `Username`/`ApiKey` to match the wire.** Rejected: it would match the gateway and
mismatch every operator reading their own credentials page.

## Consequences

- One login per process per ~55 minutes.
- **A server-side token revocation is not noticed until the next 401**, which is not retried and surfaces to the
  caller. Acceptable: a revoked credential *should* be loud.
- If the gateway ever shortens token life below 55 minutes, requests fail with 401 until the cache turns over.
  That is the failure mode the `exp` follow-up removes.
- The `ApiKey`-is-really-a-username trap is permanent, so it is documented in three places a reader might land:
  here, the [architecture doc](../projectx-client-architecture.md#authentication), and R-1.

## Follow-ups

- Parse the `exp` claim and use it, falling back to 55 minutes when absent or implausible. Blocked only on
  accepting a JWT-parsing dependency, or on writing the ~20 lines of base64url decode inline.
- `WebSocketOptions` binds the hardcoded string `"ProjectX:WebSocket"` while `ProjectXOptions` uses a
  `SectionName` constant. Cosmetic, but the constant is the right pattern.
