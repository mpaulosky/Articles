using Bunit;
using FluentAssertions;

namespace Web.UI.Tests;

public class MainLayoutThemeTests : BunitContext
{
	[Fact]
	public void MainLayoutRendersNavigationAndContentShell()
	{
		// Arrange
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = Render<Web.Components.Layout.MainLayout>();

		// Assert
		cut.Find("header").Should().NotBeNull();
		cut.Find("nav").TextContent.Should().Contain("Overview");
		cut.Find("nav").TextContent.Should().Contain("Counter");
		cut.Find("main").Should().NotBeNull();
		cut.Find("article").Should().NotBeNull();
		cut.Find("header").ClassList.Should().Contain("app-header");
		cut.Markup.Should().Contain("Articles");
	}

	[Fact]
	public void ThemeToggleSwitchesBetweenLightAndDarkState()
	{
		// Arrange
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = Render<Web.Components.Layout.MainLayout>();
		var toggle = cut.Find("button[aria-label='Switch to dark theme']");

		// Act
		cut.WaitForAssertion(() => toggle.TextContent.Trim().Should().Be("🌙"));
		toggle.Click();

		// Assert
		cut.WaitForAssertion(() => cut.Find("button[aria-label='Switch to light theme']").TextContent.Trim().Should().Be("☀️"));
		var applyThemeInvocation = JSInterop.Invocations
			.Where(invocation => invocation.Identifier == "applyTheme")
			.LastOrDefault();

		applyThemeInvocation.Should().NotBeNull();
		applyThemeInvocation!.Arguments.Count.Should().Be(1);
		applyThemeInvocation.Arguments[0].Should().Be("dark");
	}

	[Fact]
	public void DefaultThemeUsesJavaScriptThemeStateWhenRendering()
	{
		// Arrange
		JSInterop.Setup<string>("getTheme").SetResult("dark");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = Render<Web.Components.Layout.MainLayout>();

		// Assert
		cut.WaitForAssertion(() => cut.Find("button[aria-label='Switch to light theme']").TextContent.Trim().Should().Be("☀️"));
	}

	[Fact]
	public void MainLayoutUsesThemeHooksWithoutHardcodedCssAssertions()
	{
		// Arrange
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = Render<Web.Components.Layout.MainLayout>();
		var shell = cut.FindAll("div").FirstOrDefault(div =>
			div.ClassList.Contains("app-page"));

		// Assert
		shell.Should().NotBeNull();
		cut.Find("button[aria-label='Switch to dark theme']").Should().NotBeNull();
		cut.Find("nav").ClassList.Should().Contain("hidden");
		cut.Find("nav").ClassList.Should().Contain("app-nav");
	}
}
