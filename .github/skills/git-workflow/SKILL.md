---
name: "git-workflow"
description: "Standardize Squad's topology-aware branching, PR, worktree, and cleanup workflow. Use when managing issue branches, worktrees, PRs, merge cleanup, or syncing base branches after cleanup."
license: MIT
metadata:
  version: "1.0"
domain: "version-control"
confidence: "high"
source: "team-decision"
---

## Context

Squad supports two branch topologies. Pick the one your repo uses and follow it consistently.

## Supported Branch Topologies

### Topology A: Release pipeline (`main` / `dev` / `insiders`)

| Branch | Purpose | Publishes |
|--------|---------|-----------|
| `main` | Released, tagged, in-npm code only | `npm publish` on tag |
| `dev` | Integration branch — all feature work lands here | `npm publish --tag preview` on merge |
| `insiders` | Early-access channel — synced from dev | `npm publish --tag insiders` on sync |

**Feature branch base:** `dev`  
**PR base:** `dev`

### Topology B: Mainline + PR branches (`main` + `squad/*`)

| Branch | Purpose |
|--------|---------|
| `main` | Default integration branch and release line |
| `squad/*` | Issue/feature branches opened as PRs |

**Feature branch base:** `main`  
**PR base:** `main`

## Branch Naming Convention

Issue branches MUST use: `squad/{issue-number}-{kebab-case-slug}`

Examples:
- `squad/195-fix-version-stamp-bug`
- `squad/42-add-profile-api`

## Workflow for Issue Work

1. **Preflight checks:**
   - Confirm the repo remote exists and is correct (`git remote -v` / `git remote get-url origin`).
   - Confirm `gh` auth/status (`gh auth status`).
   - Ensure the working tree is clean before branch or worktree operations (`git status --short`).
   - Fetch the latest base branch before branching (`git fetch origin {base-branch}`).

   ### Existing branch/PR guard
   - If the issue branch already exists locally or on the remote, stop and inspect before creating a duplicate.
   - If a PR already exists for the same branch/issue, do not create another one; reuse or inspect it.

2. **Set your topology base branch:**
   - Topology A base branch: `dev`
   - Topology B base branch: `main`

3. **Branch from your topology base branch:**
   ```bash
   git fetch origin
   git switch {base-branch}
   git pull --ff-only origin {base-branch}
   git switch -c squad/{issue-number}-{slug}
   ```

4. **Push the branch before opening the PR:**
   ```bash
   git push -u origin squad/{issue-number}-{slug}
   ```

5. **Mark issue in-progress:**
   ```bash
   gh issue edit {number} --add-label "status:in-progress"
   ```

6. **Create a draft PR targeting your topology PR base:**
   - Topology A: `--base dev`
   - Topology B: `--base main`
   ```bash
   gh pr create --base {pr-base} --title "{description}" --body "Closes #{issue-number}" --draft
   ```

7. **Do the work.** Make changes, write tests, commit with issue reference.

8. **Run pre-merge validation before cleanup and before merge readiness:** use the repo-standard commands in this order: YAML lint → Markdown lint → build → tests. Examples:
   - YAML lint: `npm run lint` or the repo's documented YAML lint equivalent
   - Markdown lint: the repo's documented Markdown lint command
   - Build: `npm run build` or the repo's documented build equivalent
   - Tests: `npm test`, `dotnet test`, or the repo's documented test equivalent

9. **Push updates and mark ready:**
   ```bash
   git push origin squad/{issue-number}-{slug}
   gh pr ready
   ```
   - Do not mark the PR ready or proceed to cleanup until required checks are green.

10. **After merge to your topology PR base and passing the Pre-Cleanup PR Gate:**
   ```bash
   git checkout {base-branch}
   git pull origin {base-branch}
   git branch -d squad/{issue-number}-{slug}
   git push origin --delete squad/{issue-number}-{slug}
   ```
   - Topology A only: update `main` from `origin/main` after cleanup. This refreshes the local branch from the remote; the actual promotion from `dev` to `main` remains a separate manual/PR-based step.
    ```bash
    git checkout main
    git pull origin main
    ```

### Pre-Cleanup PR Gate (Required)

Before deleting any `squad/*` branch or related worktree metadata in either topology (A: `dev`, B: `main`), verify PR state from the branch head:

```bash
BRANCH="squad/{issue-number}-{slug}"
gh pr list --head "$BRANCH" --state all --json number,state,isDraft,mergedAt,baseRefName,url
```

Decision logic:
- If matching PR is **open** or **draft** (`state=open` or `isDraft=true`) → **do not delete** branch/worktree.
- If matching PR is **closed but not merged** (`state=closed` and `mergedAt=null`) → **do not auto-delete** branch/worktree.
- If matching PR is **merged** (`mergedAt` has a timestamp) → safe to delete branch/worktree.
- If **no PR is found** for the branch head, or **more than one matching PR** is returned → **do not auto-delete**; manual review required.

## Parallel Multi-Issue Work (Worktrees)

When the coordinator routes multiple issues simultaneously (e.g., "fix bugs X, Y, and Z"), use `git worktree` to give each agent an isolated working directory. No filesystem collisions, no branch-switching overhead.

### When to Use Worktrees vs Sequential

| Scenario | Strategy |
|----------|----------|
| Single issue | Standard workflow above — no worktree needed |
| 2+ simultaneous issues in same repo | Worktrees — one per issue |
| Work spanning multiple repos | Separate clones as siblings (see Multi-Repo below) |

### Setup

From the main clone, choose the correct base branch:
- Topology A: `origin/dev`
- Topology B: `origin/main`

```bash
# Ensure base branch is current
git fetch origin {base-branch}

# Create a worktree per issue — siblings to the main clone
git worktree add ../squad-195 -b squad/195-fix-stamp-bug origin/{base-branch}
git worktree add ../squad-193 -b squad/193-refactor-loader origin/{base-branch}
```

**Naming convention:** `../{repo-name}-{issue-number}` (e.g., `../squad-195`, `../squad-pr-42`).

Each worktree:
- Has its own working directory and index
- Is on its own `squad/{issue-number}-{slug}` branch from your topology base branch
- Shares the same `.git` object store (disk-efficient)

### Per-Worktree Agent Workflow

Each agent operates inside its worktree exactly like the single-issue workflow:

```bash
cd ../squad-195

# Work normally — commits, tests, pushes
git add -A && git commit -m "fix: stamp bug (#195)"
git push -u origin squad/195-fix-stamp-bug

# Create PR targeting topology base
# Topology A: --base dev
# Topology B: --base main
gh pr create --base {pr-base} --title "fix: stamp bug" --body "Closes #195" --draft
```

All PRs target the topology PR base (`dev` in A, `main` in B). Agents never interfere with each other's filesystem.

### .squad/ State in Worktrees

The `.squad/` directory exists in each worktree as a copy. This is safe because:
- `.gitattributes` declares `merge=union` on append-only files (history.md, decisions.md, logs)
- Each agent appends to its own section; union merge reconciles on PR merge to the topology PR base
- **Rule:** Never rewrite or reorder `.squad/` files in a worktree — append only

### Cleanup After Merge

After a worktree's PR is merged to the topology PR base **and passes the Pre-Cleanup PR Gate**:

```bash
# From the main clone
# Safety: only remove the worktree after it is clean and the PR has already been merged.
git worktree remove ../squad-195
git worktree prune          # clean stale metadata
git branch -d squad/195-fix-stamp-bug
git push origin --delete squad/195-fix-stamp-bug
```

Note: If `git worktree remove` fails, inspect the worktree state before using `--force`.

Topology A only: update `main` from `origin/main` after cleanup. This refreshes the local branch from the remote; the actual promotion from `dev` to `main` remains a separate manual/PR-based step.
```bash
git checkout main
git pull origin main
```

If a worktree was deleted manually (rm -rf), `git worktree prune` recovers the state.

---

## Multi-Repo Downstream Scenarios

When work spans multiple repositories (e.g., squad-cli changes need squad-sdk changes, or a user's app depends on squad):

### Setup

Clone downstream repos as siblings to the main repo:

```
~/work/
  squad-pr/          # main repo
  squad-sdk/         # downstream dependency
  user-app/          # consumer project
```

Each repo gets its own issue branch following its own naming convention. If the downstream repo also uses Squad conventions, use `squad/{issue-number}-{slug}`.

### Coordinated PRs

- Create PRs in each repo independently
- Link them in PR descriptions:
  ```
  Closes #42

  **Depends on:** squad-sdk PR #17 (squad-sdk changes required for this feature)
  ```
- Merge order: dependencies first (e.g., squad-sdk), then dependents (e.g., squad-cli)

### Local Linking for Testing

Before pushing, verify cross-repo changes work together:

```bash
# Node.js / npm
cd ../squad-sdk && npm link
cd ../squad-pr && npm link squad-sdk

# Go
# Use replace directive in go.mod:
# replace github.com/org/squad-sdk => ../squad-sdk

# Python
cd ../squad-sdk && pip install -e .
```

**Important:** Local links are for testing only and must be removed before the final commit/push. `npm link` and `go replace` are dev-only — CI must use published packages or PR-specific refs.

### Worktrees + Multi-Repo

These compose naturally. You can have:
- Multiple worktrees in the main repo (parallel issues)
- Separate clones for downstream repos
- Each combination operates independently

---

## Anti-Patterns

- ❌ Branching from the wrong base branch for your topology (A: `dev`, B: `main`)
- ❌ PR targeting the wrong base branch for your topology (A: `dev`, B: `main`)
- ❌ Non-conforming branch names (must be squad/{number}-{slug})
- ❌ Committing directly to shared integration branches (A: `dev`/`main`, B: `main`) — use PRs
- ❌ Switching branches in the main clone while worktrees are active (use worktrees instead)
- ❌ Using worktrees for cross-repo work (use separate clones)
- ❌ Leaving stale worktrees after PR merge (clean up immediately)
- ❌ Deleting branches/worktrees without verifying PR existence and merged state first

## Promotion Pipeline

Applies to **Topology A** repos:
- dev → insiders: Automated sync on green build
- dev → main: Manual merge when ready for stable release, then tag
- Hotfixes: Branch from main as `hotfix/{slug}`, PR to dev, cherry-pick to main if urgent
