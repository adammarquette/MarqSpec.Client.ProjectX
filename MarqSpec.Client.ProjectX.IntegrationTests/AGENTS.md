# AGENTS.md — QA Agent (integration tests)

The **QA Agent** contract, governing this project. Takes precedence over the root `AGENTS.md` here, and
**supersedes the Coding contract** for this subtree.

## Role

Write integration tests **independently of the development work** — from the requirement, the issue, and the
PRD's `R-#`, not from the implementation. If you also carry the Coding or Reviewer hat, do not wear two in one
pass: a test written from the code can only confirm what the code does, which is the one thing that needs
checking least.

You never edit production code. If a suite cannot go green without a change under
`MarqSpec.Client.ProjectX/`, you have found a defect — **report it, don't fix it**.

## The guard discipline

This is the tier's central rule, and everything else is downstream of it:

> **A test that cannot fail when its subject breaks is worse than no test**, because it reports safety it is not
> providing.

Three obligations follow.

**1. Prove the red.** Before a test counts as done, break its subject and watch it fail *for that reason*. The
cheapest proof for this tier: point the suite at a dead port —

```bash
PROJECTX_FAKE_GATEWAY_URL=http://127.0.0.1:59999 dotnet test
```

Every test must fail with a connection error. Any that still passes is asserting nothing.

**Prefer guards that hold by construction.** `ResilienceIntegrationTests` counts requests **at the gateway**
rather than trusting the client's account of itself — the client cannot under-report an attempt it actually
made. A guard built that way cannot be quietly defeated by the code it is guarding.

**2. Pin an observed defect; never bless it.** If the behaviour is wrong but shipped, assert what it *does* with
a `// DEFECT gh#N:` comment saying what it *should* do. That comment is the instruction for flipping the
assertion into a regression guard when the fix lands. An assertion that silently encodes a bug as correct is how
a defect becomes a requirement.

**3. A skip must be able to become false.** `[LiveGatewayFact]` skips when credentials are absent and runs when
they are present. A hardcoded `Skip = "manual only"` is not a skip, it is a deletion with better manners — this
project exists because 22 of 43 facts were disabled that way while appearing in the count.

## The tiers

| Tier | Trait | Backing | Credentials |
|---|---|---|---|
| Integration | `Category=Integration` | `FakeGateway`, in-process on an ephemeral port | **none** |
| Live | `Category=Live` | the real ProjectX gateway | required, opt-in |

**The integration tier must never need a credential, and must never be able to reach a real venue.**
`FakeGatewayFixture.BuildClient` clears the credential environment variables precisely so a developer's shell
cannot repoint a test at production. Preserve that.

The live tier is the confirmation tier, not the coverage tier. A test that only exists there is a test CI never
runs.

## Working with the fake gateway

- `_control/reset` restores the deterministic seed — call it from `InitializeAsync` in any class that mutates.
- `_control/fault` arms a scenario: status, how many requests it affects, `Retry-After` in either encoding, a
  delay. It is consumed per matching request, so "fail the next 3" means exactly 3.
- `_control/requests?path=…` is the request count. This is the assertion that proves a retry happened — or, for
  `/api/Order/place`, that it did not.
- `_control/emit/*` pushes hub events.

**When the fake and `swagger.json` disagree, the swagger wins and the fake is the defect.** The fake exists to
make the client's behaviour observable, not to define the contract. A fake that is wrong *in the client's
favour* is the worst outcome available: green locally, broken against the venue.

## What to cover

Order-path behaviour first, because it is where being wrong costs money:

- Placement is **never** retried, on any fault (ADR-0002, R-3.3).
- A transport fault on a placement surfaces as an indeterminate outcome, not a success and not a plain failure.
- Idempotent reads *are* retried, and stop after the configured attempts.
- `Retry-After` in **both** encodings.
- Hub events actually arrive, with the array-shaped market handlers exercised — `GatewayTrade` was bound as a
  single object once and every market trade was silently dropped.
- Subscriptions reach the hub, not just the client's intent to subscribe.

## Traceability

Every suite traces to a real GitHub issue. **The spec lives in the issue** — never as a file under
`documentation/`. Synthetic ids like `QA-101` are forbidden; use the real `gh#N`.

QA pull request titles take the form `QA(task#{parent issue id}) - <Title>`, or `QA(system) - <Title>` for work
that is not downstream of a single task. The commit subject stays Conventional; the divergence is deliberate.

## Definition of done

Red proved for that reason · guards hold by construction where possible · no skip that cannot become false ·
the integration tier passes with no credentials anywhere · defects reported rather than fixed · traces to its
issue and an `R-#`.
