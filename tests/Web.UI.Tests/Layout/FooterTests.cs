using Bunit;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using System.Net;

using Web.Components.Layout;
using Web.Services;

namespace Web.UI.Tests.Layout;

public sealed class FooterTests : BunitContext
{
	[Fact]
	public void RendersWithoutErrors()
	{
		// Arrange
		SetupHttpClientFactory();

		// Act
		var cut = Render<Footer>();

		// Assert
		cut.Should().NotBeNull();
		cut.Markup.Should().Contain("Articles");
		cut.Markup.Should().Contain("mpaulosky.org");
	}

	[Fact]
	public void DisplaysCopyrightWithCurrentYear()
	{
		// Arrange
		SetupHttpClientFactory();

		// Act
		var cut = Render<Footer>();

		// Assert
		var currentYear = DateTime.UtcNow.Year.ToString();
		cut.Markup.Should().Contain($"© {currentYear} Articles");
	}

	[Fact]
	public void DisplaysCompanyName()
	{
		// Arrange
		SetupHttpClientFactory();

		// Act
		var cut = Render<Footer>();

		// Assert
		cut.Markup.Should().Contain("from mpaulosky.org");
	}

	[Fact]
	public void DisplaysGitHubReleaseLink()
	{
		// Arrange
		SetupHttpClientFactory();

		// Act
		var cut = Render<Footer>();

		// Assert
		cut.Markup.Should().Contain("GitHub Release");
		cut.Markup.Should().Contain("https://github.com/mpaulosky/Articles/releases");
	}

	[Fact]
	public void DisplaysLastCommitLink()
	{
		// Arrange
		SetupHttpClientFactory();

		// Act
		var cut = Render<Footer>();

		// Assert
		cut.Markup.Should().Contain("Last Commit");
		cut.Markup.Should().Contain("https://github.com/mpaulosky/Articles/commit");
	}

	[Fact]
	public void UsesGitHubMetadata_WhenAvailable()
	{
		// Arrange
		var mockHandler = new MockHttpMessageHandler(new HttpResponseMessage
		{
			StatusCode = HttpStatusCode.OK,
			Content = new StringContent(@"{
				""tag_name"": ""v1.2.3"",
				""target_commitish"": ""abc123def456""
			}")
		});

		var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.github.com/") };
		var factory = Substitute.For<IHttpClientFactory>();
		factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
		Services.AddSingleton(factory);

		// Act
		var cut = Render<Footer>();

		// Assert - Should contain metadata when loaded (async operation)
		cut.Markup.Should().Contain("Articles");
	}

	[Fact]
	public void FallsBackToBuildInfo_WhenMetadataUnavailable()
	{
		// Arrange
		var mockHandler = new MockHttpMessageHandler(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

		var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.github.com/") };
		var factory = Substitute.For<IHttpClientFactory>();
		factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
		Services.AddSingleton(factory);

		// Act
		var cut = Render<Footer>();

		// Assert - Should still render with BuildInfo fallback
		cut.Markup.Should().Contain("Articles");
		cut.Markup.Should().Contain("GitHub Release");
	}

	[Fact]
	public void FooterHasProperStructure()
	{
		// Arrange
		SetupHttpClientFactory();

		// Act
		var cut = Render<Footer>();

		// Assert
		cut.Markup.Should().Contain("<footer");
		cut.Markup.Should().Contain("</footer>");
		cut.Markup.Should().Contain("app-footer");
	}

	private IRenderedComponent<Footer> Render<T>() where T : Footer
	{
		return base.Render<T>();
	}

	private void SetupHttpClientFactory()
	{
		var mockHandler = new MockHttpMessageHandler(new HttpResponseMessage
		{
			StatusCode = HttpStatusCode.OK,
			Content = new StringContent(@"{
				""tag_name"": ""v0.1.0"",
				""target_commitish"": ""main""
			}")
		});

		var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.github.com/") };
		var factory = Substitute.For<IHttpClientFactory>();
		factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
		Services.AddSingleton(factory);
	}

	private sealed class MockHttpMessageHandler : HttpMessageHandler
	{
		private readonly HttpResponseMessage _response;

		public MockHttpMessageHandler(HttpResponseMessage response)
		{
			_response = response;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(_response);
		}
	}
}
