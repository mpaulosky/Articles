using System.Security.Claims;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Web.Components.Features.UserManagement;

namespace Web.UI.Tests;

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
		var user = new ClaimsPrincipal(new ClaimsIdentity(
			new[]
			{
				new Claim(ClaimTypes.Name, "Test User"),
				new Claim(ClaimTypes.Email, "test@example.com"),
				new Claim(ClaimTypes.NameIdentifier, "auth0|123456")
			},
			"TestAuthType"));

		AddAuthorization();
		Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(user));
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();

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
		var user = new ClaimsPrincipal(new ClaimsIdentity(
			new[]
			{
				new Claim(ClaimTypes.Name, "Admin User"),
				new Claim(ClaimTypes.Email, "admin@example.com"),
				new Claim(ClaimTypes.Role, "Admin")
			},
			"TestAuthType"));

		AddAuthorization();
		Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(user));
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();

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

		AddAuthorization();
		Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(anonymousUser));
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();

		// Act
		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.Profile>());

		// Assert
		// Anonymous users see the profile page but with empty state
		cut.Markup.Should().Contain("Unknown User");
		cut.Markup.Should().Contain("No claims were found");
	}
}
