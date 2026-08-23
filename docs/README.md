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
| 2026-08-23 | [fix: treat closed-unmerged PRs as immediate orphan-branch signal](docs/blogs/2026-08-23-pr-105-fix-treat-closed-unmerged-prs-as-immediate-orphan-branch-signal.md) | release,automation |
| 2026-08-23 | [fix: enable PR auto-merge for all same-repo branches, not just squad/*](docs/blogs/2026-08-23-pr-103-fix-enable-pr-auto-merge-for-all-same-repo-branches-not-just-squad.md) | release,automation |
| 2026-08-23 | [chore: add squad promote workflow and casting/identity scaffolding](docs/blogs/2026-08-23-pr-101-chore-add-squad-promote-workflow-and-casting-identity-scaffolding.md) | release,automation |
| 2026-08-23 | [Add Articles/UserManagement features, mediator wiring, and squad workflow updates](docs/blogs/2026-08-23-pr-99-add-articles-usermanagement-features-mediator-wiring-and-squad-workflow-updates.md) | release,automation |
| 2026-08-23 | [fix: add missing squad branch cleanup script, apply nightly automatically](docs/blogs/2026-08-23-pr-97-fix-add-missing-squad-branch-cleanup-script-apply-nightly-automatically.md) | release,automation |
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
   dotnet test Articles.slnx
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
| 2026-08-23 | [fix: treat closed-unmerged PRs as immediate orphan-branch signal](docs/blogs/2026-08-23-pr-105-fix-treat-closed-unmerged-prs-as-immediate-orphan-branch-signal.md) | release,automation |
| 2026-08-23 | [fix: enable PR auto-merge for all same-repo branches, not just squad/*](docs/blogs/2026-08-23-pr-103-fix-enable-pr-auto-merge-for-all-same-repo-branches-not-just-squad.md) | release,automation |
| 2026-08-23 | [chore: add squad promote workflow and casting/identity scaffolding](docs/blogs/2026-08-23-pr-101-chore-add-squad-promote-workflow-and-casting-identity-scaffolding.md) | release,automation |
| 2026-08-23 | [Add Articles/UserManagement features, mediator wiring, and squad workflow updates](docs/blogs/2026-08-23-pr-99-add-articles-usermanagement-features-mediator-wiring-and-squad-workflow-updates.md) | release,automation |
| 2026-08-23 | [fix: add missing squad branch cleanup script, apply nightly automatically](docs/blogs/2026-08-23-pr-97-fix-add-missing-squad-branch-cleanup-script-apply-nightly-automatically.md) | release,automation |
<!-- BLOG_END -->`)
  from `docs/blogs/README.md` (top 5 rows),
- updates [docs/index.html](docs/index.html) latest blog links from the same rows.
