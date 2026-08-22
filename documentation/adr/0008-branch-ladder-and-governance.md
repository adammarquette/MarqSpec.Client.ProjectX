# ADR-0008 — Branch ladder, merge methods, and repo governance

**Status:** Accepted

## Context

The repository had **no branch protection at all**. `GET /repos/…/branches/master/protection` returned 404, and
the single ruleset, `master-codeowner-enforcement`, was created with `"enforcement": "disabled"` and never
switched on — its `created_at` and `updated_at` are the same second.

So `CODEOWNERS`, the PR template, and PR #4, whose entire stated purpose was "prevent commits to master", were
all decorative. Anything could be pushed directly to the default branch, force-pushed, or deleted.

The branch model was also the odd one out. Every other repo in the account uses `main` or the full
`develop → staging → main` ladder; this one used `master`, alone, with no `develop`. That matters because this
repo is pinned as a submodule into trading-copilot, whose contributors carry the parent's ladder in their heads.

Governance also has a shape problem: the parts that live in files (workflows, templates) are reviewable and
drift slowly, while the parts that live in GitHub settings (rulesets, merge methods, labels) are invisible in
the repo and drift silently. The disabled ruleset is exactly that failure.

## Decision

**Adopt trading-copilot's ladder, and enforce it with rulesets that constrain the merge method per rung.**

| Target | Allowed source | Merge method | Exception |
|---|---|---|---|
| `develop` | any `feature` / `bug` branch | **rebase only** | — |
| `staging` | `develop` only | **merge commit only** | `ladder-exception` label |
| `main` | `staging` only | **merge commit only** | none |

- `master` renamed to `main`; `develop` and `staging` created; **default branch is `develop`**.
- Squash-merge **disabled**; rebase-merge and merge commits enabled; auto-delete-on-merge enabled.
- `protect-develop` / `protect-staging` / `protect-main` rulesets, all `active`: pull request required,
  force-push and deletion blocked, no bypass actors. Required status checks are attached as they come to exist.
- The `work:*`, `Work Estimate: 1`–`5`, `safety-critical`, `epic`, `backlog` and `ladder-exception` label
  taxonomy is created to match the parent's, as **repo** labels so an agent reading the raw issue via `gh` sees
  them.

**Constraining the merge method is the part that carries weight.** "Curated commits into `develop`, merge
commits for promotions" is otherwise a rule people have to remember; as a ruleset it is a property of the
branch. A promotion is a merge commit by construction, and a feature landing cannot be one.

## Alternatives considered

**Keep `master` and add `develop`/`staging` above it.** Rejected: it keeps this repo the only one in the account
on `master`, which means the shared `CONTRIBUTING.md` and the shared `branch-policy.yml` would both need a
per-repo variant — and the whole point is that they should not.

**A two-rung `develop → main` ladder.** Genuinely tempting for a library with no deployed environment. Rejected
for template consistency: this repo is the shape the other supporting repos get copied from, and a rung is
cheap while a divergence in the template is not. `staging` here means "promoted, not yet released" — a real
state for a package.

**Classic branch protection instead of rulesets.** Rejected: rulesets express allowed merge methods, apply to
multiple refs, and are readable through the API as a unit.

**Require approving reviews.** Rejected: single operator. Requiring an approval that only the author can give
either blocks everything or trains people to bypass. The gate is *checks*, not approvals.

## Consequences

- Nothing reaches `main` except through `staging`, and nothing reaches `staging` except through `develop` or an
  explicitly labelled, auditable exception.
- **Existing external links to `master` redirect**, and open PRs were retargeted by the rename. The submodule
  pin in trading-copilot is a SHA, so the parent is unaffected.
- Until the CI work lands, `ci.yml` still triggers on `master`/`main`, so **a PR into `develop` gets no checks
  at all**. The rulesets require a PR but cannot require a check that does not exist yet. Transitional.
- **`staging` and `main` are permanently "ahead" of `develop`, and that is not drift.** A promotion's merge
  commit is created on the *target* branch, and the ladder is one-way, so `develop` never receives it. The gap
  grows by two commits per release and never resets — seven by the 2.1.0 release, one for each promotion — while
  the content stays identical. Commit counts are the wrong instrument here; compare trees:
  `git rev-parse origin/develop^{tree} origin/staging^{tree} origin/main^{tree} | sort -u | wc -l`, where `1`
  means all three hold the same content. **Do not back-merge `main` into `develop` to flatten the number.** It
  would contradict the one-way promotion this ADR exists to enforce, the ladder check would not stop it (only
  `staging` and `main` constrain their source), and it fixes a display rather than a problem.
- The repo-settings half of this decision — rulesets, merge methods, labels — **exists only in GitHub settings**.
  That is precisely how the previous ruleset came to be disabled and unnoticed, so it is recorded here, and a
  `bootstrap.sh` reproduces it for the next repo rather than leaving it to be re-clicked.

## Follow-ups

- Attach required status checks to all three rulesets once the CI work provides them.
- Two merged branches (`fix/enum-serialization`, `claude/exciting-feynman-6abtq4`) remain on origin and should
  be deleted. A third, `license-fix`, carries **one commit that never reached `main`** — it needs a decision
  before deletion, not a sweep.
