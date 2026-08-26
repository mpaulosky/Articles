// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserAuthFixture.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

namespace Web.E2E.Tests;

/// <summary>
/// Authenticates as the plain authenticated User test user per the fixture design on wayfinder
/// #158. Drives one real Auth0 Universal Login through the shared browser and caches the resulting
/// storage state in memory, so tests can spin up authenticated pages without repeating the login.
/// Reads <c>Auth0:E2E:User:Username</c>/<c>Password</c> from Web.E2E.Tests' user secrets locally, or
/// <c>Auth0__E2E__User__Username</c>/<c>Password</c> env vars in CI; when absent, sets
/// <see cref="SkipReason"/> instead of touching the network.
/// </summary>
public sealed class UserAuthFixture(PlaywrightAppFixture app) : IAsyncLifetime
{
	private string? _storageState;

	public string? SkipReason { get; private set; }

	public async ValueTask InitializeAsync()
	{
		var config = new ConfigurationBuilder()
			.AddUserSecrets<UserAuthFixture>(optional: true)
			.AddEnvironmentVariables()
			.Build();

		var username = config["Auth0:E2E:User:Username"];
		var password = config["Auth0:E2E:User:Password"];

		if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
		{
			SkipReason = "Auth0:E2E:User:Username/Password not configured; " +
				"run `dotnet user-secrets set` on Web.E2E.Tests to enable this test locally.";
			return;
		}

		var context = await app.Browser
			.NewContextAsync(new BrowserNewContextOptions { BaseURL = PlaywrightAppFixture.BaseUrl, IgnoreHTTPSErrors = true })
			.ConfigureAwait(false);
		var page = await context.NewPageAsync().ConfigureAwait(false);

		await page.GotoAsync("/Account/Login").ConfigureAwait(false);

		// Auth0's hosted Universal Login form.
		await page.FillAsync("#username", username).ConfigureAwait(false);
		await page.FillAsync("#password", password).ConfigureAwait(false);
		await page.ClickAsync("button[type=submit]").ConfigureAwait(false);

		await page.WaitForURLAsync(url => !url.Contains("auth0.com", StringComparison.OrdinalIgnoreCase))
			.ConfigureAwait(false);

		_storageState = await context.StorageStateAsync().ConfigureAwait(false);
		await context.CloseAsync().ConfigureAwait(false);
	}

	public async Task<IPage> CreateAuthenticatedPageAsync()
	{
		if (_storageState is null)
		{
			throw new InvalidOperationException(
				$"{nameof(UserAuthFixture)} has no cached storage state; check {nameof(SkipReason)} first.");
		}

		var context = await app.Browser
			.NewContextAsync(new BrowserNewContextOptions
			{
				BaseURL = PlaywrightAppFixture.BaseUrl,
				IgnoreHTTPSErrors = true,
				StorageState = _storageState
			})
			.ConfigureAwait(false);

		// CI runners are noticeably slower than local dev machines for a real Blazor Server + MongoDB
		// round trip; the 30s Playwright default has been observed to time out navigation there.
		context.SetDefaultTimeout(60_000);
		context.SetDefaultNavigationTimeout(60_000);

		return await context.NewPageAsync().ConfigureAwait(false);
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
