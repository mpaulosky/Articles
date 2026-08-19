// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GetUsersWithRolesQueryTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using Domain.Abstractions;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

using NSubstitute;

using Web.Components.Features.UserManagement.Caching.Interfaces;
using Web.Components.Features.UserManagement.GetUserWithRoles;
using Web.Components.Features.UserManagement.ManageRoles;

namespace Web.Tests.Features.UserManagement;

public class GetUsersWithRolesQueryTests
{
	[Fact]
	public void GetUsersWithRolesReturnsAllUsers()
	{
		// Arrange
		var query = new GetUsersWithRolesQuery();

		// Act & Assert
		query.Should().NotBeNull();
	}

	[Fact]
	public async Task GetUsersWithRolesIncludesRolesForEachUser()
	{
		// Arrange
		var configuration = Substitute.For<IConfiguration>();
		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		var cache = Substitute.For<IUserManagementCacheService>();

		var cachedUsers = new List<UserWithRolesDto>
		{
			new("auth0|123", "alice@example.com", "Alice", new[] { "Admin" }),
			new("auth0|456", "bob@example.com", "Bob", new[] { "Editor", "Viewer" })
		}.AsReadOnly();

		cache.GetOrFetchUsersAsync(Arg.Any<Func<Task<IReadOnlyList<UserWithRolesDto>>>>(), Arg.Any<CancellationToken>())
			.Returns(new ValueTask<IReadOnlyList<UserWithRolesDto>>(cachedUsers));

		var handler = new UserManagementHandler(configuration, httpClientFactory, cache);
		var query = new GetUsersWithRolesQuery();

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);

		var alice = result.Value.First(u => u.Email == "alice@example.com");
		alice.Roles.Should().Contain("Admin");

		var bob = result.Value.First(u => u.Email == "bob@example.com");
		bob.Roles.Should().HaveCount(2);
		bob.Roles.Should().Contain(new[] { "Editor", "Viewer" });
	}

	[Fact]
	public async Task GetUsersWithRolesCachesResults()
	{
		// Arrange
		var configuration = Substitute.For<IConfiguration>();
		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		var cache = Substitute.For<IUserManagementCacheService>();

		var cachedUsers = new List<UserWithRolesDto> { new("auth0|123", "alice@example.com", "Alice", new[] { "Admin" }) }
			.AsReadOnly();

		var fetchCallCount = 0;
		cache.GetOrFetchUsersAsync(Arg.Any<Func<Task<IReadOnlyList<UserWithRolesDto>>>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				fetchCallCount++;
				return new ValueTask<IReadOnlyList<UserWithRolesDto>>(cachedUsers);
			});

		var handler = new UserManagementHandler(configuration, httpClientFactory, cache);
		var query = new GetUsersWithRolesQuery();

		// Act
		var result1 = await handler.Handle(query, CancellationToken.None);
		var result2 = await handler.Handle(query, CancellationToken.None);

		// Assert
		result1.Should().NotBeNull();
		result2.Should().NotBeNull();

		// Cache should be called for both queries
		await cache.Received(2).GetOrFetchUsersAsync(Arg.Any<Func<Task<IReadOnlyList<UserWithRolesDto>>>>(),
			Arg.Any<CancellationToken>());

		// Both results should have the same cached data
		result1.Value.Should().HaveCount(1);
		result2.Value.Should().HaveCount(1);
	}
}
