# Live-fetch the release list from GitHub instead of hand-writing release notes

**Superseded (2026-10):** the release list is static again. Since the move to the repo-ci-baseline Template, its release
workflow (`.github/scripts/release_post.py`) rewrites the release and blog tables in `docs/index.html` on every release,
so the list can't go stale. Only the header version badge is still fetched live.

`docs/index.html` had a hand-written "Release notes" section that went stale immediately, because nothing in the release automation touched it —
it was still frozen at 2026-07-18 while the repo had already moved past v0.1.9.
We retired that section and replaced it with a "Releases" section (tag, date, link for the most recent 10, plus a link to the full GitHub releases page) and a header version badge,
both fetched live from the public GitHub REST API (`/repos/mpaulosky/Articles/releases`) at page load,
rather than extending the release workflow to bake them into static HTML the way the blog list is generated.
We chose live fetching because the repo is public, so the unauthenticated call runs from each visitor's own browser/IP rather than a shared server, staying well within GitHub's rate limits;
and because the release workflow currently commits docs *before* creating the tag/release, so a static bake would always lag one release behind without reordering that workflow.

**Consequences**: the docs site now depends on GitHub API availability for this content at runtime.
A "Loading releases…" placeholder covers the fetch,
and a static "View releases on GitHub" link is the fallback if the call fails (offline viewer, API outage, rate limit, or a network that blocks `api.github.com`).
