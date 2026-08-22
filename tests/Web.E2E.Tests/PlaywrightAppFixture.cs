// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     PlaywrightAppFixture.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

namespace Web.E2E.Tests;

/// <summary>
/// Boots the Web app on a real Kestrel port (rather than the in-memory TestServer) so a
/// Playwright browser can navigate to it, and owns the Playwright browser instance for the test class.
/// </summary>
public sealed class PlaywrightAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{

	private IPlaywright? _playwright;

	private IBrowser? _browser;

	public async ValueTask InitializeAsync()
	{
		// Must be called before the factory is initialized. Port 0 picks a free port; the real
		// listening address is then populated onto ClientOptions.BaseAddress on first use below.
		UseKestrel(0);

		// CreateClient() forces WebApplicationFactory to actually start Kestrel.
		using var warmupClient = CreateClient();

		_playwright = await Playwright.CreateAsync().ConfigureAwait(false);
		_browser = await _playwright.Chromium.LaunchAsync().ConfigureAwait(false);
	}

	public async Task<IPage> CreatePlaywrightPageAsync()
	{
		var context = await _browser!
			.NewContextAsync(new BrowserNewContextOptions { BaseURL = ClientOptions.BaseAddress.ToString() })
			.ConfigureAwait(false);
		return await context.NewPageAsync().ConfigureAwait(false);
	}

	public override async ValueTask DisposeAsync()
	{
		if (_browser is not null)
		{
			await _browser.DisposeAsync().ConfigureAwait(false);
		}

		_playwright?.Dispose();

		await base.DisposeAsync().ConfigureAwait(false);
	}

}
