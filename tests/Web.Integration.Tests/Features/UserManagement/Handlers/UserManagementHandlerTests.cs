// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserManagementHandlerTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

using Auth0.ManagementApi;
using Auth0.ManagementApi.Core;
using Auth0.ManagementApi.Users;

using NSubstitute;

using Web.Components.Features.UserManagement.Auth0;
using Web.Components.Features.UserManagement.ManageRoles;
using Web.Components.Features.UserManagement.Models;
using Web.Integration.Tests.Fixtures;

using UserRolesClient = Auth0.ManagementApi.Users.IRolesClient;
using TopLevelRolesClient = Auth0.ManagementApi.IRolesClient;
using RawResponse = Auth0.ManagementApi.RawResponse;

namespace Web.Integration.Tests.Features.UserManagement.Handlers;

/// <summary>
///     Exercises <c>UserManagementHandler</c> through the real mediator pipeline (as wired in
///     <c>Program.cs</c>: <c>AddMyMediator</c> plus <c>LoggingBehavior</c>) and the real
///     <c>UserManagementCacheService</c>, instead of calling the handler directly. Unlike the
///     Article/Category handler tickets, this handler has no MongoDB dependency: it calls the Auth0
///     Management API, which isn't available in CI, so only <see cref="IManagementApiClientFactory" />
///     is substituted. Business-logic edge cases (factory failure branches) are already covered at the
///     unit level in <c>Web.Tests</c>; these tests confirm the pipeline wiring and the real cache work
///     for every request type the handler serves.
/// </summary>
public class UserManagementHandlerTests
{
	[Fact]
	public async Task SendAsync_GetUsersWithRolesQuery_ReturnsUsersWithRolesFromTheRealCacheAsync()
	{
		// Arrange
		var alice = new UserResponseSchema { UserId = "auth0|alice", Email = "alice@example.com", Name = "Alice" };

		var usersClient = Substitute.For<IUsersClient>();
		usersClient.ListAsync(Arg.Any<ListUsersRequestParameters>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<Pager<UserResponseSchema>>(new FakePager<UserResponseSchema>([alice])));

		var userRolesClient = Substitute.For<UserRolesClient>();
		userRolesClient.ListAsync("auth0|alice", Arg.Any<ListUserRolesRequestParameters>(), Arg.Any<RequestOptions>(),
				Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<Pager<Role>>(new FakePager<Role>([new Role { Id = "rol_1", Name = "Admin" }])));
		usersClient.Roles.Returns(userRolesClient);

		var managementClient = Substitute.For<IManagementApiClient>();
		managementClient.Users.Returns(usersClient);

		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(managementClient);

		await using var host = UserManagementTestHost.Create(managementApiClientFactory);

		// Act
		var result = await host.Mediator.Send(new GetUsersWithRolesQuery(), TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		var user = result.Value.Should().ContainSingle().Subject;
		user.UserId.Should().Be("auth0|alice");
		user.Roles.Should().Equal("Admin");
	}

	[Fact]
	public async Task SendAsync_GetAvailableRolesQuery_ReturnsRolesFromTheRealCacheAsync()
	{
		// Arrange
		var rolesClient = Substitute.For<TopLevelRolesClient>();
		rolesClient.ListAsync(Arg.Any<ListRolesRequestParameters>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<Pager<Role>>(new FakePager<Role>([new Role { Id = "rol_1", Name = "Admin" }])));

		var managementClient = Substitute.For<IManagementApiClient>();
		managementClient.Roles.Returns(rolesClient);

		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(managementClient);

		await using var host = UserManagementTestHost.Create(managementApiClientFactory);

		// Act
		var result = await host.Mediator.Send(new GetAvailableRolesQuery(), TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		var role = result.Value.Should().ContainSingle().Subject;
		role.Should().Be(new RoleDto("rol_1", "Admin"));
	}

	[Fact]
	public async Task SendAsync_AssignRoleCommand_AssignsRoleThroughTheRealPipelineAsync()
	{
		// Arrange
		var userRolesClient = Substitute.For<UserRolesClient>();
		userRolesClient.AssignAsync(Arg.Any<string>(), Arg.Any<AssignUserRolesRequestContent>(),
				Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
			.Returns(new WithRawResponseTask(Task.FromResult(CreateRawResponse())));

		var usersClient = Substitute.For<IUsersClient>();
		usersClient.Roles.Returns(userRolesClient);

		var managementClient = Substitute.For<IManagementApiClient>();
		managementClient.Users.Returns(usersClient);

		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(managementClient);

		await using var host = UserManagementTestHost.Create(managementApiClientFactory);
		var command = new AssignRoleCommand("auth0|12345", "rol_abc123");

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		_ = userRolesClient.Received(1).AssignAsync(
			"auth0|12345",
			Arg.Is<AssignUserRolesRequestContent>(c => c.Roles!.Single() == "rol_abc123"),
			Arg.Any<RequestOptions>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task SendAsync_RemoveRoleCommand_RemovesRoleThroughTheRealPipelineAsync()
	{
		// Arrange
		var userRolesClient = Substitute.For<UserRolesClient>();
		userRolesClient.DeleteAsync(Arg.Any<string>(), Arg.Any<DeleteUserRolesRequestContent>(),
				Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
			.Returns(new WithRawResponseTask(Task.FromResult(CreateRawResponse())));

		var usersClient = Substitute.For<IUsersClient>();
		usersClient.Roles.Returns(userRolesClient);

		var managementClient = Substitute.For<IManagementApiClient>();
		managementClient.Users.Returns(usersClient);

		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(managementClient);

		await using var host = UserManagementTestHost.Create(managementApiClientFactory);
		var command = new RemoveRoleCommand("auth0|12345", "rol_abc123");

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		_ = userRolesClient.Received(1).DeleteAsync(
			"auth0|12345",
			Arg.Is<DeleteUserRolesRequestContent>(c => c.Roles!.Single() == "rol_abc123"),
			Arg.Any<RequestOptions>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	///     Creates a <see cref="RawResponse" /> without going through its constructor, since its required
	///     <c>Headers</c> member (<c>ResponseHeaders</c>) has no accessible public constructor for tests to supply.
	///     Production code never inspects the raw response on the success path exercised here.
	/// </summary>
	private static RawResponse CreateRawResponse() =>
		(RawResponse)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(RawResponse));

	private sealed class FakePager<T>(IReadOnlyList<T> items) : Pager<T>
	{
		public Page<T> CurrentPage => throw new NotSupportedException();

		public bool HasNextPage => false;

		public Task<Page<T>> GetNextPageAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

		public IAsyncEnumerable<Page<T>> AsPagesAsync(CancellationToken cancellationToken) =>
			throw new NotSupportedException();

		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			await Task.Yield();
			foreach (var item in items)
			{
				yield return item;
			}
		}
	}
}
