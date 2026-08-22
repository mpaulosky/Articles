using System.Security.Claims;

using Bunit;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Web.Components.Features.UserManagement;

namespace Web.UI.Tests.Features.UserManagement;

public class ProfileTests : BunitContext
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
	public void LoadsCurrentUserProfile()
	{
		// Arrange
		var user = CreateUser(
			new Claim(ClaimTypes.Name, "Test User"),
			new Claim(ClaimTypes.Email, "test@example.com"),
			new Claim(ClaimTypes.NameIdentifier, "auth0|123456"));

		SetAuthenticatedUser(user);

		// Act
		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.Profile>());

		// Assert
		cut.Markup.Should().Contain("Test User");
		cut.Markup.Should().Contain("test@example.com");
	}

	[Fact]
	public void DisplaysUserClaimsWhenAuthenticated()
	{
		// Arrange
		var user = CreateUser(
			new Claim(ClaimTypes.Name, "Admin User"),
			new Claim(ClaimTypes.Email, "admin@example.com"),
			new Claim(ClaimTypes.Role, "Admin"));

		SetAuthenticatedUser(user);

		// Act
		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.Profile>());

		// Assert
		cut.Markup.Should().Contain("Admin User");
		cut.Markup.Should().Contain("Claims");
		cut.Markup.Should().Contain("Admin");
	}

	[Fact]
	public void RedirectsToLoginWhenAnonymous()
	{
		// Arrange
		var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
		SetAuthenticatedUser(anonymousUser);

		// Act
		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.Profile>());

		// Assert
		cut.Markup.Should().Contain("Unknown User");
		cut.Markup.Should().Contain("No claims were found");
	}

	[Fact]
	public void RendersPictureAvatar_WhenPictureClaimExists()
	{
		// Arrange
		var user = CreateUser(
			new Claim(ClaimTypes.Name, "Jane Doe"),
			new Claim("picture", "https://cdn.example.com/jane.png"));
		SetAuthenticatedUser(this, user);

		// Act
		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.Profile>());

		// Assert
		var img = cut.Find("img");
		img.GetAttribute("src").Should().Be("https://cdn.example.com/jane.png");
		img.GetAttribute("alt").Should().Be("Jane Doe profile image");
	}

	[Fact]
	public void FallsBackToInitialsAvatar_WhenPictureClaimIsMissing_UsingDisplayNameThenEmailLocalPart()
	{
		// Arrange
		using var nameContext = new BunitContext();
		var userWithDisplayName = CreateUser(
			new Claim(ClaimTypes.Name, "Mary Jane Watson"));
		SetAuthenticatedUser(nameContext, userWithDisplayName);

		// Act
		var nameCut = nameContext.Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.Profile>());

		// Assert
		nameCut.Markup.Should().Contain("MJ");

		// Arrange
		using var emailContext = new BunitContext();
		var userWithoutName = CreateUser(
			new Claim(ClaimTypes.Email, "alicia.smith@example.com"));
		SetAuthenticatedUser(emailContext, userWithoutName);

		// Act
		var emailCut = emailContext.Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.Profile>());

		// Assert
		emailCut.Markup.Should().Contain("AS");
	}

	[Fact]
	public void RendersRoleBadges_WithAdminAndNonAdminStyles()
	{
		// Arrange
		var user = CreateUser(
			new Claim(ClaimTypes.Name, "Admin User"),
			new Claim(ClaimTypes.Role, "Admin"),
			new Claim(ClaimTypes.Role, "Editor"));
		SetAuthenticatedUser(this, user);

		// Act
		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.Profile>());

		// Assert
		cut.Markup.Should().Contain("bg-red-700");
		cut.Markup.Should().Contain("bg-green-700");
		cut.Markup.Should().Contain("Admin");
		cut.Markup.Should().Contain("Editor");
	}

	[Fact]
	public void ResolvesValidEmailFallbacks_AndSkipsBadValues()
	{
		// Arrange
		var user = CreateUser(
			new Claim("upn", "invalid name"),
			new Claim("preferred_username", "user without email"),
			new Claim("emails", "[\"bad-value\",\"user@example.com\"]"));
		SetAuthenticatedUser(this, user);

		// Act
		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.Profile>());

		// Assert
		cut.Markup.Should().Contain("user@example.com");
		cut.Markup.Should().NotContain("No email claim found");
	}

	[Fact]
	public void FiltersSensitiveClaims_AndSortsRemainingClaimsByTypeThenValue()
	{
		// Arrange
		var user = CreateUser(
			new Claim("nonce", "secret"),
			new Claim("name", "Alice"),
			new Claim("b", "beta"),
			new Claim("a", "alpha"),
			new Claim("exp", "123"),
			new Claim("z", "zeta"));
		SetAuthenticatedUser(this, user);

		// Act
		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.Profile>());

		// Assert
		var rows = cut.FindAll("tbody tr");
		var normalizedRows = rows
			.Select(row => string.Join(' ', row.TextContent.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)))
			.ToList();

		normalizedRows.Should().ContainInOrder(
			"a alpha",
			"b beta",
			"name Alice",
			"z zeta");
		rows.Should().NotContain(row => row.TextContent.Contains("nonce", StringComparison.OrdinalIgnoreCase));
		rows.Should().NotContain(row => row.TextContent.Contains("exp", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void LeavesLoadingState_WhenAuthenticationStateTaskIsNull()
	{
		// Act
		var cut = Render<Web.Components.Features.UserManagement.Profile>();

		// Assert
		cut.Markup.Should().Contain("Loading profile...");
	}

	private static ClaimsPrincipal CreateUser(params Claim[] claims)
	{
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
	}

	private void SetAuthenticatedUser(ClaimsPrincipal user)
	{
		AddAuthorization();
		Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(user));
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
	}

	private static void SetAuthenticatedUser(BunitContext context, ClaimsPrincipal user)
	{
		context.AddAuthorization();
		context.Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(user));
		context.Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
	}
}
