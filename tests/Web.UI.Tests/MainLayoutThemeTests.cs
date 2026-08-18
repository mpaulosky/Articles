using System.Security.Claims;

using Bunit;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Web.UI.Tests;

public class MainLayoutThemeTests : BunitContext
{
	private sealed class TestAuthStateProvider : AuthenticationStateProvider
	{
		private readonly ClaimsPrincipal _user = new(new ClaimsIdentity(
			new[] { new Claim(ClaimTypes.Name, "test-user") },
			"TestAuthType"));

		public override Task<AuthenticationState> GetAuthenticationStateAsync()
		{
			return Task.FromResult(new AuthenticationState(_user));
		}
	}

	private sealed class TestAuthorizationService : IAuthorizationService
	{
		public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
		{
			return Task.FromResult(AuthorizationResult.Success());
		}

		public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
		{
			return Task.FromResult(AuthorizationResult.Success());
		}
	}

	private void RegisterTestConfiguration()
	{
		AddAuthorization();
		Services.AddSingleton<AuthenticationStateProvider, TestAuthStateProvider>();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Auth0:Domain"] = "", ["Auth0:ClientId"] = "", ["Auth0:ClientSecret"] = ""
			})
			.Build());
	}

	private IRenderedComponent<CascadingAuthenticationState> RenderMainLayout()
	{
		return Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Layout.MainLayout>());
	}

	[Fact]
	public void MainLayoutRendersNavigationAndContentShell()
	{
		// Arrange
		RegisterTestConfiguration();
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = RenderMainLayout();

		// Assert
		cut.Find("header").Should().NotBeNull();
		cut.Find("nav").TextContent.Should().Contain("Overview");
		cut.Find("nav").TextContent.Should().Contain("test-user");
		cut.Find("nav").TextContent.Should().Contain("Logout");
		cut.Find("main").Should().NotBeNull();
		cut.Find("article").Should().NotBeNull();
		cut.Find("header").ClassList.Should().Contain("app-header");
		cut.Markup.Should().Contain("Articles");
	}

	[Fact]
	public void ThemeToggleSwitchesBetweenLightAndDarkState()
	{
		// Arrange
		RegisterTestConfiguration();
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = RenderMainLayout();
		var toggle = cut.Find("button[aria-label='Switch to dark theme']");

		// Act
		cut.WaitForAssertion(() => toggle.TextContent.Trim().Should().Be("🌙"));
		toggle.Click();

		// Assert
		cut.WaitForAssertion(() =>
			cut.Find("button[aria-label='Switch to light theme']").TextContent.Trim().Should().Be("☀️"));
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
		RegisterTestConfiguration();
		JSInterop.Setup<string>("getTheme").SetResult("dark");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = RenderMainLayout();

		// Assert
		cut.WaitForAssertion(() =>
			cut.Find("button[aria-label='Switch to light theme']").TextContent.Trim().Should().Be("☀️"));
	}

	[Fact]
	public void MainLayoutRendersPageShellWithHiddenNavAndDarkThemeToggle()
	{
		// Arrange
		RegisterTestConfiguration();
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = RenderMainLayout();
		var shell = cut.FindAll("div").FirstOrDefault(div =>
			div.ClassList.Contains("app-page"));

		// Assert
		shell.Should().NotBeNull();
		cut.Find("button[aria-label='Switch to dark theme']").Should().NotBeNull();
		cut.Find("nav").ClassList.Should().Contain("hidden");
		cut.Find("nav").ClassList.Should().Contain("app-nav");
	}
}
