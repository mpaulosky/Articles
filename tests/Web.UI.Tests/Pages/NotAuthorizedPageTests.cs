using Bunit;
using FluentAssertions;
using Web.Components.Pages;

namespace Web.UI.Tests.Pages;

public class NotAuthorizedPageTests : BunitContext
{
	[Fact]
	public void RendersNotAuthorizedMessage()
	{
		// Arrange & Act
		var cut = Render<NotAuthorizedPage>();

		// Assert
		cut.Find("h1").TextContent.Should().Contain("Not Authorized");
		cut.Markup.Should().Contain("You are not authorized to access this resource.");
	}
}
