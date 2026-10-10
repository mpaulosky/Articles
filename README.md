# Articles

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![xUnit Tests](https://img.shields.io/badge/Tests-xUnit-blueviolet?logo=github)](https://github.com/mpaulosky/Articles/actions/workflows/ci.yml)
[![Latest Release](https://img.shields.io/github/v/release/mpaulosky/Articles?logo=github&color=blue&label=Release)](https://github.com/mpaulosky/Articles/releases/latest)

[![CI/CD](https://github.com/mpaulosky/Articles/actions/workflows/ci.yml/badge.svg)](https://github.com/mpaulosky/Articles/actions/workflows/ci.yml)
[![CodeCov Coverage](https://codecov.io/gh/mpaulosky/Articles/branch/main/graph/badge.svg)](https://codecov.io/gh/mpaulosky/Articles)
[![Coverage Gate](https://img.shields.io/badge/Coverage%20Gate-≥80%25-brightgreen?logo=codecov)](https://github.com/mpaulosky/Articles/actions/workflows/ci.yml)

[![Open Issues](https://img.shields.io/github/issues/mpaulosky/Articles?color=0366d6)](https://github.com/mpaulosky/Articles/issues?q=is%3Aopen+is%3Aissue)
[![Closed Issues](https://img.shields.io/github/issues-closed/mpaulosky/Articles?color=6f42c1)](https://github.com/mpaulosky/Articles/issues?q=is%3Aclosed+is%3Aissue)
[![Open PRs](https://img.shields.io/github/issues-pr/mpaulosky/Articles?color=28a745)](https://github.com/mpaulosky/Articles/pulls?q=is%3Aopen+is%3Apr)
[![Closed PRs](https://img.shields.io/github/issues-pr-closed/mpaulosky/Articles?color=6f42c1)](https://github.com/mpaulosky/Articles/pulls?q=is%3Aclosed+is%3Apr)

## Purpose

This repository is designed to create a Web application that allows a user to
 create articles on any topic they wish and manage the publication of said
 articles when they are completed. The articles include a title, an
 introduction, a category, and the content of the full article. It also allows
 adding links, pictures, and other attachments. The author is responsible for managing their own articles.
 They can create, edit, and delete their articles as needed.
 An administrator oversees the platform to ensure content quality and compliance.

## Repository structure

- [src](src): the .NET projects (the Blazor web app and its Aspire host).
- [tests](tests): architecture, unit, component, integration and end-to-end test suites.
- [docs](docs): the development process, decisions, ADRs and release posts.
- [.github](.github): CI, release and lint workflows, git hooks and contributor instructions.

## Documentation

- [Development process](docs/PROCESS.md): hooks, branches, worktrees, commits, pull requests and releases.
- [Contributing guide](docs/CONTRIBUTING.md): setting up and validating a change.
- [Decisions](docs/decisions.md) and [ADRs](docs/adr): why the code is the way it is.
- [Release posts](docs/blogs/README.md): one post per release.

## Quick start

```bash
git clone https://github.com/mpaulosky/Articles.git
cd Articles
git config core.hooksPath .github/hooks
dotnet build Articles.slnx
dotnet test --solution Articles.slnx
```

## Releases

<!-- RELEASES_START -->

| Version | Date | Title | Blog post |
| ------- | ---- | ----- | --------- |
| [v0.1.127](https://github.com/mpaulosky/Articles/releases/tag/v0.1.127) | 2026-10-10 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-10-pr-330-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.1.126](https://github.com/mpaulosky/Articles/releases/tag/v0.1.126) | 2026-10-10 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-10-pr-328-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.1.125](https://github.com/mpaulosky/Articles/releases/tag/v0.1.125) | 2026-10-10 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-10-pr-326-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.1.124](https://github.com/mpaulosky/Articles/releases/tag/v0.1.124) | 2026-10-10 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-10-pr-324-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.1.123](https://github.com/mpaulosky/Articles/releases/tag/v0.1.123) | 2026-10-10 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-10-pr-322-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.1.122](https://github.com/mpaulosky/Articles/releases/tag/v0.1.122) | 2026-10-10 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-10-pr-320-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.1.121](https://github.com/mpaulosky/Articles/releases/tag/v0.1.121) | 2026-10-09 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-09-pr-318-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.1.120](https://github.com/mpaulosky/Articles/releases/tag/v0.1.120) | 2026-10-09 | fix(sandcastle): Give the host's merge of main a commit-format message | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-09-pr-316-fix-sandcastle-give-the-host-s-merge-of-main-a-commit-format-message.md) |
| [v0.1.119](https://github.com/mpaulosky/Articles/releases/tag/v0.1.119) | 2026-10-09 | fix(sandcastle): Keep agent-written code from running on the host | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-09-pr-314-fix-sandcastle-keep-agent-written-code-from-running-on-the-host.md) |
| [v0.1.118](https://github.com/mpaulosky/Articles/releases/tag/v0.1.118) | 2026-10-08 | chore(deps): Bump Aspire's MongoDB, Redis packages and AppHost SDK to 13.6.1 | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-08-pr-312-chore-deps-bump-aspire-s-mongodb-redis-packages-and-apphost-sdk-to-13-6-1.md) |

<!-- RELEASES_END -->

[All releases →](https://github.com/mpaulosky/Articles/releases)
