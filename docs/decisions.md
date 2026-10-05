# Decisions

Lasting decisions about Articles that don't warrant a full ADR. Architectural decisions with real trade-offs are in
[`adr/`](adr/).

## Tailwind shared component styles

Component CSS shared across pages lives in `src/Web/wwwroot/app.tailwind.css`, as reusable Tailwind component classes
(headings such as `h1` to `h3`, and common layout patterns), rather than repeated utility lists in each page.

## Auth0 Management API configuration is separate from login

Server-side user and role management uses the Auth0 Management API through its own configuration namespace
(`Auth0:Management:*`, read by `Auth0ManagementApiClientFactory`) and service layer. It authenticates with the
client-credentials flow (machine to machine), so it is independent of the user-login settings (`Auth0:Domain`,
`Auth0:ClientId`, `Auth0:ClientSecret`).

## Squad removed (2026-10)

The repo no longer uses the squad-team framework. Its `.squad/` state, coordinator agent, `squad-*` workflows, skill
catalogs, MCP server and `squad*` labels were removed, and the repo moved to the shared CI, release and git-hook
Baseline, whose process is described in [`PROCESS.md`](PROCESS.md). Branches follow that standard (`feature/`, `fix/`,
`hotfix/`, `chore/`); `squad/` and `sprint/` are retired. The squad decision log's release and blog rules are replaced
by the Baseline's release pipeline, and its session logs and agent history were dropped rather than migrated.
