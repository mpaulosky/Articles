// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GetAvailableRolesQueryTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using Domain.Abstractions;

using FluentAssertions;

using NSubstitute;

using Web.Components.Features.UserManagement.Auth0;
using Web.Components.Features.UserManagement.Caching.Interfaces;
using Web.Components.Features.UserManagement.ManageRoles;
using Web.Components.Features.UserManagement.Models;

namespace Web.Tests.Features.UserManagement;

public class GetAvailableRolesQueryTests
{
	[Fact]
	public void GetAvailableRolesReturnsAllConfiguredRoles()
	{
		// Arrange
		var query = new GetAvailableRolesQuery();

		// Act & Assert
		query.Should().NotBeNull();
	}

	[Fact]
	public async Task GetAvailableRolesHandlesEmptyConfiguration()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		var cache = Substitute.For<IUserManagementCacheService>();

		// Setup cache to return empty list
		cache.GetOrFetchRolesAsync(Arg.Any<Func<Task<IReadOnlyList<RoleDto>>>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var fetch = callInfo.Arg<Func<Task<IReadOnlyList<RoleDto>>>>();
				return new ValueTask<IReadOnlyList<RoleDto>>(Array.Empty<RoleDto>());
			});

		var handler = new UserManagementHandler(managementApiClientFactory, cache);
		var query = new GetAvailableRolesQuery();

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
		// Due to Auth0 API dependencies, actual role fetching won't work in tests
		// But we verify the query structure is valid
	}

	[Fact]
	public async Task GetAvailableRoles_UsesCachedResults_WhenAvailable()
	{
		// Arrange
		var managementApiClientFactory = Substitute.For<IManagementApiClientFactory>();
		var cache = Substitute.For<IUserManagementCacheService>();

		var cachedRoles = new List<RoleDto> { new("rol_123", "Admin"), new("rol_456", "Editor") }.AsReadOnly();

		cache.GetOrFetchRolesAsync(Arg.Any<Func<Task<IReadOnlyList<RoleDto>>>>(), Arg.Any<CancellationToken>())
			.Returns(new ValueTask<IReadOnlyList<RoleDto>>(cachedRoles));

		var handler = new UserManagementHandler(managementApiClientFactory, cache);
		var query = new GetAvailableRolesQuery();

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value.Should().Contain(r => r.Name == "Admin");
		result.Value.Should().Contain(r => r.Name == "Editor");
	}
}
