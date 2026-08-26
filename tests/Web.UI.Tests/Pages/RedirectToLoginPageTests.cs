using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Web.Components.Pages;

namespace Web.UI.Tests.Pages;

public class RedirectToLoginPageTests : BunitContext
{
	[Fact]
	public void NavigatesToLogin_WhenInitialized()
	{
		// Act
		Render<RedirectToLoginPage>();

		// Assert
		var nav = Services.GetRequiredService<NavigationManager>();
		nav.Uri.Should().Contain("Account/Login");
	}
}
