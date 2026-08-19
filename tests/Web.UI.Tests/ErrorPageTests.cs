using System.Diagnostics;
using Bunit;
using FluentAssertions;
using Web.Components.Pages;

namespace Web.UI.Tests;

public class ErrorPageTests : BunitContext
{
	[Fact]
	public void RendersErrorMessage()
	{
		// Arrange & Act
		var cut = Render<Error>();

		// Assert
		cut.Find("h1").TextContent.Should().Contain("Error");
		cut.Find("h2").TextContent.Should().Contain("An error occurred while processing your request");
	}

	[Fact]
	public void DisplaysStackTraceInDevelopment()
	{
		// Arrange
		using var activity = new Activity("test");
		Activity.Current = activity.Start();

		// Act
		var cut = Render<Error>();

		// Assert
		cut.Markup.Should().Contain("Request ID:");
		cut.Markup.Should().Contain(Activity.Current.Id);

		// Cleanup
		Activity.Current.Stop();
		Activity.Current = null;
	}

	[Fact]
	public void ShowsDevelopmentModeMessage()
	{
		// Arrange
		Activity.Current = null;

		// Act
		var cut = Render<Error>();

		// Assert
		cut.Markup.Should().Contain("Development Mode");
		cut.Markup.Should().Contain("ASPNETCORE_ENVIRONMENT");
	}
}
