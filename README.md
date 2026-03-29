# MarqSpec.Client.ProjectX — Solution README

A .NET client library and sample applications for the ProjectX REST + WebSocket APIs.

This repository contains:
- [`MarqSpec.Client.ProjectX/`](MarqSpec.Client.ProjectX/) — Library implementing REST client, WebSocket client, models and DI helpers. See the [library README](MarqSpec.Client.ProjectX/README.md) for full API reference and usage docs.
- [`MarqSpec.Client.ProjectX.Samples/`](MarqSpec.Client.ProjectX.Samples/) — Console sample(s) demonstrating REST + real-time WebSocket usage. See the [samples README](MarqSpec.Client.ProjectX.Samples/README.md).
- [`MarqSpec.Client.ProjectX.Diagnostics/`](MarqSpec.Client.ProjectX.Diagnostics/) — Diagnostic tools for troubleshooting API contract search and connectivity. See the [diagnostics README](MarqSpec.Client.ProjectX.Diagnostics/README.md).
- [`MarqSpec.Client.ProjectX.Tests/`](MarqSpec.Client.ProjectX.Tests/) — Unit and integration tests.
- `PRD.md` — Product Requirements Document (kept at solution root).

Supported target framework
- .NET 10 (C# 14)

Quick start — consumers
1. Add the package to your project (when published):

```bash
dotnet add package MarqSpec.Client.ProjectX
```

2. Register services in `Program.cs`:

```csharp
using MarqSpec.Client.ProjectX.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProjectXApiClient(builder.Configuration);
var app = builder.Build();
```

3. Configure credentials via environment variables or `appsettings.json`:
- `PROJECTX_API_KEY`
- `PROJECTX_API_SECRET`

4. See `MarqSpec.Client.ProjectX.Samples/` for runnable examples:

```bash
cd MarqSpec.Client.ProjectX.Samples
dotnet run
```

Developer notes — contributors
- Build and run tests:

```bash
dotnet build
dotnet test --filter "Category!=Integration"
```

- Run integration tests (requires live API credentials):

```bash
# Set credentials via environment variables
export PROJECTX_API_KEY=your-api-key
export PROJECTX_API_SECRET=your-api-secret
dotnet test --filter "Category=Integration"
```

- Coding standards:
  - Target .NET 10
  - Follow existing project conventions and XML documentation comments
  - All public async methods must accept `CancellationToken`
  - Use `ILogger<T>` for logging; do not log credentials

- Running diagnostics: see `MarqSpec.Client.ProjectX.Diagnostics`.

Documentation and references
- Primary library README: `MarqSpec.Client.ProjectX/README.md`
- Detailed docs and summaries: `docs/README.md` (index)
- Solution-level documentation appears under Solution Items → Summary in Visual Studio

Contributing
- Fork → feature branch → PR with tests and description
- Ensure unit coverage and run integration tests where applicable

License and support
- Proprietary — Marquette Speculations
- For issues and questions, contact the development team (refer to repo issues)

