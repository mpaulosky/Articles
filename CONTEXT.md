# Articles

Articles is a .NET 10 starter repository for Aspire-hosted, Auth0-enabled Blazor applications, paired with a public docs site (`docs/index.html`) that surfaces the project's release history and dev blog.

## Language

**Article**:
A piece of authored content in the publishing feature, carrying a Title, Content body, an Author snapshot, and an optional Category, with independent Published/Draft and Archived states.

**Published** / **Draft**:
An Article's visibility state, toggled by its `Publish()`/`Unpublish()` domain methods. A Published Article is visible to readers and carries a `PublishedOn` timestamp; a Draft is not.
_Avoid_: Live (for Published), unpublished (as a state name — the domain method `Unpublish()` names the transition, but the resulting state is "Draft").

**Archived**:
A reversible retirement state for an Article, toggled by its idempotent `Archive()`/`Unarchive()` domain methods, that hides it from the default Articles list without deleting it. Archived is independent of Published/Draft — archiving never changes an Article's Published/Draft state — and changing it always goes through `Archive()`/`Unarchive()` rather than a plain setter, unlike Category's own `IsArchived` flag. Only an Admin may Archive or Unarchive an Article, regardless of authorship.
_Avoid_: Delete, Remove — Archive is reversible; Delete is permanent.

**ArticleImage**:
A structured record of one image referenced inside an Article's `Content` markdown (filename/url, size, mimetype, alt text, uploaded timestamp), held in an array on Article and kept in sync by parsing `Content` on every save. It replaces regex-scanning `Content` at read time as the way to know which uploaded files an Article uses; see [ADR-0003](docs/adr/0003-article-image-array.md).
_Avoid_: Attachment — scope is images referenced in Content only, not general file attachments.

**Release**:
A tagged version of the repository (e.g. `v0.1.10`), created by the release workflow and published as a GitHub Release. The docs site lists releases live from the GitHub API as tag, date, and link — never hand-authored.
_Avoid_: Release notes (as a section name) — the old hand-written section was retired in favor of this live list; see [ADR-0001](docs/adr/0001-live-fetched-release-list.md).
