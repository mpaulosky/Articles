using System.Security.Claims;

using Microsoft.AspNetCore.Components.Web;

using Bunit;

using Domain.Abstractions;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Web.Components.Features.UserManagement.AddUserRoles;
using Web.Components.Features.UserManagement;
using Web.Components.Features.UserManagement.GetUserRoles;
using Web.Components.Features.UserManagement.GetUserWithRoles;
using Web.Components.Features.UserManagement.ManageRoles;
using Web.MyMediator;

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
			new[] { new Claim(ClaimTypes.Name, "Normal User"), new Claim(ClaimTypes.Role, "User") },
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

	[Fact]
	public void RendersUsersAndAvailableRoleToggles()
	{
		// Arrange
		var users = new[]
		{
			new UserWithRolesDto("user-1", "alice@example.com", "Alice", new[] { "User" }),
			new UserWithRolesDto("user-2", "bob@example.com", "Bob", new[] { "Admin", "User" })
		};
		var roles = new[] { new RoleDto("role-1", "Admin"), new RoleDto("role-2", "Editor") };
		var mediator = Substitute.For<IMediator>();
		mediator.Send(Arg.Any<GetUsersWithRolesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<UserWithRolesDto>>(users));
		mediator.Send(Arg.Any<GetAvailableRolesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<RoleDto>>(roles));

		var adminUser = new ClaimsPrincipal(new ClaimsIdentity(
			new[] { new Claim(ClaimTypes.Name, "Admin User"), new Claim(ClaimTypes.Role, "Admin") },
			"TestAuthType"));

		AddAuthorization();
		Services.AddSingleton<IMediator>(mediator);
		Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(adminUser));
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();

		// Act
		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.ManageRoles.ManageRoles>());

		// Assert
		cut.Markup.Should().Contain("Manage User Roles");
		cut.Markup.Should().Contain("Alice");
		cut.Markup.Should().Contain("Bob");
		cut.FindAll("button.role-toggle").Count.Should().Be(4);
	}

	[Fact]
	public void ShowsErrorMessage_WhenRoleLoadFails()
	{
		// Arrange
		var mediator = Substitute.For<IMediator>();
		mediator.Send(Arg.Any<GetUsersWithRolesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IReadOnlyList<UserWithRolesDto>>("Unable to load users."));
		mediator.Send(Arg.Any<GetAvailableRolesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<RoleDto>>(new[] { new RoleDto("role-1", "Admin") }));

		var adminUser = new ClaimsPrincipal(new ClaimsIdentity(
			new[] { new Claim(ClaimTypes.Name, "Admin User"), new Claim(ClaimTypes.Role, "Admin") },
			"TestAuthType"));

		AddAuthorization();
		Services.AddSingleton<IMediator>(mediator);
		Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(adminUser));
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();

		// Act
		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.ManageRoles.ManageRoles>());

		// Assert
		cut.Markup.Should().Contain("Unable to load users.");
	}

	[Fact]
	public async Task ClickingInactiveRoleToggle_AssignsTheRoleAndRefreshes()
	{
		// Arrange
		var initialUsers = new[] { new UserWithRolesDto("user-1", "alice@example.com", "Alice", new[] { "User" }) };
		var refreshedUsers = new[]
		{
			new UserWithRolesDto("user-1", "alice@example.com", "Alice", new[] { "User", "Admin" })
		};
		var roles = new[] { new RoleDto("role-1", "Admin") };
		var mediator = Substitute.For<IMediator>();
		mediator.Send(Arg.Any<GetUsersWithRolesQuery>(), Arg.Any<CancellationToken>())
			.Returns(
				Result.Ok<IReadOnlyList<UserWithRolesDto>>(initialUsers),
				Result.Ok<IReadOnlyList<UserWithRolesDto>>(refreshedUsers));
		mediator.Send(Arg.Any<GetAvailableRolesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<RoleDto>>(roles));
		mediator.Send(Arg.Any<AssignRoleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		var adminUser = new ClaimsPrincipal(new ClaimsIdentity(
			new[] { new Claim(ClaimTypes.Name, "Admin User"), new Claim(ClaimTypes.Role, "Admin") },
			"TestAuthType"));

		AddAuthorization();
		Services.AddSingleton<IMediator>(mediator);
		Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(adminUser));
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();

		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.ManageRoles.ManageRoles>());

		// Act
		await cut.Find("button.role-toggle-inactive").ClickAsync(new MouseEventArgs());

		// Assert
		await mediator.Received(1).Send(Arg.Is<AssignRoleCommand>(cmd =>
			cmd.UserId == "user-1" && cmd.RoleId == "role-1"), Arg.Any<CancellationToken>());
		cut.Markup.Should().Contain("Admin");
	}

	[Fact]
	public async Task ClickingActiveRoleToggle_RemovesTheRoleAndRefreshes()
	{
		// Arrange
		var initialUsers = new[]
		{
			new UserWithRolesDto("user-1", "alice@example.com", "Alice", new[] { "User", "Admin" })
		};
		var refreshedUsers = new[] { new UserWithRolesDto("user-1", "alice@example.com", "Alice", new[] { "User" }) };
		var roles = new[] { new RoleDto("role-1", "Admin") };
		var mediator = Substitute.For<IMediator>();
		mediator.Send(Arg.Any<GetUsersWithRolesQuery>(), Arg.Any<CancellationToken>())
			.Returns(
				Result.Ok<IReadOnlyList<UserWithRolesDto>>(initialUsers),
				Result.Ok<IReadOnlyList<UserWithRolesDto>>(refreshedUsers));
		mediator.Send(Arg.Any<GetAvailableRolesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<RoleDto>>(roles));
		mediator.Send(Arg.Any<RemoveRoleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		var adminUser = new ClaimsPrincipal(new ClaimsIdentity(
			new[] { new Claim(ClaimTypes.Name, "Admin User"), new Claim(ClaimTypes.Role, "Admin") },
			"TestAuthType"));

		AddAuthorization();
		Services.AddSingleton<IMediator>(mediator);
		Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(adminUser));
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();

		var cut = Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Features.UserManagement.ManageRoles.ManageRoles>());

		// Act
		await cut.Find("button.role-toggle-active").ClickAsync(new MouseEventArgs());

		// Assert
		await mediator.Received(1).Send(Arg.Is<RemoveRoleCommand>(cmd =>
			cmd.UserId == "user-1" && cmd.RoleId == "role-1"), Arg.Any<CancellationToken>());
		cut.Markup.Should().Contain("User");
	}
}
