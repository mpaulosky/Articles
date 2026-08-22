---
id: categoriespage-redesign-implement
title: Implement CategoriesPage redesign
labels: [wayfinder:task]
status: closed
parent: map-categoriespage-redesign
assignee: implement-session
blocked_by: []
created: 2026-08-22
closed: 2026-08-22
---

## Question

Carry out the CategoriesPage redesign per the map's Destination
(`.wayfinder/maps/categoriespage-redesign.md`):

1. Add `@attribute [Authorize(Roles = "Admin")]` to `CategoriesPage.razor` (no per-action
	 authorization service needed — mirror `ManageRoles.razor`'s page-level gate).
2. Extract a `CategoryForm` component (Name + Description fields, validation) mirroring
	 `ArticleForm`'s `Model` / `Saving` / `SubmitLabel` / `OnValidSubmit` shape.
3. Wire "Create Category" behind a toggle button/panel using `CategoryForm`, mirroring
	 ArticlesPage's "Create New Article" panel (open/close/cancel behavior, error display).
4. Build an Edit modal reusing `CategoryForm`, reusing the existing `role="dialog"` modal shell
	 already on this page for the archive-confirm dialog. Edit button on each grid row opens the
	 modal pre-populated with that category.
5. Replace the current card-list with a QuickGrid: columns Name (sortable, Archived badge inline),
	 Slug (sortable), Description (not sortable), Actions (Edit → opens modal, Archive/Unarchive →
	 existing confirm-modal flow). No per-column filters, no global search box, no pagination.
6. Match ArticlesPage's visual structure throughout: `app-page-card` header, `app-panel-card`
	 sections, `app-section-header`, `app-badge` counts, button classes (`app-button-primary` /
	 `app-button-secondary` / `app-button-warning`).

This is a Task ticket carrying execution (per the map's Notes: all design decisions were already
settled in the charting grilling session, so this ticket does the build rather than deciding
anything further).

## Answer

Implemented per the six-step checklist:

1. `src/Web/Components/Features/Categories/Pages/CategoriesPage.razor` gets
	 `@attribute [Authorize(Roles = "Admin")]`, mirroring `ManageRoles.razor`'s page-level gate. No
	 `CategoryAuthorizationService` was added.
2. Extracted `src/Web/Components/Features/Categories/Models/CategoryFormModel.cs` and
	 `src/Web/Components/Features/Categories/Components/CategoryForm.razor`, structural mirrors of
	 `ArticleFormModel`/`ArticleForm`.
3. "Create Category" button toggles a hidden `app-panel-card` panel using `CategoryForm`, mirroring
	 ArticlesPage's create-panel open/close/cancel/error-display behavior.
4. Added an Edit modal reusing the page's existing `role="dialog"` shell (previously only used for
	 the archive-confirm dialog); the grid's Edit button opens it pre-populated via `StartEdit`.
5. Replaced the card-list with a `QuickGrid`: Name (sortable, Archived badge inline), Slug
	 (sortable), Description (not sortable), Actions (Edit/Archive/Unarchive). No filters, search box,
	 or pagination.
6. Converted the archive-confirm modal's buttons from raw Tailwind utility classes to
	 `app-button-secondary`/`app-button-warning`, and used `app-panel-card`/`app-section-header`
	 throughout, matching ArticlesPage.

Also split the single `_error` field into `_createError` and `_editError` (plus the pre-existing
`_loadError`) so the create panel and edit modal don't clobber each other's error message — found
during `/code-review`'s Standards pass, not part of the original checklist.

`tests/Web.UI.Tests/Features/Categories/Pages/CategoriesPageTests.cs` was rewritten to match (20
tests); full `Web.UI.Tests` suite (115 tests) and `Web.Tests` Category suite (25 tests) pass, and
the full solution builds clean. `/code-review` ran both Standards and Spec axes clean (no missing
requirements; the one Standards finding was fixed before commit).
