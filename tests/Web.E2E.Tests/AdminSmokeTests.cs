// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AdminSmokeTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

namespace Web.E2E.Tests;

/// <summary>
/// PROTOTYPE (wayfinder #159): proves an authenticated Admin session, captured once by
/// <see cref="AdminAuthFixture" />, can reach an Admin-gated page. Not the thin per-role checklist
/// from #162 — that's #163's job once this mechanism is validated.
/// </summary>
[Collection(E2ETestCollectionDefinition.Name)]
public class AdminSmokeTests(AdminAuthFixture auth)
{
	[Fact]
	public async Task AdminUser_CanReach_ManageUsersPage()
	{
		Assert.SkipWhen(auth.SkipReason is not null, auth.SkipReason ?? "");

		var page = await auth.CreateAuthenticatedPageAsync();

		await page.GotoAsync("/admin/users");

		await Expect(page).ToHaveURLAsync(new Regex("/admin/users$"));
	}
}
