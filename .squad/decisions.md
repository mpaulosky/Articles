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
