// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AccountEndpointsTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Web.Tests;

// Each test builds its own WebApplicationFactory<Program> (rather than sharing one via IClassFixture) because
// each needs different Auth0 configuration, and a shared factory risks cross-test contamination.
public class AccountEndpointsTests
{
	[Fact]
	public async Task Login_WhenAuth0IsConfigured_ChallengesTheAuth0Scheme()
	{
		// Arrange
		await using var factory = CreateFactory(domain: "example.auth0.com", clientId: "real-client-id",
			clientSecret: "real-client-secret");
		using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

		// Act
		using var response = await client.GetAsync(
			new Uri("/Account/Login?returnUrl=%2Farticles", UriKind.Relative), TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Redirect);
		response.Headers.Location.Should().NotBeNull();
		response.Headers.Location!.Host.Should().Be("example.auth0.com");
		response.Headers.Location.AbsolutePath.Should().Be("/authorize");
	}

	[Fact]
	public async Task Login_WhenAuth0IsNotConfigured_ChallengesTheLocalCookieScheme()
	{
		// Arrange
		await using var factory = CreateFactory(domain: "YOUR_DOMAIN", clientId: "YOUR_CLIENT_ID",
			clientSecret: "YOUR_CLIENT_SECRET");
		using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

		// Act
		using var response = await client.GetAsync(
			new Uri("/Account/Login?returnUrl=%2Farticles", UriKind.Relative), TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Redirect);
		response.Headers.Location.Should().NotBeNull();
		response.Headers.Location!.AbsolutePath.Should().Be("/Account/Login");
	}

	[Fact]
	public async Task Logout_WhenAuth0IsConfigured_SignsOutOfAuth0()
	{
		// Arrange
		await using var factory = CreateFactory(domain: "example.auth0.com", clientId: "real-client-id",
			clientSecret: "real-client-secret");
		using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

		// Act
		using var response = await client.GetAsync(
			new Uri("/Account/Logout", UriKind.Relative), TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Redirect);
		response.Headers.Location.Should().NotBeNull();
		response.Headers.Location!.Host.Should().Be("example.auth0.com");
	}

	[Fact]
	public async Task Logout_WhenAuth0IsNotConfigured_RedirectsHomeWithoutError()
	{
		// Arrange
		await using var factory = CreateFactory(domain: "YOUR_DOMAIN", clientId: "YOUR_CLIENT_ID",
			clientSecret: "YOUR_CLIENT_SECRET");
		using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

		// Act
		using var response = await client.GetAsync(
			new Uri("/Account/Logout", UriKind.Relative), TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Redirect);
		response.Headers.Location.Should().NotBeNull();
		response.Headers.Location!.OriginalString.Should().Be("/");
	}

	private static WebApplicationFactory<Program> CreateFactory(string domain, string clientId, string clientSecret)
	{
		return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
			.UseSetting("Auth0:Domain", domain)
			.UseSetting("Auth0:ClientId", clientId)
			.UseSetting("Auth0:ClientSecret", clientSecret));
	}
}
