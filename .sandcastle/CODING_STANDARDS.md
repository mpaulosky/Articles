# Coding Standards

The reviewer loads this file during code review, and the implementer reads it before it starts. It doesn't restate the
rules: it points at the files that define them, which are the same ones every human contributor follows. Read the ones
that apply to the files the change touches.

## Everything

- [docs/CONTRIBUTING.md](../docs/CONTRIBUTING.md): code standards, test-first work and where tests go
- [CONTEXT.md](../CONTEXT.md): the domain language; use its terms in code and docs
- [docs/adr/](../docs/adr) and [docs/decisions.md](../docs/decisions.md): decisions already made; don't contradict them
- [.editorconfig](../.editorconfig): formatting
- [.github/instructions/git-commit-instructions.md](../.github/instructions/git-commit-instructions.md): commit messages

## By kind of file

| Files | Rules |
| --- | --- |
| `*.cs`, `*.csproj`, `Directory.*.props` | [dotnet-project.instructions.md](../.github/instructions/dotnet-project.instructions.md) |
| `*.razor`, Blazor components | [blazor.instructions.md](../.github/instructions/blazor.instructions.md) |
| MongoDB data access | [mongo-dba.instructions.md](../.github/instructions/mongo-dba.instructions.md) |
| `*.md` | [markdown.instructions.md](../.github/instructions/markdown.instructions.md) |
| Anything else | [.github/copilot-instructions.md](../.github/copilot-instructions.md), the repository-wide guidance |

## Always

- Package versions live in [Directory.Packages.props](../Directory.Packages.props), never in a `.csproj`.
- Every behaviour change comes with tests: xUnit v3, bUnit for components, TestContainers for integration tests,
  Playwright for end-to-end tests. A test for `src/<Project>/<path>/Foo.cs` goes in
  `tests/<Project>.Tests/<path>/FooTests.cs`.
- No secrets, tokens or connection strings in code, tests or config.
