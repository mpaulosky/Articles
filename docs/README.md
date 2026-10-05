# Articles

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](../LICENSE)
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

- [src](../src): the .NET projects (the Blazor web app and its Aspire host).
- [tests](../tests): architecture, unit, component, integration and end-to-end test suites.
- [docs](./): the development process, decisions, ADRs and release posts.
- [.github](../.github): CI, release and lint workflows, git hooks and contributor instructions.

## Documentation

- [Development process](PROCESS.md): hooks, branches, worktrees, commits, pull requests and releases.
- [Contributing guide](CONTRIBUTING.md): setting up and validating a change.
- [Decisions](decisions.md) and [ADRs](adr): why the code is the way it is.
- [Release posts](blogs/README.md): one post per release.

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
| [v0.1.108](https://github.com/mpaulosky/Articles/releases/tag/v0.1.108) | 2026-10-05 | chore: Re-apply the repo-ci-baseline Template for the release-post fixes | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-05-pr-291-chore-re-apply-the-repo-ci-baseline-template-for-the-release-post-fixes.md) |
| [v0.1.107](https://github.com/mpaulosky/Articles/releases/tag/v0.1.107) | 2026-10-05 | chore: Use pnpm instead of npm and npx in the Squad files | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-05-pr-287-chore-use-pnpm-instead-of-npm-and-npx-in-the-squad-files.md) |
| [v0.1.106](https://github.com/mpaulosky/Articles/releases/tag/v0.1.106) | 2026-10-05 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-05-pr-286-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.1.105](https://github.com/mpaulosky/Articles/releases/tag/v0.1.105) | 2026-10-05 | docs: Update ADRs for renamed CI and release workflows | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-05-pr-288-docs-update-adrs-for-renamed-ci-and-release-workflows.md) |
| [v0.1.104](https://github.com/mpaulosky/Articles/releases/tag/v0.1.104) | 2026-10-05 | chore: Ignore local SDK caches, coverage reports and personal files | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-05-pr-284-chore-ignore-local-sdk-caches-coverage-reports-and-personal-files.md) |
| [v0.1.103](https://github.com/mpaulosky/Articles/releases/tag/v0.1.103) | 2026-10-05 | chore: Standardize on the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-05-pr-280-chore-standardize-on-the-repo-ci-baseline-template.md) |
| [v0.1.102](https://github.com/mpaulosky/Articles/releases/tag/v0.1.102) | 2026-10-04 | fix(squad): Close the remaining branch-cleanup edge cases | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-10-04-pr-278-fix-squad-close-the-remaining-branch-cleanup-edge-cases.md) |
| [v0.1.101](https://github.com/mpaulosky/Articles/releases/tag/v0.1.101) | 2026-09-29 | build: write a single-document pnpm lockfile | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-09-29-pr-274-build-write-a-single-document-pnpm-lockfile.md) |
| [v0.1.100](https://github.com/mpaulosky/Articles/releases/tag/v0.1.100) | 2026-09-29 | build: switch the web project from npm to pnpm | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-09-29-pr-272-build-switch-the-web-project-from-npm-to-pnpm.md) |
| [v0.1.99](https://github.com/mpaulosky/Articles/releases/tag/v0.1.99) | 2026-09-28 | chore(web): Stop committing the generated app.css | [Post](https://github.com/mpaulosky/Articles/blob/main/docs/blogs/2026-09-28-pr-270-chore-web-stop-committing-the-generated-app-css.md) |

<!-- RELEASES_END -->

[All releases →](https://github.com/mpaulosky/Articles/releases)
