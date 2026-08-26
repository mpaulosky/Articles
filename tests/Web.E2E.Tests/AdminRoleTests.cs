// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AdminRoleTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

using static Web.E2E.Tests.RoleTestHelpers;

namespace Web.E2E.Tests;

/// <summary>
/// Thin per-role E2E checklist for the Admin role (wayfinder #163, checklist decided on #162):
/// admin-only nav links are visible, Admin lands on the Admin-gated Manage Roles page, and Admin
/// can create and archive a throwaway article — the one action gated Admin-only via
/// <c>ArticleAuthorizationService.CanArchiveArticle</c>. All test data is created live through the
/// real UI; nothing is seeded into the database. Reuses the storage-state session captured once by
/// <see cref="AdminAuthFixture" />. Shared plumbing lives in <see cref="RoleTestHelpers" />.
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
		var article = await CreateArticleAsync(page, $"E2E Admin Archive {runId}", categoryName);

		var row = await FindArticleRowAsync(page, article.Title);

		await row.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Archive" }).ClickAsync();
		await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Confirm" }).ClickAsync();

		await page.GetByLabel("Include Archived").CheckAsync();
		await Expect(row.Locator(".app-badge", new LocatorLocatorOptions { HasText = "Archived" })).ToBeVisibleAsync();
	}

}
