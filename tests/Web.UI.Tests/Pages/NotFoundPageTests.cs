using Bunit;
using FluentAssertions;
using Web.Components.Pages;

namespace Web.UI.Tests.Pages;

public class NotFoundPageTests : BunitContext
{
	[Fact]
	public void Renders404Message()
	{
		// Arrange & Act
		var cut = Render<NotFound>();

		// Assert
		cut.Find("h3").TextContent.Should().Contain("Not Found");
		cut.Markup.Should().Contain("Sorry, the content you are looking for does not exist");
	}

	[Fact]
	public void DisplaysNavigationLink()
	{
		// Arrange & Act
		var cut = Render<NotFound>();

		// Assert
		cut.Markup.Should().Contain("Not Found");
		cut.Find("p").Should().NotBeNull();
	}
}
