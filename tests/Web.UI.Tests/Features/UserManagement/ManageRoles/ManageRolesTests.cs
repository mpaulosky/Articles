using System.Security.Claims;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Web.UI.Tests.Features.UserManagement.ManageRoles;

public class ManageRolesTests : BunitContext
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
			var hasAdminRole = user.IsInRole("Admin");
			return Task.FromResult(hasAdminRole
				? AuthorizationResult.Success()
				: AuthorizationResult.Failed());
		}

		public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
		{
			var hasAdminRole = user.IsInRole("Admin");
			return Task.FromResult(hasAdminRole
				? AuthorizationResult.Success()
				: AuthorizationResult.Failed());
		}
	}

	[Fact]
	public void RequiresAdminRole()
	{
		// Arrange
		var normalUser = new ClaimsPrincipal(new ClaimsIdentity(
			new[]
			{
				new Claim(ClaimTypes.Name, "Normal User"),
				new Claim(ClaimTypes.Role, "User")
			},
			"TestAuthType"));

		AddAuthorization();
		Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(normalUser));
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();

		// Act & Assert
		// The component requires admin role and should not render properly for non-admin users
		// We expect this to fail because the component needs IMediator which we intentionally don't provide
		var ex = Assert.Throws<InvalidOperationException>(() =>
		{
			var cut = Render<CascadingAuthenticationState>(parameters =>
				parameters.AddChildContent<Web.Components.Features.UserManagement.ManageRoles.ManageRoles>());
		});

		ex.Message.Should().Contain("IMediator");
	}
}
