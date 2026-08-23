// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Auth0ManagementApiClientFactoryTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using System.Net;
using System.Text;

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
		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		var factory = new Auth0ManagementApiClientFactory(configuration, httpClientFactory);

		// Act
		var act = () => factory.CreateAsync(CancellationToken.None);

		// Assert
		var exception = await act.Should().ThrowAsync<InvalidOperationException>();
		exception.Which.Message.Should().Contain($"{expectedMissingSetting} not configured.");
	}

	[Fact]
	public async Task CreateAsync_WhenTokenEndpointReturnsErrorStatus_ThrowsHttpRequestException()
	{
		// Arrange
		var configuration = CreateConfiguration();
		var httpClientFactory = CreateHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
		var factory = new Auth0ManagementApiClientFactory(configuration, httpClientFactory);

		// Act
		var act = () => factory.CreateAsync(CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<HttpRequestException>();
	}

	[Fact]
	public async Task CreateAsync_WhenTokenResponseHasNoAccessToken_ThrowsWithTokenErrorMessage()
	{
		// Arrange
		var configuration = CreateConfiguration();
		var httpClientFactory = CreateHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("{}", Encoding.UTF8, "application/json")
		});
		var factory = new Auth0ManagementApiClientFactory(configuration, httpClientFactory);

		// Act
		var act = () => factory.CreateAsync(CancellationToken.None);

		// Assert
		var exception = await act.Should().ThrowAsync<InvalidOperationException>();
		exception.Which.Message.Should().Be("Auth0 Management API token response did not contain a valid access_token.");
	}

	[Fact]
	public async Task CreateAsync_WhenTokenRequestIsCanceled_PropagatesOperationCanceledException()
	{
		// Arrange
		var configuration = CreateConfiguration();
		var httpClientFactory = CreateHttpClientFactory(_ => throw new TaskCanceledException("Request timed out."));
		var factory = new Auth0ManagementApiClientFactory(configuration, httpClientFactory);

		// Act
		var act = () => factory.CreateAsync(CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task CreateAsync_WhenTokenResponseHasAccessToken_ReturnsClient()
	{
		// Arrange
		var configuration = CreateConfiguration();
		var httpClientFactory = CreateHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("""{"access_token":"a-valid-token"}""", Encoding.UTF8, "application/json")
		});
		var factory = new Auth0ManagementApiClientFactory(configuration, httpClientFactory);

		// Act
		var client = await factory.CreateAsync(CancellationToken.None);

		// Assert
		client.Should().NotBeNull();
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

	private static IHttpClientFactory CreateHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> handler)
	{
		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		httpClientFactory.CreateClient().Returns(new HttpClient(new StubHttpMessageHandler(handler)));
		return httpClientFactory;
	}

	private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
		: HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			try
			{
				return Task.FromResult(handler(request));
			}
#pragma warning disable CA1031 // Intentional: forwards any stub-configured exception into the returned Task
			catch (Exception ex)
			{
				return Task.FromException<HttpResponseMessage>(ex);
			}
#pragma warning restore CA1031
		}
	}
}
