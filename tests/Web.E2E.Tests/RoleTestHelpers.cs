// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RoleTestHelpers.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

namespace Web.E2E.Tests;

/// <summary>
/// Shared plumbing for the per-role E2E checklist tests (<see cref="AdminRoleTests" />,
/// <see cref="AuthorRoleTests" />, <see cref="UserRoleTests" />): navigating and waiting for a
/// Blazor Server circuit, closing a test's browser context, and creating throwaway categories,
/// articles, and publishing them live through the real UI. Extracted once a third role-test file
/// would otherwise have triple-copied this - see the code review on wayfinder #164/PR #190.
/// </summary>
internal static class RoleTestHelpers
{

	/// <summary>
	/// Runs an async cleanup action on <c>await using</c> disposal - here, closing the browser
	/// context each test opens via an auth fixture's <c>CreateAuthenticatedPageAsync</c> so its
	/// Blazor Server circuit doesn't stay open (and consuming CI runner resources) for the rest of
	/// the assembly run.
	/// </summary>
	public sealed class AsyncDisposeAction(Func<Task> action) : IAsyncDisposable
	{
		public ValueTask DisposeAsync() => new(action());
	}

	public sealed record ArticleRef(string Title, string Slug);

	/// <summary>
	/// Navigates and waits for the page's Blazor Server SignalR circuit to finish connecting.
	/// A click sent before the circuit is up is silently dropped rather than queued, so an
	/// interaction right after <c>GotoAsync</c> can be a no-op even once the target element is
	/// visible and enabled.
	/// </summary>
	public static async Task GotoAndWaitForCircuitAsync(IPage page, string path)
	{
		await page.GotoAsync(path);
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	/// <summary>
	/// Creates a throwaway category through the Admin-only /categories page (creating an article
	/// requires selecting one, and nothing is pre-seeded), and returns its display name.
	/// </summary>
	public static async Task<string> CreateCategoryAsync(IPage page, string runId)
	{
		var categoryName = $"E2E Category {runId}";

		await GotoAndWaitForCircuitAsync(page, "/categories");
		await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create Category" }).ClickAsync();

		await Expect(page.Locator("#create-category-name")).ToBeVisibleAsync();
		await page.FillAsync("#create-category-name", categoryName);
		await page.FillAsync("#create-category-description", $"Created by an E2E test run {runId}.");
		await page.Locator("button[type=submit]", new PageLocatorOptions { HasText = "Create category" }).ClickAsync();

		await Expect(page.Locator("table tbody tr", new PageLocatorOptions { HasText = categoryName }))
			.ToBeVisibleAsync();

		return categoryName;
	}

	/// <summary>
	/// Creates a throwaway article via the real UI, filling the markdown editor's CodeMirror surface
	/// through real keystrokes since it isn't a plain input Playwright can <c>Fill</c> directly.
	/// </summary>
	public static async Task<ArticleRef> CreateArticleAsync(IPage page, string articleTitle, string categoryName)
	{
		await GotoAndWaitForCircuitAsync(page, "/articles/create");
		await page.FillAsync("#article-title", articleTitle);

		await page.Locator(".EasyMDEContainer .CodeMirror").ClickAsync();
		await page.Keyboard.TypeAsync($"Content for {articleTitle}.");

		await page.SelectOptionAsync("#article-category", new SelectOptionValue { Label = categoryName });
		await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create article" }).ClickAsync();

		// Excludes "create" itself: "/articles/[a-z0-9-]+$" also matches the still-on-the-create-page
		// URL "/articles/create" (a valid match for the character class), so without this exclusion
		// the assertion can pass before the redirect to the new article actually happens.
		await Expect(page).ToHaveURLAsync(new Regex("/articles/(?!create(?:/|$))[a-z0-9-]+$"));

		var slug = new Uri(page.Url).Segments[^1].TrimEnd('/');

		return new ArticleRef(articleTitle, slug);
	}

	/// <summary>
	/// Navigates to /articles and finds a specific article's row by searching for its (unique,
	/// per-run) title. The list is paginated 10-per-page and sorted by title, and every E2E run
	/// leaves its throwaway articles behind with no cleanup, so relying on default pagination to
	/// show a freshly created row goes flaky once enough runs have accumulated data - searching
	/// narrows the grid to just this one row regardless of how much else exists.
	/// </summary>
	public static async Task<ILocator> FindArticleRowAsync(IPage page, string articleTitle)
	{
		await GotoAndWaitForCircuitAsync(page, "/articles");

		await page.GetByLabel("Search articles by title or author").FillAsync(articleTitle);

		var row = page.Locator("table tbody tr", new PageLocatorOptions { HasText = articleTitle });
		await Expect(row).ToBeVisibleAsync();

		return row;
	}

	/// <summary>
	/// Publishes an existing throwaway article from the /articles list via the real UI.
	/// </summary>
	public static async Task PublishArticleAsync(IPage page, string articleTitle)
	{
		var row = await FindArticleRowAsync(page, articleTitle);

		await row.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Publish" }).ClickAsync();

		await Expect(row.Locator("text=Published")).ToBeVisibleAsync();
	}

}
