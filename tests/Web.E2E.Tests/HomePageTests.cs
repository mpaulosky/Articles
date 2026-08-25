// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     HomePageTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

namespace Web.E2E.Tests;

[Collection(E2ETestCollectionDefinition.Name)]
public class HomePageTests(PlaywrightAppFixture fixture)
{
	[Fact]
	public async Task HomePage_Loads_WithExpectedTitleAndFooterBrand()
	{
		var page = await fixture.CreatePlaywrightPageAsync();

		await page.GotoAsync("/");

		(await page.TitleAsync()).Should().Be("Home");
		await Expect(page.Locator(".app-footer-brand")).ToContainTextAsync("Articles");
	}
}
