// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AuthorRoleTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

namespace Web.E2E.Tests;

/// <summary>
/// Thin per-role E2E checklist for the Author role (wayfinder #164, checklist decided on #162):
/// admin-only nav links are absent, Author is redirected to <c>/not-authorized</c> when navigating
/// to the Admin-gated Manage Roles page, and Author can edit its own article but not one authored by
/// someone else - exercising <c>ArticleAuthorizationService.CanEditArticle</c>'s ownership check. All
/// test data is created live through the real UI; nothing is seeded into the database. Reuses the
/// storage-state sessions captured once by <see cref="AuthorAuthFixture" /> and
/// <see cref="AdminAuthFixture" /> (the latter needed to create an article Author does not own).
/// </summary>
[Collection(E2ETestCollectionDefinition.Name)]
public class AuthorRoleTests(AuthorAuthFixture authorAuth, AdminAuthFixture adminAuth)
{

	[Fact]
	public async Task AuthorUser_DoesNotSeeAdminOnlyNavLinks()
	{
		Assert.SkipWhen(authorAuth.SkipReason is not null, authorAuth.SkipReason ?? "");

		var page = await authorAuth.CreateAuthenticatedPageAsync();
		await using var _ = new AsyncDisposeAction(() => page.Context.CloseAsync());

		await GotoAndWaitForCircuitAsync(page, "/");

		await Expect(page.Locator("a[href='/categories']")).ToBeHiddenAsync();
		await Expect(page.Locator("a[href='/admin/users']")).ToBeHiddenAsync();
	}

	[Fact]
	public async Task AuthorUser_RedirectedFromManageUsersPage()
	{
		Assert.SkipWhen(authorAuth.SkipReason is not null, authorAuth.SkipReason ?? "");

		var page = await authorAuth.CreateAuthenticatedPageAsync();
		await using var _ = new AsyncDisposeAction(() => page.Context.CloseAsync());

		await GotoAndWaitForCircuitAsync(page, "/admin/users");

		await Expect(page).ToHaveURLAsync(new Regex(@"/not-authorized(\?|$)"));
		await Expect(page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Not Authorized" }))
			.ToBeVisibleAsync();
	}

	[Fact]
	public async Task AuthorUser_CanEditOwnArticle_ButNotOthers()
	{
		Assert.SkipWhen(authorAuth.SkipReason is not null, authorAuth.SkipReason ?? "");
		Assert.SkipWhen(adminAuth.SkipReason is not null, adminAuth.SkipReason ?? "");

		var runId = Guid.NewGuid().ToString("N");

		var adminPage = await adminAuth.CreateAuthenticatedPageAsync();
		await using var adminDispose = new AsyncDisposeAction(() => adminPage.Context.CloseAsync());

		var categoryName = await CreateCategoryAsync(adminPage, runId);
		var othersArticle = await CreateArticleAsync(adminPage, $"E2E Author-Others {runId}", categoryName);

		var authorPage = await authorAuth.CreateAuthenticatedPageAsync();
		await using var authorDispose = new AsyncDisposeAction(() => authorPage.Context.CloseAsync());

		var ownArticle = await CreateArticleAsync(authorPage, $"E2E Author-Own {runId}", categoryName);

		// Author can edit its own article.
		await GotoAndWaitForCircuitAsync(authorPage, $"/articles/{ownArticle.Slug}/edit");

		var updatedTitle = $"{ownArticle.Title} (edited)";
		await authorPage.FillAsync("#article-title", "");
		await authorPage.FillAsync("#article-title", updatedTitle);
		await authorPage.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Save changes" }).ClickAsync();

		await Expect(authorPage).ToHaveURLAsync(new Regex("/articles/[a-z0-9-]+$"));
		await Expect(authorPage.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = updatedTitle }))
			.ToBeVisibleAsync();

		// Author cannot edit an article authored by someone else.
		await GotoAndWaitForCircuitAsync(authorPage, $"/articles/{othersArticle.Slug}/edit");

		await Expect(authorPage.GetByRole(AriaRole.Alert))
			.ToHaveTextAsync("You don't have permission to edit this article.");
	}

	/// <summary>
	/// Runs an async cleanup action on <c>await using</c> disposal - here, closing the browser
	/// context each test opens via <c>CreateAuthenticatedPageAsync</c> so its Blazor Server circuit
	/// doesn't stay open (and consuming CI runner resources) for the rest of the assembly run.
	/// </summary>
	private sealed class AsyncDisposeAction(Func<Task> action) : IAsyncDisposable
	{
		public ValueTask DisposeAsync() => new(action());
	}

	/// <summary>
	/// Navigates and waits for the page's Blazor Server SignalR circuit to finish connecting.
	/// A click sent before the circuit is up is silently dropped rather than queued, so an
	/// interaction right after <c>GotoAsync</c> can be a no-op even once the target element is
	/// visible and enabled.
	/// </summary>
	private static async Task GotoAndWaitForCircuitAsync(IPage page, string path)
	{
		await page.GotoAsync(path);
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	/// <summary>
	/// Creates a throwaway category through the Admin-only /categories page (creating an article
	/// requires selecting one, and nothing is pre-seeded), and returns its display name.
	/// </summary>
	private static async Task<string> CreateCategoryAsync(IPage page, string runId)
	{
		var categoryName = $"E2E Category {runId}";

		await GotoAndWaitForCircuitAsync(page, "/categories");
		await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create Category" }).ClickAsync();

		await Expect(page.Locator("#create-category-name")).ToBeVisibleAsync();
		await page.FillAsync("#create-category-name", categoryName);
		await page.FillAsync("#create-category-description", $"Created by the Author role E2E test run {runId}.");
		await page.Locator("button[type=submit]", new PageLocatorOptions { HasText = "Create category" }).ClickAsync();

		await Expect(page.Locator("table tbody tr", new PageLocatorOptions { HasText = categoryName }))
			.ToBeVisibleAsync();

		return categoryName;
	}

	/// <summary>
	/// Creates a throwaway article via the real UI, filling the markdown editor's CodeMirror surface
	/// through real keystrokes since it isn't a plain input Playwright can <c>Fill</c> directly.
	/// </summary>
	private static async Task<ArticleRef> CreateArticleAsync(IPage page, string articleTitle, string categoryName)
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

	private sealed record ArticleRef(string Title, string Slug);

}
