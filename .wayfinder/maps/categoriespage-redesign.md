---
id: map-categoriespage-redesign
title: Redesign CategoriesPage to match ArticlesPage
labels: [wayfinder:map]
status: open
created: 2026-08-22
---

## Destination

Redesign `CategoriesPage.razor` to match `ArticlesPage.razor`'s look and feel and structure:

- Page gated to Admin role only via `@attribute [Authorize(Roles = "Admin")]` (no per-action
	authorization service — every action on the page is already Admin-only).
- "Create Category" hidden behind a toggle button/panel, mirroring Articles' "Create New Article"
	panel.
- Category list rendered via QuickGrid (Name w/ Archived badge, Slug, Description, Actions
	columns), sortable columns only — no per-column filters, no global search box, no pagination.
- Editing happens in a modal (reusing the existing `role="dialog"` modal shell already used for the
	archive-confirm dialog on this page), not inline-in-row and not a separate route.
- Create panel and Edit modal share one new `CategoryForm` component (mirroring `ArticleForm`'s
	Model/Saving/SubmitLabel/OnValidSubmit shape) instead of duplicating the Name/Description form
	markup twice.
- Archive/Unarchive actions keep the existing confirm-modal flow already on the page.

## Notes

- Domain: this repo's Blazor Web feature-folder convention
	(`src/Web/Components/Features/<Feature>/...`).
- Reference implementation: `src/Web/Components/Features/Articles/Pages/ArticlesPage.razor` and
	its `ArticleAuthorizationService` / `ArticleForm` for structural and visual parity.
- All destination decisions below were settled in one breadth-first grilling session before any
	tickets were created; no further design conversation is expected before implementation.

## Decisions so far

- [Implement CategoriesPage redesign](../tickets/categoriespage-redesign-implement.md): built per
	the destination — Admin-only page gate, hidden create panel + edit modal sharing a new
	`CategoryForm` component, sortable-only QuickGrid with Name/Slug/Description/Actions.

## Not yet specified

<!-- empty: the grilling session covered the full frontier, no fog remains -->

## Out of scope

- Per-user/per-author ownership rules for categories (Articles' "Author can only edit their own
	article" rule) — categories have no author/owner concept, so this doesn't carry over.
- Global search box, per-column filters, and pagination on the Categories grid — explicitly
	declined for this redesign in favor of a minimal sortable-only grid.
- A dedicated `/categories/{id}/edit` route (Articles' pattern) — explicitly declined in favor of
	an edit modal.
