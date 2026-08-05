# Agent memory

The catch-all for things that must persist across sessions but **don't fit any other formal document**. It is
deliberately informal, and it is *overflow* — not a substitute for the PRD, an ADR, or a role contract.

**How to use it**

1. **Read it before starting work.** It's short; that's the point.
2. **Append, don't overwrite.** Date every entry `YYYY-MM-DD`.
3. **Promote when it grows up.** When an entry earns a formal home — an ADR, a requirement, a contract — move it
   there and leave a one-line pointer behind.
4. **Keep entries terse.** A paragraph, not an essay.

---

## Practices to follow

**2026-08-05 — Work in a `git worktree`, never in the main checkout.** Sessions run in parallel; sharing a
working tree means one session's uncommitted edits land in another's commit. `scripts/claim.sh` creates one for
you.

**2026-08-05 — When the swagger and a model disagree, the swagger wins.** `swagger.json` at the repo root is the
gateway contract as published. A model that differs is the defect, not the swagger. Field names have moved
before — `startTimestamp` / `endTimestamp` in #15.

**2026-08-05 — Two things that look like bugs and are load-bearing.** Before "fixing" either, read why:
enums serialize as **integers** (a string-enum converter breaks every enum-carrying request — #14,
[ADR-0001](adr/0001-refit-typed-rest-client.md)), and `ProjectXOptions.ApiKey` is transmitted as the gateway's
`username` field ([ADR-0003](adr/0003-jwt-acquisition-and-cache.md)).

**2026-08-05 — This repo is one half of a two-repo card.** Work here is usually driven by an issue in
trading-copilot. Check both trackers before assuming a card is free, and remember that a clean `main` here reads
as *free* precisely when someone has *finished* — in-review work lives on a branch.
(Detail: [`CONTRIBUTING.md`](../CONTRIBUTING.md).)

## Environment notes

**2026-08-05 — PowerShell 5.1 strips embedded double quotes from native-command arguments.** A
`gh api --jq '... "foo" ...'` invocation arrives at `gh` with the inner quotes gone and fails to parse. Fetch the
raw JSON and pipe it to `ConvertFrom-Json` instead, or pass the payload via `--input <file>`. The same applies to
`git commit -m` with a multi-line message containing quotes — use `git commit -F <file>`.

**2026-08-05 — `git status` can be confidently wrong after a stale fetch.** A checkout whose remote-tracking refs
have not been updated reports "up to date" against a branch that has moved. `git fetch --prune` before believing
it — the submodule checkout of this repo inside trading-copilot was four merged PRs behind while reporting clean.

## Notes & communications

*(nothing outstanding)*
