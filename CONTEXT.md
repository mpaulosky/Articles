# Articles

Articles is a .NET 10 starter repository for Aspire-hosted, Auth0-enabled Blazor applications, paired with a public docs site (`docs/index.html`) that surfaces the project's release history and dev blog.

## Language

**Release**:
A tagged version of the repository (e.g. `v0.1.10`), created by the release workflow and published as a GitHub Release. The docs site lists releases live from the GitHub API as tag, date, and link — never hand-authored.
_Avoid_: Release notes (as a section name) — the old hand-written section was retired in favor of this live list; see [ADR-0001](docs/adr/0001-live-fetched-release-list.md).
