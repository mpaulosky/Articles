// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     WebTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost.Tests
// =============================================

using Microsoft.Extensions.Logging;

namespace Tests;

public class WebTests
{
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(90);

	[Fact]
	public async Task GetWebResourceRootReturnsOkStatusCode()
	{
		// Arrange
		using var cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);
		var cancellationToken = cancellationTokenSource.Token;

		var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(cancellationToken);
		appHost.Services.AddLogging(logging =>
		{
			logging.SetMinimumLevel(LogLevel.Debug);
			// Override the logging filters from the app's configuration
			logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
			logging.AddFilter("Aspire.", LogLevel.Debug);
			// To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
		});
		appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
		{
			clientBuilder.AddStandardResilienceHandler();
		});

		await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
		await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

		// Act
		using var httpClient = app.CreateHttpClient("webfrontend");
		await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend", cancellationToken)
			.WaitAsync(DefaultTimeout, cancellationToken);
		var response = await httpClient.GetAsync(new Uri("/", UriKind.Relative), cancellationToken);

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}
}
