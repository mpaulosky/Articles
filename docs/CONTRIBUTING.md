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
- Node.js and pnpm (`corepack enable`), for the Tailwind CSS build and Markdown lint
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
| [docs/decisions.md](decisions.md) | Smaller lasting decisions |
| [.github/instructions](../.github/instructions) | Coding, commit, and documentation conventions |
| [.github/hooks](../.github/hooks) | Local git hooks (`pre-commit`, `pre-push`, `post-checkout`) |
| [.github/workflows](../.github/workflows) | CI, lint, auto-merge, and release automation |

## How a change gets to `main`

The process is written once, in [PROCESS.md](PROCESS.md): branch names and worktrees, commits and PR titles, the
PR description, the checks, review and merging, and releases. The pre-push hook runs `scripts/gate.sh`, which lints,
builds and tests the change the way CI does. What follows is specific to Articles.

### Start from an issue

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

### Make the change, test-first

- Write or update tests alongside every behaviour change, ideally first.
- Place tests in a folder structure that mirrors the source project, so it's
  obvious what each test covers. A test for `src/<Project>/<path>/Foo.cs`
  goes in `tests/<Project>.Tests/<path>/FooTests.cs`.
- Use xUnit v3 for tests, bUnit for Blazor components, TestContainers for
  integration tests, and Playwright for end-to-end tests.
- Follow the conventions in [.github/instructions](../.github/instructions) and
  the repo's [.editorconfig](../.editorconfig).

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
- [PROCESS.md](PROCESS.md): the development process
- [decisions.md](decisions.md): smaller lasting decisions
- [CONTEXT.md](../CONTEXT.md): domain glossary
- [docs/adr](adr): architecture decision records
- [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)
- [SECURITY.md](SECURITY.md)

## Questions?

Open an [issue](https://github.com/mpaulosky/Articles/issues) or reach out to
[@mpaulosky](https://github.com/mpaulosky).
