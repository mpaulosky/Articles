// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserManagementHandlerTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using Auth0.ManagementApi;
using Auth0.ManagementApi.Core;
using Auth0.ManagementApi.Users;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

using NSubstitute;

using Web.Components.Features.UserManagement.Auth0;
using Web.Components.Features.UserManagement.Caching.Interfaces;
using Web.Components.Features.UserManagement.ManageRoles;
using Web.Components.Features.UserManagement.Models;

using UserRolesClient = Auth0.ManagementApi.Users.IRolesClient;
using TopLevelRolesClient = Auth0.ManagementApi.IRolesClient;
using RawResponse = Auth0.ManagementApi.RawResponse;

namespace Web.Tests.Features.UserManagement.Handlers;

public class UserManagementHandlerTests
{
	[Fact]
	public async Task Handle_GetUsersWithRolesQuery_Success_MapsUsersAndTheirRolesIntoDtos()
	{
		// Arrange
		var alice = new UserResponseSchema { UserId = "auth0|alice", Email = "alice@example.com", Name = "Alice" };
		var bob = new UserResponseSchema { UserId = "auth0|bob", Email = "bob@example.com", Name = null };

		var usersClient = Substitute.For<IUsersClient>();
		usersClient.ListAsync(Arg.Any<ListUsersRequestParameters>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<Pager<UserResponseSchema>>(new FakePager<UserResponseSchema>([alice, bob])));

		var userRolesClient = Substitute.For<UserRolesClient>();
		userRolesClient.ListAsync("auth0|alice", Arg.Any<ListUserRolesRequestParameters>(), Arg.Any<RequestOptions>(),
				Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<Pager<Role>>(new FakePager<Role>([new Role { Id = "rol_1", Name = "Admin" }])));
		userRolesClient.ListAsync("auth0|bob", Arg.Any<ListUserRolesRequestParameters>(), Arg.Any<RequestOptions>(),
				Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<Pager<Role>>(new FakePager<Role>([
				new Role { Id = "rol_2", Name = "Editor" }, new Role { Id = "rol_3", Name = "Viewer" }
			])));
		usersClient.Roles.Returns(userRolesClient);

		var managementClient = Substitute.For<IManagementApiClient>();
		managementClient.Users.Returns(usersClient);

		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(managementClient);

		var cache = CreatePassThroughUsersCache();
		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var result = await handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);

		var aliceDto = result.Value!.Single(u => u.UserId == "auth0|alice");
		aliceDto.Name.Should().Be("Alice");
		aliceDto.Roles.Should().Equal("Admin");

		var bobDto = result.Value!.Single(u => u.UserId == "auth0|bob");
		bobDto.Name.Should().Be("bob@example.com", "the handler falls back to email when the user has no display name");
		bobDto.Roles.Should().Equal("Editor", "Viewer");
	}

	[Fact]
	public void GetRolesFetchConcurrency_WhenConfigured_ReturnsConfiguredValue()
	{
		// Arrange
		var configuration = Substitute.For<IConfiguration>();
		configuration["Auth0:Management:RolesFetchConcurrency"].Returns("3");

		// Act
		var concurrency = UserManagementHandler.GetRolesFetchConcurrency(configuration);

		// Assert
		concurrency.Should().Be(3);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("not-a-number")]
	[InlineData("0")]
	[InlineData("-1")]
	public void GetRolesFetchConcurrency_WhenNotConfiguredOrInvalid_FallsBackToDefault(string? configuredValue)
	{
		// Arrange
		var configuration = Substitute.For<IConfiguration>();
		configuration["Auth0:Management:RolesFetchConcurrency"].Returns(configuredValue);

		// Act
		var concurrency = UserManagementHandler.GetRolesFetchConcurrency(configuration);

		// Assert
		concurrency.Should().Be(5);
	}

	[Fact]
	public void GetRolesFetchConcurrency_WhenConfigurationIsNull_FallsBackToDefault()
	{
		// Act
		var concurrency = UserManagementHandler.GetRolesFetchConcurrency(null);

		// Assert
		concurrency.Should().Be(5);
	}

	[Fact]
	public async Task Handle_GetAvailableRolesQuery_Success_MapsRolesIntoDtos()
	{
		// Arrange
		var rolesClient = Substitute.For<TopLevelRolesClient>();
		rolesClient.ListAsync(Arg.Any<ListRolesRequestParameters>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<Pager<Role>>(new FakePager<Role>([
				new Role { Id = "rol_1", Name = "Admin" }, new Role { Id = "rol_2", Name = "Editor" }
			])));

		var managementClient = Substitute.For<IManagementApiClient>();
		managementClient.Roles.Returns(rolesClient);

		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>()).Returns(managementClient);

		var cache = CreatePassThroughRolesCache();
		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(
		[
			new RoleDto("rol_1", "Admin"), new RoleDto("rol_2", "Editor")
		]);
	}

	[Fact]
	public async Task Handle_AssignRoleCommand_Success_AssignsRoleAndInvalidatesUserCache()
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

		var cache = Substitute.For<IUserManagementCacheService>();
		cache.InvalidateUsersAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var result = await handler.Handle(new AssignRoleCommand("auth0|12345", "rol_abc123"), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		// Deliberately not awaited: WithRawResponseTask's default backing Task is null, and Received()'s
		// verification call returns that default rather than our configured value, so awaiting it here
		// throws NullReferenceException. Discard instead of `await`.
		_ = userRolesClient.Received(1).AssignAsync(
			"auth0|12345",
			Arg.Is<AssignUserRolesRequestContent>(c => c.Roles!.Single() == "rol_abc123"),
			Arg.Any<RequestOptions>(),
			Arg.Any<CancellationToken>());
		await cache.Received(1).InvalidateUsersAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_RemoveRoleCommand_Success_RemovesRoleAndInvalidatesUserCache()
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

		var cache = Substitute.For<IUserManagementCacheService>();
		cache.InvalidateUsersAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var result = await handler.Handle(new RemoveRoleCommand("auth0|12345", "rol_abc123"), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		// Deliberately not awaited: see the matching comment on the AssignAsync verification above.
		_ = userRolesClient.Received(1).DeleteAsync(
			"auth0|12345",
			Arg.Is<DeleteUserRolesRequestContent>(c => c.Roles!.Single() == "rol_abc123"),
			Arg.Any<RequestOptions>(),
			Arg.Any<CancellationToken>());
		await cache.Received(1).InvalidateUsersAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_AssignRoleCommand_WhenFactoryThrowsInvalidOperationException_ReturnsFailWithMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(
				new InvalidOperationException("Auth0:Management:Domain not configured.")));

		var handler = new UserManagementHandler(managementApiClientFactory, Substitute.For<IUserManagementCacheService>());

		// Act
		var result = await handler.Handle(new AssignRoleCommand("auth0|12345", "rol_abc123"), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Auth0:Management:Domain not configured.");
	}

	[Fact]
	public async Task Handle_AssignRoleCommand_WhenFactoryThrowsHttpRequestException_ReturnsFailWithMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new HttpRequestException("Connection refused.")));

		var handler = new UserManagementHandler(managementApiClientFactory, Substitute.For<IUserManagementCacheService>());

		// Act
		var result = await handler.Handle(new AssignRoleCommand("auth0|12345", "rol_abc123"), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Connection refused.");
	}

	[Fact]
	public async Task Handle_AssignRoleCommand_WhenFactoryThrowsUnexpectedException_ReturnsGenericFailMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new InvalidCastException("boom")));

		var handler = new UserManagementHandler(managementApiClientFactory, Substitute.For<IUserManagementCacheService>());

		// Act
		var result = await handler.Handle(new AssignRoleCommand("auth0|12345", "rol_abc123"), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("An unexpected error occurred.");
	}

	[Fact]
	public async Task Handle_AssignRoleCommand_WhenFactoryThrowsOperationCanceledException_PropagatesException()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new OperationCanceledException()));

		var handler = new UserManagementHandler(managementApiClientFactory, Substitute.For<IUserManagementCacheService>());

		// Act
		var act = () => handler.Handle(new AssignRoleCommand("auth0|12345", "rol_abc123"), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task Handle_GetUsersWithRolesQuery_WhenFactoryThrowsInvalidOperationException_ReturnsFailWithMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(
				new InvalidOperationException("Auth0:Management:Domain not configured.")));

		var cache = CreatePassThroughUsersCache();
		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var result = await handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Auth0:Management:Domain not configured.");
	}

	[Fact]
	public async Task Handle_GetUsersWithRolesQuery_WhenFactoryThrowsHttpRequestException_ReturnsFailWithMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new HttpRequestException("Connection refused.")));

		var cache = CreatePassThroughUsersCache();
		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var result = await handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Connection refused.");
	}

	[Fact]
	public async Task Handle_GetUsersWithRolesQuery_WhenFactoryThrowsUnexpectedException_ReturnsGenericFailMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new InvalidCastException("boom")));

		var cache = CreatePassThroughUsersCache();
		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var result = await handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("An unexpected error occurred.");
	}

	[Fact]
	public async Task Handle_GetUsersWithRolesQuery_WhenFactoryThrowsOperationCanceledException_PropagatesException()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new OperationCanceledException()));

		var cache = CreatePassThroughUsersCache();
		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var act = () => handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task Handle_RemoveRoleCommand_WhenFactoryThrowsInvalidOperationException_ReturnsFailWithMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(
				new InvalidOperationException("Auth0:Management:Domain not configured.")));

		var handler = new UserManagementHandler(managementApiClientFactory, Substitute.For<IUserManagementCacheService>());

		// Act
		var result = await handler.Handle(new RemoveRoleCommand("auth0|12345", "rol_abc123"), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Auth0:Management:Domain not configured.");
	}

	[Fact]
	public async Task Handle_RemoveRoleCommand_WhenFactoryThrowsHttpRequestException_ReturnsFailWithMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new HttpRequestException("Connection refused.")));

		var handler = new UserManagementHandler(managementApiClientFactory, Substitute.For<IUserManagementCacheService>());

		// Act
		var result = await handler.Handle(new RemoveRoleCommand("auth0|12345", "rol_abc123"), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Connection refused.");
	}

	[Fact]
	public async Task Handle_RemoveRoleCommand_WhenFactoryThrowsUnexpectedException_ReturnsGenericFailMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new InvalidCastException("boom")));

		var handler = new UserManagementHandler(managementApiClientFactory, Substitute.For<IUserManagementCacheService>());

		// Act
		var result = await handler.Handle(new RemoveRoleCommand("auth0|12345", "rol_abc123"), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("An unexpected error occurred.");
	}

	[Fact]
	public async Task Handle_RemoveRoleCommand_WhenFactoryThrowsOperationCanceledException_PropagatesException()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new OperationCanceledException()));

		var handler = new UserManagementHandler(managementApiClientFactory, Substitute.For<IUserManagementCacheService>());

		// Act
		var act = () => handler.Handle(new RemoveRoleCommand("auth0|12345", "rol_abc123"), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task Handle_GetAvailableRolesQuery_WhenFactoryThrowsInvalidOperationException_ReturnsFailWithMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(
				new InvalidOperationException("Auth0:Management:Domain not configured.")));

		var cache = CreatePassThroughRolesCache();
		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Auth0:Management:Domain not configured.");
	}

	[Fact]
	public async Task Handle_GetAvailableRolesQuery_WhenFactoryThrowsHttpRequestException_ReturnsFailWithMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new HttpRequestException("Connection refused.")));

		var cache = CreatePassThroughRolesCache();
		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Connection refused.");
	}

	[Fact]
	public async Task Handle_GetAvailableRolesQuery_WhenFactoryThrowsUnexpectedException_ReturnsGenericFailMessage()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new InvalidCastException("boom")));

		var cache = CreatePassThroughRolesCache();
		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("An unexpected error occurred.");
	}

	[Fact]
	public async Task Handle_GetAvailableRolesQuery_WhenFactoryThrowsOperationCanceledException_PropagatesException()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		managementApiClientFactory.CreateAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IManagementApiClient>(new OperationCanceledException()));

		var cache = CreatePassThroughRolesCache();
		var handler = new UserManagementHandler(managementApiClientFactory, cache);

		// Act
		var act = () => handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	/// <summary>
	///     Creates a <see cref="RawResponse" /> without going through its constructor, since its required
	///     <c>Headers</c> member (<c>ResponseHeaders</c>) has no accessible public constructor for tests to supply.
	///     Production code never inspects the raw response on the success path exercised here.
	/// </summary>
	private static RawResponse CreateRawResponse() =>
		(RawResponse)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(RawResponse));

	private static IUserManagementCacheService CreatePassThroughUsersCache()
	{
		var cache = Substitute.For<IUserManagementCacheService>();
		cache.GetOrFetchUsersAsync(Arg.Any<Func<Task<IReadOnlyList<UserWithRolesDto>>>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => new ValueTask<IReadOnlyList<UserWithRolesDto>>(
				callInfo.Arg<Func<Task<IReadOnlyList<UserWithRolesDto>>>>()()));
		return cache;
	}

	private static IUserManagementCacheService CreatePassThroughRolesCache()
	{
		var cache = Substitute.For<IUserManagementCacheService>();
		cache.GetOrFetchRolesAsync(Arg.Any<Func<Task<IReadOnlyList<RoleDto>>>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => new ValueTask<IReadOnlyList<RoleDto>>(
				callInfo.Arg<Func<Task<IReadOnlyList<RoleDto>>>>()()));
		return cache;
	}

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
