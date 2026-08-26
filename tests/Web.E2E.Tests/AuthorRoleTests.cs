// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AuthorRoleTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

using static Web.E2E.Tests.RoleTestHelpers;

namespace Web.E2E.Tests;

/// <summary>
/// Thin per-role E2E checklist for the Author role (wayfinder #164, checklist decided on #162):
/// admin-only nav links are absent, Author is redirected to <c>/not-authorized</c> when navigating
/// to the Admin-gated Manage Roles page, and Author can edit its own article but not one authored by
/// someone else - exercising <c>ArticleAuthorizationService.CanEditArticle</c>'s ownership check. All
/// test data is created live through the real UI; nothing is seeded into the database. Reuses the
/// storage-state sessions captured once by <see cref="AuthorAuthFixture" /> and
/// <see cref="AdminAuthFixture" /> (the latter needed to create an article Author does not own).
/// Shared plumbing lives in <see cref="RoleTestHelpers" />.
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

		// Admin's setup work (category + the article Author doesn't own) closes its browser context
		// before Author's page opens, so only one authenticated Blazor Server circuit is ever live at
		// once - the same resource-conscious shape every other role test already follows, and needed
		// here since CI runners have observed MongoDB connection failures under concurrent-circuit
		// pressure (see #163's follow-up / PR #188).
		string categoryName;
		ArticleRef othersArticle;
		{
			var adminPage = await adminAuth.CreateAuthenticatedPageAsync();
			await using var adminDispose = new AsyncDisposeAction(() => adminPage.Context.CloseAsync());

			categoryName = await CreateCategoryAsync(adminPage, runId);
			othersArticle = await CreateArticleAsync(adminPage, $"E2E Author-Others {runId}", categoryName);
		}

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

}
