// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AdminRoleTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

namespace Web.E2E.Tests;

/// <summary>
/// Thin per-role E2E checklist for the Admin role (wayfinder #163, checklist decided on #162):
/// admin-only nav links are visible, Admin lands on the Admin-gated Manage Roles page, and Admin
/// can create and archive a throwaway article — the one action gated Admin-only via
/// <c>ArticleAuthorizationService.CanArchiveArticle</c>. All test data is created live through the
/// real UI; nothing is seeded into the database. Reuses the storage-state session captured once by
/// <see cref="AdminAuthFixture" />.
/// </summary>
[Collection(E2ETestCollectionDefinition.Name)]
public class AdminRoleTests(AdminAuthFixture auth)
{

	[Fact]
	public async Task AdminUser_SeesAdminOnlyNavLinks()
	{
		Assert.SkipWhen(auth.SkipReason is not null, auth.SkipReason ?? "");

		var page = await auth.CreateAuthenticatedPageAsync();
		await using var _ = new AsyncDisposeAction(() => page.Context.CloseAsync());

		await GotoAndWaitForCircuitAsync(page, "/");

		await Expect(page.Locator("a[href='/categories']")).ToBeVisibleAsync();
		await Expect(page.Locator("a[href='/admin/users']")).ToBeVisibleAsync();
	}

	[Fact]
	public async Task AdminUser_CanReach_ManageUsersPage()
	{
		Assert.SkipWhen(auth.SkipReason is not null, auth.SkipReason ?? "");

		var page = await auth.CreateAuthenticatedPageAsync();
		await using var _ = new AsyncDisposeAction(() => page.Context.CloseAsync());

		await GotoAndWaitForCircuitAsync(page, "/admin/users");

		await Expect(page).ToHaveURLAsync(new Regex("/admin/users$"));
		await Expect(page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Manage User Roles" }))
			.ToBeVisibleAsync();
	}

	[Fact]
	public async Task AdminUser_CanCreateAndArchiveArticle()
	{
		Assert.SkipWhen(auth.SkipReason is not null, auth.SkipReason ?? "");

		var page = await auth.CreateAuthenticatedPageAsync();
		await using var _ = new AsyncDisposeAction(() => page.Context.CloseAsync());

		var runId = Guid.NewGuid().ToString("N");
		var categoryName = await CreateCategoryAsync(page, runId);
		var articleTitle = await CreateArticleAsync(page, runId, categoryName);

		await GotoAndWaitForCircuitAsync(page, "/articles");

		var row = page.Locator("table tbody tr", new PageLocatorOptions { HasText = articleTitle });
		await Expect(row).ToBeVisibleAsync();

		await row.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Archive" }).ClickAsync();
		await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Confirm" }).ClickAsync();

		await page.GetByLabel("Include Archived").CheckAsync();
		await Expect(row.Locator(".app-badge", new LocatorLocatorOptions { HasText = "Archived" })).ToBeVisibleAsync();
	}

	/// <summary>
	/// Runs an async cleanup action on <c>await using</c> disposal - here, closing the browser
	/// context each test opens via <see cref="AdminAuthFixture.CreateAuthenticatedPageAsync" /> so
	/// its Blazor Server circuit doesn't stay open (and consuming CI runner resources) for the rest
	/// of the assembly run.
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
		await page.FillAsync("#create-category-description", $"Created by the Admin role E2E test run {runId}.");
		await page.Locator("button[type=submit]", new PageLocatorOptions { HasText = "Create category" }).ClickAsync();

		await Expect(page.Locator("table tbody tr", new PageLocatorOptions { HasText = categoryName }))
			.ToBeVisibleAsync();

		return categoryName;
	}

	/// <summary>
	/// Creates a throwaway article via the real UI, filling the markdown editor's CodeMirror surface
	/// through real keystrokes since it isn't a plain input Playwright can <c>Fill</c> directly.
	/// </summary>
	private static async Task<string> CreateArticleAsync(IPage page, string runId, string categoryName)
	{
		var articleTitle = $"E2E Admin Archive {runId}";

		await GotoAndWaitForCircuitAsync(page, "/articles/create");
		await page.FillAsync("#article-title", articleTitle);

		await page.Locator(".EasyMDEContainer .CodeMirror").ClickAsync();
		await page.Keyboard.TypeAsync($"Content created by the Admin role E2E test run {runId}.");

		await page.SelectOptionAsync("#article-category", new SelectOptionValue { Label = categoryName });
		await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create article" }).ClickAsync();

		await Expect(page).ToHaveURLAsync(new Regex("/articles/[a-z0-9-]+$"));

		return articleTitle;
	}

}
