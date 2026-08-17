# Tailwind Web Theme Plan

## Goal

Modernize the web app shell and page styling with Tailwind CSS while preserving the app behavior and adding a persistent theme toggle.

## Workstream

1. Add Tailwind CSS tooling to `src/Web` and remove the Bootstrap entry points from the app shell.
2. Update the shared layout and page styling to use Tailwind utilities and an app-level theme layer.
3. Add a theme toggle that persists the selected theme in localStorage and respects the system preference.
4. Add focused TDD coverage for the theme script and layout behavior before final validation.
5. Validate the app compiles and the targeted tests pass.

## TDD checkpoints

- `tests/Web.Tests/ThemeScriptBehaviorTests.cs` verifies the app shell no longer depends on Bootstrap and initializes the theme script correctly.
- `tests/Web.UI.Tests/MainLayoutThemeTests.cs` verifies the shell renders the expected content and the theme toggle flips the UI state.
- These tests are required to pass before the implementation is considered complete.
