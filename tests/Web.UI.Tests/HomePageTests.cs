using Bunit;
using FluentAssertions;
using Web.Components.Pages;

namespace Web.UI.Tests;

public class HomePageTests : BunitContext
{
	[Fact]
	public void RendersWelcomeMessage()
	{
		// Arrange & Act
		var cut = Render<Home>();

		// Assert
		cut.Find("h1").TextContent.Should().Contain("Articles");
		cut.Markup.Should().Contain("Build a durable foundation for modern .NET application work");
		cut.Markup.Should().Contain("Release ready");
	}

	[Fact]
	public void DisplaysNavigationLinks()
	{
		// Arrange & Act
		var cut = Render<Home>();

		// Assert
		cut.Markup.Should().Contain(".NET 10");
		cut.Markup.Should().Contain("Tailwind");
		cut.Markup.Should().Contain("Production ready");
		cut.Markup.Should().Contain("Project overview");
	}
}
