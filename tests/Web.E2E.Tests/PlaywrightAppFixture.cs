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
/// Playwright browser can navigate to it, and owns the Playwright browser instance for the assembly.
/// </summary>
public sealed class PlaywrightAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{

	// Auth0's Allowed Callback URLs are registered as https://localhost:7122/callback, so the app
	// under test must be reached at this exact scheme+host+port for the OAuth redirect_uri to match
	// (Auth0 rejects http://127.0.0.1:7122/callback with "Callback URL mismatch" even though it's
	// the same server). UseKestrel(int) only binds http://127.0.0.1, hence the explicit Kestrel
	// config below instead.
	public const string BaseUrl = "https://localhost:7122";

	private IPlaywright? _playwright;

	private IBrowser? _browser;

	public IBrowser Browser => _browser!;

	public async ValueTask InitializeAsync()
	{
		// Must be called before the factory is initialized.
		UseKestrel(options => options.ListenLocalhost(7122, listenOptions => listenOptions.UseHttps()));

		// CreateClient() forces WebApplicationFactory to actually start Kestrel.
		using var warmupClient = CreateClient();

		_playwright = await Playwright.CreateAsync().ConfigureAwait(false);
		_browser = await _playwright.Chromium.LaunchAsync().ConfigureAwait(false);
	}

	public async Task<IPage> CreatePlaywrightPageAsync()
	{
		var context = await _browser!
			.NewContextAsync(new BrowserNewContextOptions { BaseURL = BaseUrl, IgnoreHTTPSErrors = true })
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
