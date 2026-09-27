# Contributing to Articles

Thank you for your interest in contributing. Articles is a .NET 10 Blazor web
application, orchestrated with .NET Aspire, that lets authors create, edit, and
publish articles. This guide covers how to set up the repo, where things live,
and how a change travels from an issue to `main`.

## Initial setup

### 1. Prerequisites

- .NET 10 SDK (the exact version is pinned in [global.json](../global.json))
- `git` and the GitHub CLI (`gh`)
- Docker, for the integration and E2E tests (TestContainers) and for running the
  AppHost
- Node.js with `npx`, for Markdown lint
- Optional: `yamllint` (the pre-push hook falls back to Docker when it's missing)

### 2. Clone, restore, and enable the hooks

```bash
git clone https://github.com/mpaulosky/Articles.git
cd Articles
git config core.hooksPath .github/hooks
dotnet restore Articles.slnx
```

After the first checkout, the `post-checkout` hook keeps `core.hooksPath` set
and makes sure the hooks are executable.

### 3. Run the app

Always start the app through the Aspire AppHost. The Web project doesn't serve
correctly on its own because the AppHost provides its dependencies.

```bash
dotnet run --project src/AppHost
```

## Repository layout

| Path | Contents |
| ---- | -------- |
| [src/AppHost](../src/AppHost) | .NET Aspire orchestration: the entry point for running the app |
| [src/Web](../src/Web) | Blazor web application |
| [src/Domain](../src/Domain) | Domain model and business rules |
| [src/ServiceDefaults](../src/ServiceDefaults) | Shared Aspire service defaults (telemetry, health checks, resilience) |
| [tests](../tests) | Test projects: `AppHost`, `Architecture`, `Domain`, `Web` (unit), `Web.UI` (bUnit), `Web.Integration`, and `Web.E2E` (Playwright) |
| [testutils/Web.TestData](../testutils/Web.TestData) | Shared test data used across test projects ([ADR 0002](adr/0002-shared-test-data-outside-tests-folder.md)) |
| [CONTEXT.md](../CONTEXT.md) | Domain glossary: the shared vocabulary for the project |
| [docs/adr](adr) | Architecture decision records |
| [.github/instructions](../.github/instructions) | Coding, commit, and documentation conventions |
| [.github/hooks](../.github/hooks) | Local git hooks (`pre-commit`, `pre-push`, `post-checkout`) |
| [.github/workflows](../.github/workflows) | CI, lint, triage, auto-merge, and release automation |

## How a change gets to `main`

### 1. Start from an issue

Work is tracked in [GitHub Issues](https://github.com/mpaulosky/Articles/issues).
Open or pick an issue before starting. Triage uses these labels:

- `needs-triage`: a maintainer needs to evaluate it
- `needs-info`: waiting on the reporter
- `ready-for-agent`: fully specified and ready for an AFK agent
- `ready-for-human`: needs a human to implement it
- `wontfix`: will not be actioned

If a change introduces or renames a domain concept, update
[CONTEXT.md](../CONTEXT.md). If it makes a significant architectural decision,
record it as a new ADR in [docs/adr](adr).

### 2. Branch from an up-to-date `main`

```bash
git checkout main
git pull origin main
git checkout -b squad/42-fix-login-validation
```

The pre-push hook rejects pushes from `main`, `preview`, and `dev`, and from
any branch that doesn't match one of these patterns:

| Pattern | Use for |
| ------- | ------- |
| `squad/{issue-number}-{kebab-slug}` | Work tied to an issue (the default) |
| `sprint/{n}-{kebab-slug}` | Sprint-scoped work |
| `hotfix/{kebab-slug}` | Urgent fixes |
| `chore/{kebab-slug}` | Maintenance, docs, and tooling changes |

### 3. Make the change, test-first

- Write or update tests alongside every behaviour change, ideally first.
- Place tests in a folder structure that mirrors the source project, so it's
  obvious what each test covers. A test for `src/<Project>/<path>/Foo.cs`
  goes in `tests/<Project>.Tests/<path>/FooTests.cs`.
- Use xUnit v3 for tests, bUnit for Blazor components, TestContainers for
  integration tests, and Playwright for end-to-end tests.
- Follow the conventions in [.github/instructions](../.github/instructions) and
  the repo's [.editorconfig](../.editorconfig).

### 4. Validate locally

```bash
# Build
dotnet build Articles.slnx --configuration Release

# Test each project (running against the .slnx can report "zero tests ran"
# under Microsoft Testing Platform, so the hook runs per project)
for p in tests/*/*.csproj; do dotnet test "$p" --configuration Release; done

# Lint Markdown
npx --yes markdownlint-cli2 "**/*.md"

# Lint YAML (if you changed workflows); uses Docker when yamllint isn't installed
if command -v yamllint >/dev/null; then
  yamllint -c .yamllint.yml .github/workflows
else
  docker run --rm -v "$PWD:/work" -w /work cytopia/yamllint:latest \
    -c .yamllint.yml .github/workflows
fi
```

### 5. Commit

Commit messages follow Conventional Commits, as described in
[git-commit-instructions.md](../.github/instructions/git-commit-instructions.md):

```text
<type>(<scope>): <short imperative summary>
```

Types include `feat`, `fix`, `docs`, `test`, `refactor`, `build`, `ci`, and
`chore`. The scope is the affected project or area, such as `Web`, `Domain`, or
`docs`. The `pre-commit` hook lints staged Markdown files only when it finds a
`markdownlint` binary (installed globally or in `node_modules`). Otherwise it
prints a warning and skips the check, so run the `npx` command from step 4 yourself.

### 6. Push once and open a PR to `main`

```bash
git push -u origin squad/42-fix-login-validation
gh pr create --base main
```

On push, the `pre-push` hook runs these gates:

- branch-name validation,
- YAML lint on changed `.yml`/`.yaml` files,
- Markdown lint on changed `.md` files,
- `dotnet test` for every test project under `tests/`.

Tag-only pushes skip the gates. Avoid `git push --no-verify`. Fix the reported
problem instead.

This repo has no `dev` or `preview` staging branch: every PR targets `main`
directly. A good PR description covers:

- what changed,
- why it changed,
- what validation was run.

### 7. CI, auto-merge, and the release blog

- **CI** ([squad-ci.yml](../.github/workflows/squad-ci.yml)) builds the
  solution, runs every test project, and reports coverage. The coverage target
  is at least 80% line coverage.
- **Auto-merge** ([squad-pr-automerge.yml](../.github/workflows/squad-pr-automerge.yml))
  enables squash auto-merge on same-repo PRs, so a PR merges as soon as its
  checks pass. Once your branch is pushed, don't keep committing to it. Put
  follow-up work on a new branch from `main` after the PR merges, or the
  branch can silently diverge from `main`.
- **Release blog**: when a release-eligible PR merges, the release workflow
  ([squad-release.yml](../.github/workflows/squad-release.yml)) publishes a
  release. If that produces changes under [docs/blogs](blogs) or the READMEs,
  it opens a follow-up `docs: add release blog for PR #N [skip-release]` PR.
  PRs with `[skip-release]` in the title (including the blog PRs themselves)
  don't trigger a release. When a blog PR is opened, `main` isn't fully caught
  up until it has merged too.

### 8. Clean up

After both PRs have merged:

```bash
git checkout main
git pull origin main
git branch -d squad/42-fix-login-validation
```

Stale branches and worktrees can be pruned with
[scripts/squad/cleanup-squad-branches.sh](../scripts/squad/cleanup-squad-branches.sh).
The same cleanup also runs nightly in CI.

## Code standards

- Keep changes focused and easy to review. Prefer several small PRs to one
  large one.
- Use the terms from [CONTEXT.md](../CONTEXT.md) in code and docs.
- Follow standard .NET naming conventions and keep C# readable.
- Add or update tests with every behaviour change.
- Manage package versions centrally in
  [Directory.Packages.props](../Directory.Packages.props). Don't put versions in
  individual `.csproj` files.

## Resources

- [README.md](../README.md): project purpose and overview
- [CONTEXT.md](../CONTEXT.md): domain glossary
- [docs/adr](adr): architecture decision records
- [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)
- [SECURITY.md](SECURITY.md)

## Questions?

Open an [issue](https://github.com/mpaulosky/Articles/issues) or reach out to
[@mpaulosky](https://github.com/mpaulosky).
