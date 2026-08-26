// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserRoleTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

using static Web.E2E.Tests.RoleTestHelpers;

namespace Web.E2E.Tests;

/// <summary>
/// Thin per-role E2E checklist for the plain authenticated User role (wayfinder #165, checklist
/// decided on #162): admin-only nav links are absent, User is redirected to
/// <c>/not-authorized</c> when navigating to the Admin-gated Manage Roles page, and User can view a
/// published article - the one action available to it, since it cannot create, edit, or archive.
/// All test data is created live through the real UI; nothing is seeded into the database. Reuses
/// the storage-state sessions captured once by <see cref="UserAuthFixture" /> and
/// <see cref="AdminAuthFixture" /> (the latter needed to create and publish the shared article,
/// since User cannot create one itself). Shared plumbing lives in <see cref="RoleTestHelpers" />.
/// </summary>
[Collection(E2ETestCollectionDefinition.Name)]
public class UserRoleTests(UserAuthFixture userAuth, AdminAuthFixture adminAuth)
{

	[Fact]
	public async Task PlainUser_DoesNotSeeAdminOnlyNavLinks()
	{
		Assert.SkipWhen(userAuth.SkipReason is not null, userAuth.SkipReason ?? "");

		var page = await userAuth.CreateAuthenticatedPageAsync();
		await using var _ = new AsyncDisposeAction(() => page.Context.CloseAsync());

		await GotoAndWaitForCircuitAsync(page, "/");

		await Expect(page.Locator("a[href='/categories']")).ToBeHiddenAsync();
		await Expect(page.Locator("a[href='/admin/users']")).ToBeHiddenAsync();
	}

	[Fact]
	public async Task PlainUser_RedirectedFromManageUsersPage()
	{
		Assert.SkipWhen(userAuth.SkipReason is not null, userAuth.SkipReason ?? "");

		var page = await userAuth.CreateAuthenticatedPageAsync();
		await using var _ = new AsyncDisposeAction(() => page.Context.CloseAsync());

		await GotoAndWaitForCircuitAsync(page, "/admin/users");

		await Expect(page).ToHaveURLAsync(new Regex(@"/not-authorized(\?|$)"));
		await Expect(page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Not Authorized" }))
			.ToBeVisibleAsync();
	}

	[Fact]
	public async Task PlainUser_CanViewPublishedArticle()
	{
		Assert.SkipWhen(userAuth.SkipReason is not null, userAuth.SkipReason ?? "");
		Assert.SkipWhen(adminAuth.SkipReason is not null, adminAuth.SkipReason ?? "");

		var runId = Guid.NewGuid().ToString("N");

		// Admin's setup work (category + article + publish) closes its browser context before
		// User's page opens, so only one authenticated Blazor Server circuit is ever live at once -
		// the same resource-conscious shape every other role test already follows.
		ArticleRef article;
		{
			var adminPage = await adminAuth.CreateAuthenticatedPageAsync();
			await using var adminDispose = new AsyncDisposeAction(() => adminPage.Context.CloseAsync());

			var categoryName = await CreateCategoryAsync(adminPage, runId);
			article = await CreateArticleAsync(adminPage, $"E2E User Published {runId}", categoryName);
			await PublishArticleAsync(adminPage, article.Title);
		}

		var userPage = await userAuth.CreateAuthenticatedPageAsync();
		await using var userDispose = new AsyncDisposeAction(() => userPage.Context.CloseAsync());

		await GotoAndWaitForCircuitAsync(userPage, $"/articles/{article.Slug}");

		await Expect(userPage.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = article.Title }))
			.ToBeVisibleAsync();

		// Scoped to the status badge specifically: the article's own title also contains the word
		// "Published" (by test design), so a bare GetByText("Published") matches multiple elements.
		await Expect(userPage.Locator(".app-badge", new PageLocatorOptions { HasText = "Published" }).First)
			.ToBeVisibleAsync();
	}

}
