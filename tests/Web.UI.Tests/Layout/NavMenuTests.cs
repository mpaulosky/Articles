using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Security.Claims;
using Web.Components.Layout;

namespace Web.UI.Tests.Layout;

public sealed class NavMenuTests : BunitContext
{
	private sealed class TestAuthStateProvider : AuthenticationStateProvider
	{
		private readonly ClaimsPrincipal _user;

		public TestAuthStateProvider(ClaimsPrincipal user)
		{
			_user = user;
		}

		public override Task<AuthenticationState> GetAuthenticationStateAsync()
		{
			return Task.FromResult(new AuthenticationState(_user));
		}
	}

	private sealed class TestAuthorizationService : IAuthorizationService
	{
		public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource,
			IEnumerable<IAuthorizationRequirement> requirements)
		{
			return Task.FromResult(user.Identity?.IsAuthenticated == true
				? AuthorizationResult.Success()
				: AuthorizationResult.Failed());
		}

		public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
		{
			return Task.FromResult(user.Identity?.IsAuthenticated == true
				? AuthorizationResult.Success()
				: AuthorizationResult.Failed());
		}
	}

	[Fact]
	public void RendersWithoutErrors()
	{
		// Arrange
		SetupServices(CreateAnonymousUser());

		// Act
		var cut = Render<NavMenu>();

		// Assert
		cut.Should().NotBeNull();
		cut.Markup.Should().Contain("Articles");
	}

	[Fact]
	public void DisplaysBrandLogo_AndName()
	{
		// Arrange
		SetupServices(CreateAnonymousUser());

		// Act
		var cut = Render<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("app-brand");
		cut.Markup.Should().Contain("Articles");
		cut.Markup.Should().Contain("app-brand-mark");
	}

	[Fact]
	public void DisplaysNavigationLinks()
	{
		// Arrange
		SetupServices(CreateAnonymousUser());

		// Act
		var cut = Render<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("Overview");
		cut.Markup.Should().Contain("Articles");
		cut.Markup.Should().Contain("Categories");
		cut.FindAll("a[href='/']").Should().HaveCountGreaterThanOrEqualTo(1, "should have home link");
		cut.FindAll("a[href='/articles']").Should().HaveCount(1, "should have articles link");
		cut.FindAll("a[href='/categories']").Should().HaveCount(1, "should have categories link");
	}

	[Fact]
	public void ShowsLoginLink_WhenUserIsAnonymous()
	{
		// Arrange
		SetupServices(CreateAnonymousUser());

		// Act
		var cut = Render<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("Login");
		cut.Markup.Should().Contain("/Account/Login");
		cut.Markup.Should().NotContain("Logout");
	}

	[Fact]
	public void ShowsLogoutLink_WhenUserIsAuthenticated()
	{
		// Arrange
		var user = new ClaimsPrincipal(new ClaimsIdentity(
			new[]
			{
				new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
				new Claim(ClaimTypes.Name, "Test User")
			},
			"TestAuthType"));

		SetupServices(user);

		// Act
		var cut = Render<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("Logout");
		cut.Markup.Should().Contain("/Account/Logout");
		cut.Markup.Should().NotContain("Login");
	}

	[Fact]
	public void ShowsProfileLink_WhenUserIsAuthenticated()
	{
		// Arrange
		var user = new ClaimsPrincipal(new ClaimsIdentity(
			new[]
			{
				new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
				new Claim(ClaimTypes.Name, "John Doe")
			},
			"TestAuthType"));

		SetupServices(user);

		// Act
		var cut = Render<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("John Doe");
		cut.Markup.Should().Contain("/profile");
	}

	[Fact]
	public void DoesNotShowProfileLink_WhenUserIsAnonymous()
	{
		// Arrange
		SetupServices(CreateAnonymousUser());

		// Act
		var cut = Render<NavMenu>();

		// Assert
		cut.Markup.Should().NotContain("/profile");
	}

	[Fact]
	public void DisplaysThemeToggleButton()
	{
		// Arrange
		SetupServices(CreateAnonymousUser());

		// Act
		var cut = Render<NavMenu>();

		// Assert
		var themeButtons = cut.FindAll("button.theme-toggle");
		themeButtons.Should().HaveCount(1, "should have one theme toggle button");
	}

	[Fact]
	public void NavHasProperStructure()
	{
		// Arrange
		SetupServices(CreateAnonymousUser());

		// Act
		var cut = Render<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("<header");
		cut.Markup.Should().Contain("</header>");
		cut.Markup.Should().Contain("app-header");
		cut.Markup.Should().Contain("<nav");
		cut.Markup.Should().Contain("</nav>");
	}

	private IRenderedComponent<NavMenu> Render<T>() where T : NavMenu
	{
		return base.Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<T>()).FindComponent<T>();
	}

	private void SetupServices(ClaimsPrincipal user)
	{
		// Setup JSInterop for theme management
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Auth0:Domain"] = "test.auth0.com",
				["Auth0:ClientId"] = "test-client-id"
			})
			.Build();

		AddAuthorization();
		Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(user));
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		Services.AddSingleton<IConfiguration>(configuration);
	}

	private static ClaimsPrincipal CreateAnonymousUser()
	{
		return new ClaimsPrincipal(new ClaimsIdentity());
	}
}
