# Documentation

**This directory is authoritative. Do not read it wholesale.** Find the document you need here, open **the
section you need**, and stop. `R-#`, ADR numbers and `gh#N` are the symbol table — resolve symbols on demand, the
way a compiler does, rather than loading every source file.

Sizes are approximate tokens, so you can see what a read costs before you pay for it.

## Start here

| Document | ~tok | Read it when |
|---|---|---|
| [`projectx-client-prd.md`](projectx-client-prd.md) | 2.2K | You need to know **what is required** — or you are citing an `R-#` in a PR, test or ADR. Requirement ids are stable and never renumbered. |
| [`projectx-client-architecture.md`](projectx-client-architecture.md) | 2.0K | Before changing the request pipeline, the auth flow, hub lifetimes, or anything about DI registration. Read *The request pipeline* before touching resilience. |
| [`adr/`](adr/README.md) | index 0.4K | You are about to change something and want to know whether the current shape was chosen or inherited. **Never read the folder** — resolve the number. |

## Working agreements

| Document | ~tok | Read it when |
|---|---|---|
| [`AGENT-MEMORY.md`](AGENT-MEMORY.md) | 0.7K | **Before starting any work.** Cheap; just read it. |
| [`agents/`](agents/README.md) | index 0.5K | You are wearing a role hat. Reviewer and Platform contracts **never auto-load** — open them yourself. |
| [`../CONTRIBUTING.md`](../CONTRIBUTING.md) | 2.0K | Branching, claiming, commits, PRs, **and the release procedure**. |
| [`../AGENTS.md`](../AGENTS.md) | 1.2K | Loads automatically. The five non-negotiables and the role routing table. |

## Decisions — `adr/`

Nygard form, `NNNN-slug.md`. Once **Accepted**, the decision is immutable: a later ADR **supersedes** it, and
nothing is rewritten in place. Records grow by dated `## Update` entries under a `## Decision log`;
`## Follow-ups` stays last.

| ADR | ~tok | Open it when |
|---|---|---|
| [0001](adr/0001-refit-typed-rest-client.md) | 0.5K | Changing the REST interface, the facade, or serializer settings |
| [0002](adr/0002-resilience-and-idempotency.md) | 0.9K | **Changing anything about retry.** The order-placement exclusion lives here |
| [0003](adr/0003-jwt-acquisition-and-cache.md) | 0.7K | Touching auth, token lifetime, or the `ApiKey`-is-a-username trap |
| [0004](adr/0004-hub-client-is-a-singleton.md) | 0.5K | Changing a DI lifetime — the consumer depends on the singleton |
| [0005](adr/0005-multi-targeting.md) | 0.4K | Adding a dependency, or wondering why `net8.0` is still here |
| [0006](adr/0006-tag-driven-versioning.md) | 0.6K | Cutting a release, or looking for `<Version>` and not finding it |
| [0007](adr/0007-local-test-environment.md) | 0.8K | Working on the fake gateway, compose, or the integration tier |
| [0008](adr/0008-branch-ladder-and-governance.md) | 0.7K | Anything about branches, merge methods, rulesets or labels |
| [0011](adr/0011-trade-direction-is-nullable.md) | 0.7K | Changing `TradeLogType` or market-hub `ContractId` stamping |

## Role contracts — `agents/`

Loaded **on demand by role**, not by directory.

| Contract | ~tok | Open it when |
|---|---|---|
| [`code-reviewer.md`](agents/code-reviewer.md) | 0.9K | You are reviewing a change — **anywhere** in the repo |
| [`platform.md`](agents/platform.md) | 0.9K | You are touching CI/CD, compose, the fake-gateway image, packaging, or the release path |

Two more contracts load by directory proximity instead, from the subtree they govern: the **Coding** contract at
[`MarqSpec.Client.ProjectX/AGENTS.md`](../MarqSpec.Client.ProjectX/AGENTS.md) and the **QA** contract at
[`MarqSpec.Client.ProjectX.IntegrationTests/AGENTS.md`](../MarqSpec.Client.ProjectX.IntegrationTests/AGENTS.md).
[`agents/README.md`](agents/README.md) explains why the four are split that way.

## What is not here

- **Task specs and acceptance criteria.** They live in the **GitHub issue**. A spec under `documentation/`
  duplicates the tracker and drifts from it.
- **API reference and usage examples.** They live in
  [`../MarqSpec.Client.ProjectX/README.md`](../MarqSpec.Client.ProjectX/README.md), which ships inside the NuGet
  package — so a consumer reading it on nuget.org sees the same text. Keep it in lockstep with the code.
- **The gateway contract.** That is `swagger.json` at the repo root, and it is authoritative over any model or
  document that disagrees with it.

---
*Adding a document? Add its row here in the same PR — a document nothing routes to is a document nobody opens.*
