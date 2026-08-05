# MarqSpec.Client.ProjectX

A .NET client library for the ProjectX gateway — REST over Refit, real-time over SignalR. It is a **transport
client**: it moves requests and events, and it makes no trading decisions
([R-8](documentation/projectx-client-prd.md)).

Consumed as a submodule by [trading-copilot](https://github.com/adammarquette/trading-copilot), and published to
nuget.org.

Targets **.NET 8.0** and **.NET 10.0**.

## For AI agents & new readers — start here

1. [`AGENTS.md`](AGENTS.md) — the agent contract. Loads automatically; the five non-negotiables and the role
   routing table.
2. [`documentation/README.md`](documentation/README.md) — the **routing map** for everything else. Route, don't
   read: find the section you need, open it, and stop.
3. [`CONTRIBUTING.md`](CONTRIBUTING.md) — branching, claiming, commits, PRs, and the release procedure.

The full **API reference** lives in [`MarqSpec.Client.ProjectX/README.md`](MarqSpec.Client.ProjectX/README.md).
That file ships inside the NuGet package, so a consumer on nuget.org reads the same text — keep it in lockstep
with the code.

## Layout

| Path | What |
|---|---|
| [`MarqSpec.Client.ProjectX/`](MarqSpec.Client.ProjectX/) | The library — REST client, hub client, models, DI |
| [`MarqSpec.Client.ProjectX.Tests/`](MarqSpec.Client.ProjectX.Tests/) | Unit tests — fully mocked, no I/O |
| [`MarqSpec.Client.ProjectX.Samples/`](MarqSpec.Client.ProjectX.Samples/) | Runnable console examples |
| [`MarqSpec.Client.ProjectX.Diagnostics/`](MarqSpec.Client.ProjectX.Diagnostics/) | Contract-search and connectivity troubleshooting |
| [`documentation/`](documentation/) | PRD (`R-#`), architecture, ADRs, role contracts |
| `swagger.json` | The gateway contract as published — **authoritative over any model or doc that disagrees** |

## Quick start

```bash
dotnet add package MarqSpec.Client.ProjectX
```

```csharp
using MarqSpec.Client.ProjectX.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProjectXApiClient(builder.Configuration);
```

Credentials bind from the `ProjectX` configuration section, from `dotnet user-secrets`, or from the
environment. Either naming convention works (R-1.2):

```bash
export ProjectX__ApiKey=your-api-key          # ASP.NET convention — preferred
export ProjectX__ApiSecret=your-api-secret

export PROJECTX_API_KEY=your-api-key          # legacy flat form — still supported
export PROJECTX_API_SECRET=your-api-secret
```

**Never put a credential in a tracked file.** `appsettings.json` is gitignored repo-wide for that reason; only
`*.example.json` templates are tracked.

> **The option names are the gateway's, and they are misleading.** `ApiKey` is transmitted as the gateway's
> `userName` field and `ApiSecret` as its `apiKey` field — so the wire field called `apiKey` carries your
> *secret*. See [ADR-0003](documentation/adr/0003-jwt-acquisition-and-cache.md).

Two things to know before you use it in anger:

- **`POST /api/Order/place` is never automatically retried.** The gateway offers no idempotency key, so a
  retried placement is a second live order. A transport fault on a placement is an *indeterminate* outcome, not
  a failure — your code owns the reconciliation
  ([ADR-0002](documentation/adr/0002-resilience-and-idempotency.md)).
- **`IProjectXWebSocketClient` is registered as a singleton**, deliberately, and consumers depend on that
  ([ADR-0004](documentation/adr/0004-hub-client-is-a-singleton.md)).

Runnable examples: [`MarqSpec.Client.ProjectX.Samples/`](MarqSpec.Client.ProjectX.Samples/).

## Development

```bash
dotnet build MarqSpec.Client.ProjectX.slnx
dotnet test
```

**That is the whole setup** — no credentials, no Docker, no network. Unit tests are fully mocked; the
integration tier starts a fake ProjectX gateway in-process on an ephemeral port and drives the client against it
over real HTTP and a real WebSocket, including both SignalR hubs
([ADR-0007](documentation/adr/0007-local-test-environment.md)).

Optional — for pointing the sample apps at a fake venue, or reproducing a container problem:

```bash
docker compose up -d fake-gateway
PROJECTX_FAKE_GATEWAY_URL=http://localhost:8080 dotnet test
```

And for a toolchain identical to CI's, which also sidesteps Windows blocking freshly built unsigned assemblies:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml run --rm sdk dotnet test
```

Tests against the **real** gateway are tagged `Category=Live`, run only when credentials are present, and are
never required for a green build.

Before a PR: `dotnet format --verify-no-changes`, and a clean `dotnet build -c Release` on **both** target
frameworks.

## Contributing

Read [`CONTRIBUTING.md`](CONTRIBUTING.md). In short: issue first, branch off `develop` as
`<type>/<issue-id>_<title>`, push the branch before you start, Conventional Commits, PR back into `develop`.
Promotion is one-way — `staging` ← `develop`, `main` ← `staging` — and **the git tag is the version**
([ADR-0006](documentation/adr/0006-tag-driven-versioning.md)).

## License

MIT — see [`LICENSE`](LICENSE) and [`NOTICE`](NOTICE).

Not affiliated with, endorsed by, or sponsored by ProjectX or TopstepX.
