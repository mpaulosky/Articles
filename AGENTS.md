# Agent instructions

## Process

Branches, worktrees, commits, PR titles and descriptions, merging and releases follow [`docs/PROCESS.md`](docs/PROCESS.md).
Name every branch to its standard (`feature/{issue}-{slug}`, `fix/{issue}-{slug}`, `hotfix/{issue}-{slug}` or `chore/{slug}`),
including in a cloud session: CI's **Branch name** check refuses any other name.

## Agent skills

### Issue tracker

Issues live in this repo's GitHub Issues (`mpaulosky/Articles`), managed via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical labels used as-is: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout: `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
