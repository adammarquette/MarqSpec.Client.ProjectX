# AGENTS.md — MarqSpec.Client.ProjectX (root)

Rules for **every** agent in this repository — the .NET client library for the ProjectX gateway (REST +
SignalR). It is consumed as a submodule by
[trading-copilot](https://github.com/adammarquette/trading-copilot), whose auto-flatten and risk gate sit on the
other side of this transport. Role- and subtree-specific rules live in their own contracts, so they cost context
only when they apply.

## Take your role's contract first

| If you are… | Read first | How it loads |
|---|---|---|
| writing library code or unit tests | [`MarqSpec.Client.ProjectX/AGENTS.md`](MarqSpec.Client.ProjectX/AGENTS.md) — Coding | on your first read of a file there |
| writing integration tests | [`IntegrationTests/AGENTS.md`](MarqSpec.Client.ProjectX.IntegrationTests/AGENTS.md) — QA | on your first read in that project |
| **reviewing any change** | [`agents/code-reviewer.md`](documentation/agents/code-reviewer.md) | **open it yourself** |
| **touching CI/CD, the image, compose, or release** | [`agents/platform.md`](documentation/agents/platform.md) | **open it yourself** |

The subtree contracts load by directory proximity — **lazily, when you first read a file there, not at session
start**. The role contracts follow *what you are doing* rather than where a file sits, and never auto-load.
**Wearing one of those hats without opening its contract is the most common way agents get this repo wrong.**

> Each `AGENTS.md` has a one-line `CLAUDE.md` beside it holding `@AGENTS.md`. **Those shims are load-bearing** —
> Claude Code reads `CLAUDE.md`, not `AGENTS.md`. Deleting one as "redundant" silently unloads that contract.

## What this repo is

A transport client, published as the NuGet package `MarqSpec.Client.ProjectX` and multi-targeting
`net8.0;net10.0`. Solution `MarqSpec.Client.ProjectX.slnx`: the library, its unit tests, its integration tests,
a `FakeGateway` that stands in for the venue locally, plus `Samples` and `Diagnostics` console apps. Build with
`dotnet build MarqSpec.Client.ProjectX.slnx`; before a PR, `dotnet format --verify-no-changes` and unit tests
green.

The surface is four things: `IProjectXApiClient` (REST facade over a Refit interface),
`IProjectXWebSocketClient` (the two SignalR hubs), `IAuthenticationService` (JWT acquisition and cache), and the
`AddProjectXApiClient` registration extension. Everything else is wire models and options.

## Source of truth

The markdown under [`documentation/`](documentation/) **and the GitHub issues and PRs** are the highest-level
source code of the system: the C# below is reconstructable from them. Read them as source and keep them
compiling. `R-#`, ADR numbers and `gh#N` are its symbol table.

**Route, don't read.** [`documentation/README.md`](documentation/README.md) maps every document — what it is and
when to open it. Resolve the section you need through it; **never load the corpus**.

[`AGENT-MEMORY.md`](documentation/AGENT-MEMORY.md) is the catch-all for practices with no formal home — check it
before starting, and add dated entries only when nothing formal fits.

## The five that are never traded away

- **No secrets in source.** Credentials arrive through the Options pattern and environment; never a literal,
  never a tracked `appsettings.json`, never a log line. This is a **public** repository.
- **The client transmits; it does not decide.** Risk limits, sizing, eligibility and the decision to flatten
  belong to the consumer. This library holds no limit and refuses no order on policy grounds — a refusal here
  would put an enforcement point *below* the parent's gate, in the wrong repository, where its gate cannot see
  it. Transport-level refusals (a malformed request, a missing credential) are not policy and are fine.
- **Idempotency is a property of the endpoint, not of the retry policy.** `POST /api/Order/place` is not
  idempotent: a retried "timeout" can place a second live order. Anything that resends must prove the endpoint
  tolerates it — see [ADR-0002](documentation/adr/0002-resilience-and-idempotency.md).
- **Test-first is the Definition of Done.** No new public method without a failing test written first; the paths
  the parent's safety-critical code sits on — order placement, position close, hub reconnect, token refresh —
  carry high-rigor suites.
- **Wear a hat, open its contract** — before you start, not after.

## Working rules

- **Docs in lockstep — the same-PR rule.** A change whose behavior, API or configuration a document describes
  updates **the affected section of that document, in the same PR** — the PRD (`R-#`), the architecture doc, the
  ADRs, the library README that ships inside the package, this file. Update the section, not the whole file.
- **Issue-first — no orphaned PRs.** Every PR cites an issue opened before it (`Closes #N` / `Related to #N`);
  cite issues as `gh#N`. **Task specs and acceptance criteria belong in the issue**, never as files under
  `documentation/` — a parallel spec duplicates the tracker and drifts from it.
- **Maximal metadata on every issue and PR:** assignee, milestone, `work:*` and `Work Estimate` labels. Issues
  are the board cards; a PR is not carded. Epics decompose into sub-issues (issue→issue). A thin issue is a
  defect — the next agent rebuilds context from these fields.
- **Commits:** Conventional Commits, plus **both** an `Assisted-by:` and a `Co-Authored-By:` trailer on
  AI-authored changes. Full type list: [`CONTRIBUTING.md`](CONTRIBUTING.md).
- **Branch off `develop` and PR back into it.** `develop` is the sole integration branch, never a workspace.
  Promotion is one-way with one source per step: `staging` ← `develop`, `main` ← `staging`. Never branch off or
  PR into `main`. Name branches `<type>/<work-item-id>_<title>`. **A release is cut on `main`, and the tag is the
  version** — nothing declares a version in a file. Ladder detail: [`CONTRIBUTING.md`](CONTRIBUTING.md).
- **Work in a `git worktree`, never in the main checkout** — `git worktree add .worktrees/<branch> <branch>`.
  Sessions run in parallel; sharing a working tree means one session's uncommitted edits land in another's
  commit.
- **Claim before you start — `scripts/claim.sh <issue-id>`.** The **pushed** branch is the claim; a local
  worktree is invisible to parallel sessions. A tip unmoved for 4 hours is fair game — say so on the issue
  first. **This repo is one half of a two-repo card**: work here is often driven by an issue in trading-copilot,
  so check both trackers before assuming a card is free.

*Every line here is paid by every agent in every session. Keep it small: anything role- or subtree-specific
belongs in its contract, and anything with a formal home belongs there rather than restated here.*
