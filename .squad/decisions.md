### 2026-08-04T08:00:26.713-07:00: Post-merge release and versioning automation policy (consolidated)
**By:** Squad

**What:**
- Run post-merge automation automatically for every PR merged into `main`; retire milestone-based release/blog workflows.
- Produce one release and one blog post per eligible merged PR (no aggregation).
- Gate release/blog generation by release-worthy paths, including production C# paths and `tests/` changes.
- Default to a patch version bump; allow `release:minor` and `release:major` labels to override.
- Commit generated docs/blog updates directly to `main` during the same run.
- If release creation fails after docs/blog commits, keep docs commits and fail loudly for manual release retry.

**Why:**
- Provides deterministic, per-merge release behavior.
- Prevents missed releases while preserving explicit severity control for versioning.
- Keeps documentation synchronized with release generation without blocking auditability when upstream release APIs fail.

### 2026-08-04T08:00:26.713-07:00: Release blog content generation contract (consolidated)
**By:** Squad

**What:**
- Generate release blog posts from a fixed template using merged PR metadata.
- Source highlights from merged PR title/body only.
- Exclude merge-range commit messages from highlight generation.

**Why:**
- Enforces predictable, reviewable output shape.
- Avoids noisy or misleading highlights derived from unrelated merge commits.

### 2026-08-04T08:00:26.713-07:00: Blog and index publishing surfaces (consolidated)
**By:** Squad

**What:**
- Treat `docs/blogs/README.md` as the canonical blog index with columns: Date | Title | Tags.
- Require each release flow to update `docs/index.html` in addition to `docs/blogs` and root `README`.
- Maintain a generated top-5 Latest Blogs table in root `README` between `BLOG_START`/`BLOG_END`, sourced from `docs/blogs/README.md`.

**Why:**
- Keeps public-facing discovery surfaces consistent across docs entry points.
- Ensures root and docs indexes reflect the same canonical blog metadata.

### 2026-08-04T11:45:13-07:00: Tailwind CSS Skill Improvement Decisions
**By:** mpaulosky (via Copilot)

**What:**
1. Deliverable scope: improve add-tailwind-css-to-blazor skill AND migrate Articles src/Web
2. Skill location: promote from .copilot/skills/ → .github/skills/ (canonical Copilot CLI path)
3. Bootstrap JS: CSS-only cleanup (remove entire lib/ folder; no JS component audit required — no JS usage found in markup)
4. Theme: basic Tailwind migration only — no dark/light toggle (file separate issue referencing blazor-tailwind-theme-persistence skill)
5. Output path: wwwroot/css/app.css (matches skill convention, requires App.razor link tag update)
6. Issue location: mpaulosky/Articles#18 filed
7. Skill confidence: elevated from medium → high after verification against real project + official docs

**Why:** Establishes canonical path and scoping decisions for Tailwind migration work.

### 2026-08-04T15:47:56-07:00: Repository purpose redefinition
**By:** Squad (user directive)

**What:** Redefine repository purpose to a web application for creating/managing article publication with title, introduction, category, full content, and support for links/pictures/attachments.

**Why:** User directive (2026-08-04) to establish the new project scope.

### 2026-08-04T20:13:03-07:00: Tailwind shared component style direction
**By:** Squad (user directive)

**What:** For the Web Tailwind migration, move shared component-specific CSS into src/Web/Styles/app.tailwind.css and introduce reusable Tailwind-based component classes (for example headings like h1/h2/h3 and common layout patterns) to simplify usage across the site.

**Why:** User directive to establish styling conventions for the Tailwind migration.

### 2026-08-08T18:48:37-07:00: Git workflow skill must support multiple branch models
**By:** teqs (user directive)

**What:** Make the git-workflow skill flexible enough to support both current documented model (dev/main/insiders) and alternative repo model where main is the base branch and all feature work happens on squad/{issue}-{slug} branches.

**Why:** User directive (2026-08-08) to ensure the workflow skill works with different branching strategies.

### 2026-08-09T17:02:16-07:00: Use TDD for Auth0 rollout
**By:** Squad (user directive via mpaulosky)

**What:** Create tests as we go using TDD so each step is locked in before moving to the next step for Auth0 implementation.

**Why:** User directive to ensure Auth0 implementation sequence is test-first and incrementally protected by passing tests before each subsequent step.

### 2026-08-09T19:33:40-07:00: Auth0 Management API Configuration Contract
**By:** Zoe

**What:** Define separate, decoupled Auth0 Management API configuration namespace (Auth0:ManagementApi:*) and service layer for Sprint 2 server-side operations. Management API uses client credentials flow (machine-to-machine) and is independent from Sprint 1's user login authentication. Includes IAuth0ManagementApiClient interface contract, DTOs, configuration schema, service architecture, testing contract, and TDD setup.

**Why:** Ensures clean separation of concerns between user authentication (Sprint 1) and server-side user/role management (Sprint 2), with explicit configuration management and TDD-ready testing approach.
