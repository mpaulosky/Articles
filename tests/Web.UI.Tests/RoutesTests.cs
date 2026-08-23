using Bunit;
using Bunit.TestDoubles;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Web.Components;

namespace Web.UI.Tests;

public class RoutesTests : BunitContext
{
	public RoutesTests()
	{
		// MainLayout's NavMenu calls JS interop for theme detection; not relevant to what's under test here.
		JSInterop.Mode = JSRuntimeMode.Loose;

		// MainLayout's NavMenu injects IConfiguration; DI needs it resolvable even though Routes.razor doesn't use it.
		Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

		// AuthorizeRouteView renders its NotAuthorized content inside MainLayout, whose Footer needs this.
		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		httpClientFactory.CreateClient().Returns(new HttpClient(new StubHttpMessageHandler(
			_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound))));
		Services.AddSingleton(httpClientFactory);
	}

	[Fact]
	public void ShowsLoginLink_WhenUserIsNotAuthenticatedAndRouteRequiresAuthorization()
	{
		// Arrange
		AddAuthorization().SetNotAuthorized();
		Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/categories");

		// Act
		var cut = Render<Routes>();

		// Assert
		var loginLink = cut.Find("a[aria-label='Login']");
		loginLink.GetAttribute("href").Should().Be("/Account/Login");
		cut.FindAll("p[role='alert']").Should().BeEmpty();
	}

	[Fact]
	public void ShowsNotAuthorizedAlert_WhenUserIsAuthenticatedButLacksRequiredRole()
	{
		// Arrange
		AddAuthorization().SetAuthorized("regular-user");
		Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/categories");

		// Act
		var cut = Render<Routes>();

		// Assert
		var alert = cut.Find("p[role='alert']");
		alert.TextContent.Should().Be("You are not authorized to access this resource.");
		cut.FindAll("a[aria-label='Login']").Should().BeEmpty();
	}

	private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
		: HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(handler(request));
		}
	}
}
