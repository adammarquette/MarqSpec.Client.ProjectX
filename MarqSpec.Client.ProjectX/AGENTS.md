# AGENTS.md — Coding Agent (the library)

The **Coding Agent** contract, governing the library and its unit tests. Takes precedence over the root
`AGENTS.md` for this subtree (root rules still apply unless overridden here). The **QA Agent** owns the
integration tests separately — see
[`MarqSpec.Client.ProjectX.IntegrationTests/AGENTS.md`](../MarqSpec.Client.ProjectX.IntegrationTests/AGENTS.md).

## Role

Write **library code** and the **unit tests** that drive it. You do **not** write integration tests — QA does
that *independently*, so intent and implementation are verified separately.

## Test-first (mandatory)

- Write the **failing unit test before** the implementation (red → green → refactor). No new public method
  without a failing test written first; bug fixes are regression-first.
- Unit tests go in **`MarqSpec.Client.ProjectX.Tests`**, one folder per feature area mirroring the namespace,
  **every public method covered**, fully mocked with **FakeItEasy** (no I/O, no network — a unit test that opens
  a socket belongs in the integration project). The whole suite runs in seconds. Name:
  `MethodUnderTest_Should{ExpectedBehavior}_When{condition}`.
- Prefer `[Theory]` where a fact is really a table. The current suite is 162 `[Fact]`s and zero `[Theory]`s,
  which is why boundary cases are thin.

## Standards

**Multi-targets `net8.0;net10.0`, C# latest.** File-scoped namespaces, nullable on, **warnings-as-errors**,
immutability by default, **DI through the constructor**, async-all-the-way with `CancellationToken` on every
public async method, structured logging via `ILogger`, exhaustive switches. **Money, prices and tick sizes are
`decimal` — never `float`/`double`.** Define queries in fluent / method syntax, never LINQ query-comprehension.

Every public type and member carries XML documentation — `GenerateDocumentationFile` is on and the package ships
the XML, so a missing comment is a build error, not a style note.

## What this library is not

- **It does not decide.** No risk limit, no sizing rule, no eligibility check, no auto-flatten. Those belong to
  the consumer, above this transport. A policy refusal added here would sit *below* the parent's risk gate,
  where that gate cannot see or audit it. Rejecting a malformed request or a missing credential is transport,
  not policy, and is fine.
- **It does not retry what it cannot safely retry.** `POST /api/Order/place` is excluded from the resilience
  pipeline: a retried timeout can place a second live order, and the venue has no idempotency key. Any new
  endpoint added to the retry set must state, in its PR, why resending is safe
  ([ADR-0002](../documentation/adr/0002-resilience-and-idempotency.md)).
- **It does not log credentials.** Not the API key, not the secret, not the bearer token, not a request body
  containing them. `AddHttpClient`'s request-header logging redacts nothing by default — if a new typed client
  carries a secret header, call `RedactLoggedHeaders` on it.

## Wire models

`Api/Models` is one public type per file, mirroring the gateway contract in `swagger.json`. When the contract and
a model disagree, `swagger.json` wins and the model is the defect. Enum serialization is **integers, not
strings** — the gateway rejects string enums, and `_gatewaySettings` deliberately omits a string-enum converter.
Do not "fix" that back.

## Dependencies

Central Package Management (`Directory.Packages.props`) — `.csproj` files carry no versions. The library's
dependency surface is deliberately small and is part of its public contract: adding one to a package consumed by
trading-copilot risks a version conflict in the parent, so a new dependency needs a line in the PR saying why.

## Definition of done

Failing-test-first now green · every public method covered · standards + `dotnet format --verify-no-changes`
clean · `dotnet build` clean under warnings-as-errors on **both** target frameworks · traces to the task's issue
and a PRD requirement (`R-#`) · no secrets · the library README updated if the public surface moved.
