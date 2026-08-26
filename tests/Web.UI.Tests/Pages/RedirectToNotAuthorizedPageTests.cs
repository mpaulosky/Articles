using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Web.Components.Pages;

namespace Web.UI.Tests.Pages;

public class RedirectToNotAuthorizedPageTests : BunitContext
{
	[Fact]
	public void NavigatesToNotAuthorized_WhenInitialized()
	{
		// Act
		Render<RedirectToNotAuthorizedPage>();

		// Assert
		var nav = Services.GetRequiredService<NavigationManager>();
		nav.Uri.Should().Contain("not-authorized");
	}
}
