using Bunit;
using FluentAssertions;
using Web.Components.Shared;

namespace Web.UI.Tests;

public class LoadingComponentTests : BunitContext
{
	[Fact]
	public void RendersLoadingSpinner()
	{
		// Arrange & Act
		var cut = Render<LoadingComponent>();

		// Assert
		cut.Find("svg").Should().NotBeNull();
		cut.Find("svg").ClassList.Should().Contain("animate-spin");
	}

	[Fact]
	public void DisplaysLoadingMessage()
	{
		// Arrange & Act
		var cut = Render<LoadingComponent>();

		// Assert
		cut.Find("h3").TextContent.Should().Contain("Loading...");
	}
}
