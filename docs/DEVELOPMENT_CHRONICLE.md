# The Articles Project: A Development Chronicle

## About this document

This document tells the story of how the Articles application was built, from
its very first commit through its current state as a working Blazor-based
publishing platform backed by MongoDB, Auth0, and a custom CQRS/mediator
pipeline. It is assembled from the project's own release-review blog posts —
more than sixty of them, spanning from the initial project scaffold in
mid-August through the most recent release — and re-tells them as a single
narrative rather than a chronological list of change notes.

Two things are worth knowing about the source material before reading on.
First, the early releases (roughly v0.0.1 through v0.1.9) were written as
traditional, hand-styled release blogs with headings like Overview, Technical
Details, and Impact. Starting around PR #25, the project switched to an
automated release process: every merged pull request mints its own version
tag and a short, PR-description-derived post is generated automatically. The
tone and depth of the source material therefore shifts partway through this
chronicle — early sections read as fuller narratives, later sections are
built from terser, PR-plan-of-record summaries. Second, a meaningful chunk of
the later history is not about the Articles product itself at all, but about
the tooling ("squad" automation, release workflows, branch cleanup) that the
team built to keep shipping it reliably. Both threads are told here, because
both shaped how the application arrived at its current state.

---

## Era 1: Initial Setup and Foundation (v0.0.1 – v0.0.3)

The project began, in the technical sense, as a .NET 10 solution built around
clean-architecture layering: a `Domain` project for core business entities, a
`Web` project hosting the Blazor UI, an `AppHost` project for local
orchestration via .NET Aspire, and a `ServiceDefaults` project for shared
service configuration. From the outset the team centralized dependency
management through `Directory.Build.props` and `Directory.Packages.props`,
enabled nullable reference types, and turned on warnings-as-errors — decisions
that would keep paying dividends many releases later.

Alongside the structural work, the very first release (v0.0.1) also wired up
two pieces of automated governance that would run for the life of the
project: CodeQL security scanning on every push, and Dependabot for automated
dependency-update pull requests. The test stack was chosen early too — xUnit
v3 with FluentAssertions and NSubstitute — establishing a testing
vocabulary that persisted across the whole project's history.

v0.0.2 was a quieter, validating release: it re-verified that
`Directory.Build.props` inheritance, central package management, EditorConfig
settings, and the test-runner configuration were actually consistent across
every project, rather than merely present. This kind of "trust but verify"
pass on your own scaffolding is easy to skip, and its explicit inclusion as
its own release says something about the discipline the team wanted to
establish before building on top of the foundation.

v0.0.3 fixed a specific but consequential defect: the CI pipeline's code
coverage reports were being generated but never actually discovered and
uploaded to Codecov, because of an incorrect file-path pattern. Once fixed,
every future pull request would show its coverage impact, coverage trends
could be tracked over time, and a coverage gate (initially targeting 80%)
became enforceable. Small as this fix reads today, it's the release that
made "test coverage" a visible, trackable metric for everything that
followed.

## Era 2: Testing Culture and Early Stability (v0.0.4 – v0.0.5)

v0.0.4 was the project's first substantial expansion of test coverage,
targeting the Web application's theme management (light/dark mode)
functionality: initialization, switching, persistence, and event handling.
It's a modest feature to test so thoroughly, but the release explicitly
frames itself as establishing *patterns* — how to test a stateful Blazor
service, how tests should be named and organized — rather than just adding
coverage for coverage's sake. bUnit joined the test stack here as the
Blazor-component testing library.

v0.0.5 was a much bigger release in substance: Auth0 was integrated as the
application's authentication provider. This meant wiring up the Auth0 SDK
for ASP.NET Core, OpenID Connect login/logout flows, JWT validation, and
authorization middleware — all configured through user secrets rather than
checked-in configuration, with HTTPS enforced and PKCE used for the
authorization code exchange. This release is the origin of everything the
application later builds on top of authentication: user-specific article
ownership, role-based access, and the Auth0 claims-based authorization model
used throughout later features.

The same release bundled two housekeeping efforts that are easy to miss but
mattered for stability: stabilizing flaky "empty category" test assertions
(fixing test isolation and eliminating async race conditions), and restoring
the article create/read/update/delete lifecycle after a regression, along
with a broken footer/metadata display. In other words, even while adding
authentication, the team was actively defending previously-working
functionality — a pattern of "add a feature, but don't let anything else
rot" that recurs throughout the project's history.

## Era 3: Domain Refactoring Toward Vertical Slices (v0.0.6 – v0.1.0)

With authentication in place, the team turned to a structural rethink of the
`Domain` project. v0.0.6 was explicitly a preparation release: it migrated
domain entities from a layer-first structure (`Domain/Entities/Article.cs`,
`Category.cs`, `Tag.cs`) to a feature-first one (`Domain/Articles/Article.cs`,
`ArticleId.cs`, `ArticleStatus.cs`; `Domain/Categories/Category.cs`, and so
on). The stated rationale was cohesion and discoverability — related code for
a feature lives together — and, longer-term, alignment with bounded contexts
and a future CQRS/MediatR layer. The same release fixed unrelated but
blocking infrastructure problems: a broken Redis connection configuration and
web-app startup issues inside the Aspire AppHost.

v0.1.0 completed that migration. Every domain entity, value object, and
domain event was relocated into its feature folder, and — just as
importantly — the entire test suite (158 tests at the time) was moved to
mirror the new structure and continued to pass with zero regressions. The
release notes are explicit that this was viewed as laying the groundwork for
CQRS with MediatR and for a genuine vertical-slice architecture, not simply a
cosmetic reorganization. This 0.0.6/0.1.0 pairing — prepare, then complete,
each with its own test-passing checkpoint — became something of a template
for how larger structural changes were handled later in the project (the
later ArticleImage work follows a very similar two-to-four-step arc).

## Era 4: The MongoDB Data Layer (v0.1.1)

v0.1.1 gave the newly-reorganized domain a place to actually live: a
production-ready MongoDB data layer, built test-first. `ArticlesMongoDbContext`
was introduced using Entity Framework Core's MongoDB provider, with
`ArticleRepository` and `CategoryRepository` implementing full CRUD behind
narrow interfaces (`IArticleRepository`, and its category counterpart). The
repository tests were written before the implementation and ran against
isolated MongoDB instances via Testcontainers rather than mocks — a
commitment to integration-style testing for the data layer that the project
would return to and dramatically expand many releases later (see Era 12
below). At this point the goal was simply a working, tested persistence
seam; pagination, sorting, and caching were explicitly deferred to later
work.

## Era 5: Documentation as a First-Class Practice (v0.1.2)

v0.1.2 is unusual among the early releases in that it shipped no application
code at all. Instead, it retroactively wrote comprehensive release blog posts
for every prior version, v0.0.1 through v0.1.1, and established the
consistent post structure (Overview / What's New / Technical Details /
Impact, plus frontmatter metadata) that this very chronicle now draws on.
The stated motivation was straightforward: every release should have a
corresponding, discoverable record of what changed and why, so that decision
rationale isn't lost to git history alone. That discipline — sometimes
manual, later automated — persisted for the rest of the project's history,
which is precisely why a document like this one is possible to assemble at
all.

## Era 6: Test Reorganization and Demo Cleanup (v0.1.3)

v0.1.3 split UI component tests out of the general `Web.Tests` project into a
dedicated `Web.UI.Tests` project built around bUnit, separating "does this
Blazor component render and behave correctly" tests from API/service/
middleware tests. This made it possible to run component tests independently
and kept each test project focused on a single kind of concern — a split
that scaled well as the UI grew substantially in later releases (by the time
of PR #49, `Web.UI.Tests` alone had over a hundred tests). The same release
also removed the Counter demo page left over from the default Blazor
template, along with its tests and navigation entry — roughly 150 lines of
code that no longer served any purpose once real features existed.

## Era 7: Article and Category Management Arrive (v0.1.4)

v0.1.4 is where the application actually became "an articles app" in the
functional sense. It shipped complete CRUD UI for both articles and
categories: list, detail, create, and edit pages, all backed by a CQRS
pattern implemented with MediatR — commands like `CreateArticleCommand` and
`UpdateArticleCommand`, queries like `GetArticleByIdQuery`, and one handler
per operation, each with a single responsibility. Blazor components were
organized under `Components/Pages/Articles` and `Components/Pages/Categories`,
with validated forms, confirmation dialogs for deletion, and category
assignment on articles. This release also folded in another round of test
reorganization, bringing the total test count from 158 to 191 with the new
CRUD coverage added at every layer — domain, integration, and component.

## Era 8: A Custom Mediator and Better Navigation (v0.1.5)

Rather than depending directly on MediatR's `IMediator` everywhere, v0.1.5
introduced `MyMediator`, a thin wrapper interface (`IMyMediator`) around it,
plus custom pipeline behaviors for logging and validation. The logging
behavior timed every request and logged both successful completions and
failures with elapsed milliseconds; the validation behavior ran registered
FluentValidation validators ahead of the handler and threw a
`ValidationException` on failure, keeping that concern out of individual
handlers entirely. The practical effect was that handlers could focus purely
on business logic — a `CreateArticleHandler` no longer needed to manually log
its own start and completion, because the pipeline did it for every request
uniformly. This custom-mediator layer became a long-lived piece of
infrastructure: much later, PR #99 would upgrade its internal dispatch from a
`dynamic`-typed call to a type-safe `RequestHandlerWrapper` seam, and the
mediator integration tests added in Era 12 exercise `AddMyMediator` plus
`LoggingBehavior` as the *real* pipeline, not a stand-in. The same release
also reorganized the navigation menu into logical sections (Content,
Administration) instead of a flat list of links.

## Era 9: Attaching Identity to Content (v0.1.6)

With Auth0 already wired in since v0.0.5, v0.1.6 connected authentication to
authorship. A new `IUserContextService` extracted the current user's ID,
name, and email from Auth0 claims via `IHttpContextAccessor`, and
`CreateArticleHandler` was updated to reject unauthenticated requests and
stamp new articles with the current user's identity automatically rather
than trusting client-submitted author fields. An authorization handler
(`ArticleAuthorizationHandler` / `SameAuthorRequirement`) restricted editing
to the article's own author, and the UI conditionally rendered Edit/Delete
controls only for the owning user. This is the seed of an authorization model
— "owner or Admin" — that essentially every later feature (archiving,
category management, the eventual `ArticleAuthorizationService`) builds on
and extends.

## Era 10: Coverage Tooling and a Warning-Free Build (v0.1.7 – v0.1.8)

v0.1.7 brought in dotCover for coverage analysis alongside the existing
Coverlet/Codecov setup, defined explicit coverage thresholds (80% statement,
70% branch, higher for the domain layer), and — in the same pass — resolved
every outstanding compiler and static-analysis warning across the solution:
nullable-reference issues, unused variables, missing async/await patterns,
and missing XML documentation on public APIs. `TreatWarningsAsErrors` and
`EnableNETAnalyzers` were turned on in the build configuration, converting
"warnings" into "build failures" going forward.

v0.1.8 followed up by closing the coverage gaps that dotCover's reporting had
made visible: FileStorage, TextEditor, and the MyMediator pipeline behaviors
went from partial coverage (as low as 38% for TextEditor) to 95–100%, driving
overall project coverage from 82.7% to 88.1%. This pair of releases —
instrument, then act on what the instrumentation reveals — is a recurring
shape in the project: measure first, then close the gap the measurement
exposes, rather than guessing at what needs testing.

## Era 11: Release Automation Grows Up (v0.1.9 and the automated-post era)

v0.1.9 addressed two gaps left by the seven releases before it: the blog
posts for v0.1.2 through v0.1.8 had never actually been linked from any
index (so they existed on disk but were undiscoverable), and the release
workflow's automated "commit docs updates" step had been silently failing
against the repository's branch-protection rules, which reject direct pushes
to `main`. The fix routed that step through a proper pull request instead —
opened via `gh pr create`, with `[skip-release]` in its title so that the
automated docs PR's own merge wouldn't trigger a second release cycle. The
first real run of this new flow immediately surfaced a further permission
gap (GitHub Actions needed explicit permission to create and approve pull
requests), which PR #24/#25 also worked through — a good example of an
automation fix revealing the next automation fix.

From this point forward (starting with PR #25), release notes stopped being
manually written per-version and became fully automated: every merged PR
mints a new patch version and a short, structurally identical post generated
from the PR's own title, description, and test plan. PR #29 fixed a related
problem in the same vein — the CI "Build Solution" check that branch
protection requires was configured to skip on docs-only PRs, which meant
those PRs got stuck in a `BLOCKED` state with no status check ever appearing
to satisfy or fail. Removing the `paths-ignore` filter from the
`pull_request` trigger (while keeping it for the post-merge `push` trigger)
resolved it.

## Era 12: Article Archiving and the QuickGrid List Rebuild (PRs #39–#51)

A cluster of releases across late v0.1.x built out article archiving end to
end and, along the way, rebuilt the entire Articles list page. PR #39 added
the domain layer first: idempotent `Archive()`/`Unarchive()` methods on
`Article`, independent of `IsPublished`, plus matching commands and an
Admin-only `CanArchiveArticle` authorization rule — deliberately
backend-only, with UI wiring called out as a separate later ticket. That UI
wiring arrived incrementally: PR #43 extracted a shared `ArticleForm`
component out of the articles page (recovering work that had been reported
merged in an earlier session but was actually never pushed — a reminder that
"the agent said it was done" and "it's actually on `main`" are not the same
claim). PR #44 built a dedicated Article edit page on top of that shared
form, gated by the ownership/Admin authorization rule from Era 9. PR #47
replaced the two-column articles layout with a proper header, an inline
create panel, and a sortable, paginated grid. PR #49 layered archiving
controls, a global search box, and per-column filters on top of that grid.

PR #51 is worth calling out on its own: a completeness review found that,
despite six sub-issues each individually being marked "done," two explicit
requirements from the parent spec had fallen through the cracks entirely —
the list was supposed to use the real `Microsoft.AspNetCore.Components.
QuickGrid` component, not a hand-rolled `<table>`, and a set of domain terms
were supposed to already be documented in `CONTEXT.md` before the sub-issues
started, but never had been. The fix rewrote the page against actual
QuickGrid (with its `PropertyColumn`/`TemplateColumn`, `ColumnOptions` filter
popups, and built-in `Paginator`), backfilled the glossary, added the
idempotency test cases the spec had called for, and fixed seed data that
had drifted (none of the three seeded articles set `isArchived`, so the new
archiving feature had nothing seeded to demonstrate it). This is a useful
data point on how the project handled spec/implementation drift: not through
process changes, but through an explicit after-the-fact completeness audit
that treated "each sub-issue closed" as insufficient evidence that the
parent requirement was actually met.

## Era 13: Slugs, Category Archiving, and End-to-End Test Scaffolding (PRs #53–#57)

PR #53 repaired the release workflow itself — the newly added `gh workflow
run` dispatch loop was 403'ing because the job lacked `actions: write`
permission, and one workflow it tried to dispatch could never actually be
triggered that way because it only defined a `workflow_call` trigger.

PRs #55 and #57 then moved article and category routing from opaque
ObjectId-based URLs to human-readable slugs, and replaced category deletion
with the same archive/unarchive soft-delete model articles already had —
extending `ArticleAuthorizationService` with a `CanCreateArticle` check and
loosening `CanViewArticle` so any author can see any article, including
other authors' unpublished drafts (edit and archive permissions stayed
owner/Admin-only). The same pass fixed a real authorization bug: a
`!x?.y == true` operator-precedence mistake in the authorization service
had been silently treating a null `Identity` as authenticated. PR #55 also
introduced `Web.E2E.Tests`, a Playwright-backed end-to-end test project,
starting with a single home-page smoke test — the first appearance of true
browser-level testing in the project, complementing the domain, integration,
and component test layers already in place.

## Era 14: Retiring the Old Release Workflows (PRs #59–#67)

With `squad-release.yml` now doing release-notes generation, versioning, and
docs-PR automation all in one place, five separate legacy GitHub Actions
workflows — `squad-blog-readme-sync.yml`, `squad-milestone-blog.yml`,
`squad-milestone-release-decision.yml`, `squad-milestone-release.yml`, and
`squad-release-blog.yml` — were deleted outright as redundant. It's a small
but telling sequence: rather than letting superseded automation linger
unused (and potentially still triggering), the team removed it deliberately,
one file at a time, once its replacement had proven itself.

## Era 15: Trust, Redesign, and Polish (PRs #69–#73)

PR #69 fixed a subtler automation trust problem: release-notes pull requests
opened by `squad-release.yml` were authored as `github-actions[bot]` via the
default `GITHUB_TOKEN`, and GitHub treats PRs from that identity as
untrusted — every `pull_request`-triggered workflow on them required a
manual "Approve and run workflows" click before its checks would even start.
Switching the PR-creation step to authenticate with a real personal access
token (`RELEASE_PR_PAT`, falling back to `GITHUB_TOKEN` if unset) let those
PRs run their checks automatically, like any human contributor's PR would.

PR #71 redesigned the Categories management page to match the visual and
structural conventions the Articles page had already established: gated to
Admins only, a QuickGrid-based list (deliberately simpler than the Articles
grid — sortable only, no search or pagination, by design), and a shared
`CategoryForm` component mirroring `ArticleForm`. PR #73 followed with a pure
styling pass, rounding buttons and inputs from `rounded-lg` to `rounded-full`
across both pages to match the app's pill-button aesthetic, and consolidating
repeated filter-input classes into a shared `.app-button-actions` utility
class.

## Era 16: A Deliberate Coverage Sweep (PRs #83–#95)

A run of seven consecutive releases each closed one specific, numbered
coverage gap: `ManageRoles` admin UI (#80), `RedirectToLogin` navigation
behavior under both Auth0-enabled and Auth0-disabled configurations (#82),
`Profile` avatar/fallback/role-badge logic (#81), a missing `Category.Empty`
blank-state regression test (#79), edge cases in `MyMediator`'s internal
struct handling (#78), the L1/L2 cache hit paths and corrupt-JSON fallback
behavior of `UserManagementCacheService` (#77), and the auth wiring
extensions themselves — `Auth0StartupExtensions`,
`AuthenticationServiceExtensions`, `LocalAuthenticationStateProviders` (#76).
Each of these had presumably been identified as a specific gap ahead of
time (the consistent "closes #NN" framing suggests a tracked backlog rather
than ad hoc discovery), and the sweep systematically worked through it one
component at a time rather than in one large, harder-to-review PR.

## Era 17: Maturing the Squad Automation (PRs #97–#107)

This stretch is largely about the meta-tooling the team uses to manage its
own branches and pull requests, rather than the Articles application itself,
but it's woven tightly enough into the release history to be worth tracing.
PR #97 added a missing `cleanup-squad-branches.sh` script that a workflow had
already been referencing (so the workflow was failing outright without it),
defined branch cleanup-eligibility rules that account for squash-merging
(a merged branch is never literally a fast-forward ancestor of `main` under
squash-merge, so "has a merged PR" has to count as its own eligibility
signal), and switched the nightly cron from a no-op dry run to actually
applying cleanup. PR #99 was a substantial functional release bundled in:
Articles list filtering/action UI pieces, an extraction of Auth0 Management
API client construction into a testable factory, the `MyMediator` dispatch
upgrade mentioned in Era 8, and two real bugs fixed by newly-added tests — an
unconditional Auth0 sign-out on logout even when Auth0 wasn't configured,
and a duplicate role claim bug in `Auth0AuthenticationStateProvider`.
PRs #101–#107 rounded out the automation maturity work: a `squad-promote.yml`
workflow for dev→preview→main promotion, casting/identity scaffolding for
tracking agent state across sessions, PR auto-merge eligibility widened from
`squad/*` branches only to any same-repo branch, closed-but-unmerged pull
requests treated as an immediate signal to clean up their branch rather than
waiting out an age-based fallback, and a CONTRIBUTING.md correction that had
been describing a `dev`-based branch flow the repository no longer actually
used.

## Era 18: Real Integration Testing Against Real MongoDB and a Real Mediator (PRs #116–#130)

Up to this point, most of the project's "integration" tests for the data and
handler layers actually ran against Entity Framework Core's InMemory
provider, not real MongoDB — a reasonable early tradeoff, but one that can
hide real database-specific behavior. This era replaced that tradeoff
deliberately and incrementally. PR #116 scaffolded a new `Web.Integration.
Tests` project using `Testcontainers.MongoDb`, with a `MongoContainerFixture`
that starts a single MongoDB container per test assembly (pinned to the same
version AppHost uses locally) and gives every test class its own database
name inside it, so tests stay isolated without paying a per-class container
startup cost. PRs #118 and #120 then filled that project in with real CRUD
coverage for `ArticleRepository` and `CategoryRepository` against that real
container, including the trickier case of updating a detached entity fetched
through one `DbContext` and saved through another.

PRs #122, #124, and #126 pushed a layer higher: dispatching requests through
the *actual* `IMediator` pipeline (`AddMyMediator` plus `LoggingBehavior`,
matching what `Program.cs` wires up in production) rather than calling
handlers directly, for `ArticleFeatureHandler`, `CategoryFeatureHandler`, and
`UserManagementHandler` respectively. The last of these needed a different
approach — `UserManagementHandler` talks to the Auth0 Management API rather
than MongoDB, so its test fixture substitutes only the Auth0 client factory
via NSubstitute (since a live Auth0 tenant isn't available in CI) while
still wiring the real cache service and real mediator pipeline around it.
Finally, PR #130 extracted the repeated builder logic these tests had
accumulated (`CreateArticleCommand`, `UpdateArticleCommand`,
`CreateCategoryCommand`, `CategoryDto`, `AuthorDto`, and friends) into a
shared `Web.TestData` project — deliberately placed *outside* the `tests/`
folder, per an explicit ADR, because the CI workflow's test-discovery job
globs every project under `tests/**` and a shared, non-test class library
sitting in there would have broken that matrix.

## Era 19: Images Become a First-Class, Database-Backed Concept (PRs #132–#146)

The final and most substantial feature arc in this chronicle concerns how
the application handles images embedded in article content — and it follows
the same "decide, then build in stages, then backfill, then fix what
breaks in production use" shape seen earlier in the archiving work, but at
larger scale.

PR #132 started by giving article creation its own dedicated `/articles/
create` page (previously folded into the list page), upgrading the content
editor to a full markdown editor with inline image upload
(`PSC.Blazor.Components.MarkdownEditor`), and adding a SkiaSharp-backed
`ImageOptimizer` that resizes and recompresses uploaded images for the web.
It also handled orphaned uploads — images added to a draft and then removed
or replaced before saving — both client-side and via a server-side diff of
old versus new content on update and delete. In the same PR, several
services (`FileStorage`, `ImageOptimizer`, `UploadedImageReferences`,
`TextEditor`) were relocated out of generic shared folders and into the
Articles feature slice, keeping the codebase's vertical-slice convention
intact as image handling grew from "a small helper" into "a real subsystem."

That subsystem's orphan-detection approach — scanning `Content` text with a
regex to infer which images were still referenced — was recognized as
fragile, and PR #138 recorded the alternative in ADR-0003: give `Article` a
structured `ArticleImage` array, populated by actually parsing `Content` on
save, rather than inferring image state from raw markdown text after the
fact. The implementation followed in three deliberate steps. PR #140 added
just the `ArticleImage` type and an `ArticleImages` collection mapped as an
embedded Mongo `OwnsMany` (mirroring the existing `OwnsOne` pattern already
used for `Author`/`Category`) — schema and mapping only, with population
explicitly deferred. PR #142 then wired that population in:
`Article.Create`/`Article.Update` now derive `ArticleImages` by parsing
markdown image references out of `Content`, reusing prior upload metadata
(file size, mime type, upload timestamp) for images whose URL didn't change,
and the orphan-cleanup logic in `ArticleFeatureHandler` switched from
re-scanning content text to diffing the structured `ArticleImage` lists
directly — retiring the regex-based approach ADR-0003 had targeted. Getting
EF Core to reconcile a freshly-parsed image list against previously
persisted entries in a disconnected update required giving the `OwnsMany`
mapping an explicit key (`FileName`); without it, saves threw a shadow-key
`InvalidOperationException`. PR #144 closed the loop with an idempotent
startup migration, `ArticleImageBackfillMigration`, that finds every article
whose `ArticleImages` array is still empty (because it predates the field
entirely) and repopulates it by parsing that article's existing `Content` —
run once via `IDbContextFactory` at application startup, and a no-op once
every article has been backfilled.

Shipping this feature into real use surfaced two more bugs, fixed together
in PR #146: a content race where `TextEditor`'s `UploadingChanged(false)`
event could trigger a re-render that clobbered a just-inserted image with
stale content (fixed by reordering `ContentChanged` ahead of
`UploadingChanged`), and a SignalR message-size ceiling that was too low for
large images — a 10 MB file becomes roughly 13.3 MB once base64-encoded for
transport, and SignalR's default `MaximumReceiveMessageSize` would silently
disconnect the Blazor circuit mid-upload before that ceiling was raised to
15 MB.

## Where things stand

The most recent entry in the source material, PR #152, is not a shipped
feature but a plan: an implementation-ready specification for extending
`UserManagementCacheService` to a roughly five-minute TTL with page-driven
polling in `ManageRoles.razor`, incorporating decisions about an in-process
single-flight guard against cache-stampede ("thundering herd") behavior and
how equality should be computed for cached role/user data. It is a fitting
place to end this chronicle, since it captures the project's habitual
sequence in miniature: write the plan and its decisions down first, then
build it — the same pattern visible in the domain refactor of Era 3, the
archiving work of Era 12, and the ArticleImage work of Era 19.

Taken as a whole, the arc runs from a bare .NET 10 solution with CI
scaffolding and a security scanner, through a full domain redesign into
vertical slices, a real MongoDB persistence layer, Auth0-backed
authentication and authorization, a hand-rolled mediator pipeline with
logging and validation behaviors, a complete article and category management
UI with archiving, search, and filtering, a genuinely layered test strategy
(domain, component, integration-against-real-MongoDB, integration-against-
the-real-mediator, and end-to-end-via-Playwright), an increasingly
sophisticated release-automation system, and — most recently — a
database-backed model of the images embedded in an article's content,
arrived at deliberately through an ADR rather than organically. Sixty-two
small, well-documented releases in, the project reads less like a series of
disconnected patches and more like a single sustained argument for building
things in visible, reversible, well-tested steps.
