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
	public void RedirectsToLogin_WhenUserIsNotAuthenticatedAndRouteRequiresAuthorization()
	{
		// Arrange
		AddAuthorization().SetNotAuthorized();
		var nav = Services.GetRequiredService<BunitNavigationManager>();
		nav.NavigateTo("/categories");

		// Act
		Render<Routes>();

		// Assert
		nav.Uri.Should().Contain("Account/Login");
	}

	[Fact]
	public void RedirectsToNotAuthorized_WhenUserIsAuthenticatedButLacksRequiredRole()
	{
		// Arrange
		AddAuthorization().SetAuthorized("regular-user");
		var nav = Services.GetRequiredService<BunitNavigationManager>();
		nav.NavigateTo("/categories");

		// Act
		Render<Routes>();

		// Assert
		nav.Uri.Should().Contain("not-authorized");
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
