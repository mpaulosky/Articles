// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Auth0ManagementApiClientFactoryTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using FluentAssertions;

using Microsoft.Extensions.Configuration;

using NSubstitute;

using Web.Components.Features.UserManagement.Auth0;

namespace Web.Tests.Features.UserManagement.Auth0;

public class Auth0ManagementApiClientFactoryTests
{
	[Theory]
	[InlineData(false, true, true, "Auth0:Management:Domain")]
	[InlineData(true, false, true, "Auth0:Management:ClientId")]
	[InlineData(true, true, false, "Auth0:Management:ClientSecret")]
	public async Task CreateAsync_WhenManagementSettingIsMissing_ThrowsNamingThatSetting(
		bool hasDomain, bool hasClientId, bool hasClientSecret, string expectedMissingSetting)
	{
		// Arrange
		var configuration = CreateConfiguration(hasDomain, hasClientId, hasClientSecret);
		var httpClientFactory = CreateHttpClientFactory();
		using var factory = new Auth0ManagementApiClientFactory(configuration, httpClientFactory);

		// Act
		var act = () => factory.CreateAsync(CancellationToken.None);

		// Assert
		var exception = await act.Should().ThrowAsync<InvalidOperationException>();
		exception.Which.Message.Should().Contain($"{expectedMissingSetting} not configured.");
	}

	[Fact]
	public async Task CreateAsync_WhenSettingsArePresent_ReturnsClientWithoutContactingAuth0()
	{
		// Arrange: the access token is now exchanged lazily on the client's first API call, not
		// during CreateAsync, so no HTTP call should happen here at all.
		var configuration = CreateConfiguration();
		var httpClientFactory = CreateHttpClientFactory();
		using var factory = new Auth0ManagementApiClientFactory(configuration, httpClientFactory);

		// Act
		var client = await factory.CreateAsync(CancellationToken.None);

		// Assert
		client.Should().NotBeNull();
	}

	[Fact]
	public async Task CreateAsync_WhenCalledMultipleTimes_ReusesTheSameTokenProvider()
	{
		// Arrange: httpClientFactory.CreateClient() is called once to back the cached token
		// provider, plus once per CreateAsync call for the returned client's own HttpClient.
		// A fresh token provider per call (defeating the caching fix) would show up as an extra
		// CreateClient() call per invocation instead.
		var configuration = CreateConfiguration();
		var httpClientFactory = CreateHttpClientFactory();
		using var factory = new Auth0ManagementApiClientFactory(configuration, httpClientFactory);

		// Act
		await factory.CreateAsync(CancellationToken.None);
		await factory.CreateAsync(CancellationToken.None);

		// Assert
		httpClientFactory.Received(3).CreateClient();
	}

	private static IConfiguration CreateConfiguration(bool hasDomain = true, bool hasClientId = true,
		bool hasClientSecret = true)
	{
		var configuration = Substitute.For<IConfiguration>();
		if (hasDomain)
		{
			configuration["Auth0:Management:Domain"].Returns("test.auth0.com");
		}

		if (hasClientId)
		{
			configuration["Auth0:Management:ClientId"].Returns("test-client-id");
		}

		if (hasClientSecret)
		{
			configuration["Auth0:Management:ClientSecret"].Returns("test-client-secret");
		}

		return configuration;
	}

	private static IHttpClientFactory CreateHttpClientFactory()
	{
		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		httpClientFactory.CreateClient().Returns(_ => new HttpClient());
		return httpClientFactory;
	}
}
