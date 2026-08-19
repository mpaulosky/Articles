# Articles

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![xUnit Tests](https://img.shields.io/badge/Tests-xUnit-blueviolet?logo=github)](https://github.com/mpaulosky/Articles/actions/workflows/squad-ci.yml)
[![Latest Release](https://img.shields.io/github/v/release/mpaulosky/Articles?logo=github&color=blue&label=Release)](https://github.com/mpaulosky/Articles/releases/latest)

[![CI/CD](https://github.com/mpaulosky/Articles/actions/workflows/squad-ci.yml/badge.svg)](https://github.com/mpaulosky/Articles/actions/workflows/squad-ci.yml)
[![CodeCov Coverage](https://codecov.io/gh/mpaulosky/Articles/branch/main/graph/badge.svg)](https://codecov.io/gh/mpaulosky/Articles)
[![Coverage Gate](https://img.shields.io/badge/Coverage%20Gate-≥80%25-brightgreen?logo=codecov)](https://github.com/mpaulosky/Articles/actions/workflows/squad-ci.yml)

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

- [.github/workflows](.github/workflows) — canonical CI/CD, lint, triage, sync,
  and release automation workflows.
- [.github/hooks](.github/hooks) — local hook scripts (including pre-push gates).
- [.github/instructions](.github/instructions) — coding and documentation
  instructions applied to contributors and agents.
- [src](src) — .NET implementation projects used by the standard toolchain.
- [tests](tests) — architecture, domain, component, unit, and end-to-end test
  suites.
- [docs](docs) — architecture, contribution guidance, and release-review history.
- [Directory.Packages.props](Directory.Packages.props), [global.json](global.json),
  and [GitVersion.yml](GitVersion.yml) — shared dependency/version governance.

## Documentation index

- [Docs landing page](docs/index.html) — overview and documentation entry points.
- [Architecture overview](docs/ARCHITECTURE.md) — repository layout and policy
  boundaries.
- [Contributing guide](docs/CONTRIBUTING.md) — contribution workflow and validation
  expectations.
- [Release review blog index](docs/blogs/README.md) — release-review post index.

## Release review blogs

The release review posts in [docs/blogs](docs/blogs/README.md) summarize the
rollout history of workflow-standard and major changes by release.

### Latest blogs (top 5, generated)

<!-- BLOG_START -->
| Date | Title | Tags |
|------|-------|------|
| 2026-08-19 | [Articles v0.1.1: MongoDB Data Layer Implementation](docs/blogs/2026-08-19-v0.1.1-mongodb-data-layer.md) | mongodb, data-access, repository-pattern, entity-framework, tdd |
| 2026-08-19 | [Articles v0.1.0: Domain Refactoring Complete](docs/blogs/2026-08-19-v0.1.0-domain-refactoring.md) | domain-driven-design, refactoring, vertical-slices, clean-architecture, testing |
| 2026-08-19 | [Articles v0.0.6: Domain Refactoring Preparation](docs/blogs/2026-08-19-v0.0.6-domain-refactoring-prep.md) | architecture, domain-design, refactoring, redis, aspire |
| 2026-08-18 | [Articles v0.0.5: Auth0 Integration and Stability](docs/blogs/2026-08-18-v0.0.5-auth0-integration.md) | auth0, authentication, security, testing, stability |
| 2026-08-17 | [Articles v0.0.4: Theme Management Testing](docs/blogs/2026-08-17-v0.0.4-testing-expansion.md) | unit-tests, blazor, theme-management, web-testing |
<!-- BLOG_END -->

## Quick start

### For adopters (using this standard in another repo)

1. Review the standard and policy surface in
   [.github/workflows](.github/workflows), [.github/hooks](.github/hooks), and
   [.github/instructions](.github/instructions).
2. Copy the assets you want to adopt into your target repository.
3. Validate in CI and locally with the same gates this repo uses
   (workflows in `.github/workflows`, hook behavior in `.github/hooks/pre-push`).
4. Track updates through releases and release-review posts in
   [docs/blogs](docs/blogs/README.md).

### For contributors (updating this repo)

1. Clone and restore:

   ```bash
   git clone https://github.com/mpaulosky/Articles.git
   cd Articles
   dotnet restore Articles.slnx
   ```

2. Run baseline validation:

   ```bash
   dotnet build Articles.slnx --configuration Release
   dotnet test Articles.slnx --nologo
   ```

3. Follow the full contribution workflow in
   [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md).

## Merged-PR release automation (concise)

The [Squad Release workflow](.github/workflows/squad-release.yml) runs when a PR
is merged to `main` (or by manual dispatch with a merged PR number).

Every merged PR to `main` is treated as release-eligible. The workflow runs one
release/blog pass per PR (idempotency marker: `Source PR: #<number>` in release
notes) and then:

- computes semver bump from PR labels, with idempotency checks to avoid duplicate
  releases,
- generates a release-review post and rebuilds
  [docs/blogs/README.md](docs/blogs/README.md),
- updates this README latest-blog block (`<!-- BLOG_START -->
| Date | Title | Tags |
|------|-------|------|
| 2026-08-15 | [Fix Mongo AppHost startup drift](docs/blogs/2026-08-15-pr-2-fix-mongo-apphost-startup-drift.md) | release,automation |
| 2026-08-15 | [Add MongoDbContext and Extensions for service defaults and health checks](docs/blogs/2026-08-15-pr-1-add-mongodbcontext-and-extensions-for-service-defaults-and-health-checks.md) | release,automation |
<!-- BLOG_END -->`)
  from `docs/blogs/README.md` (top 5 rows),
- updates [docs/index.html](docs/index.html) latest blog links from the same rows.
