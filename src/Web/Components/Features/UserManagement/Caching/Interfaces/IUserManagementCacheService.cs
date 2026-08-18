//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     IUserManagementCacheService.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

using Web.Components.Features.UserManagement.GetUserRoles;
using Web.Components.Features.UserManagement.GetUserWithRoles;

namespace Web.Components.Features.UserManagement.Caching.Interfaces;

/// <summary>
/// Two-tier (L1 in-memory + L2 Redis) cache abstraction for <see cref="UserWithRolesDto"/>
/// and <see cref="RoleDto"/> values.
/// </summary>
/// <remarks>
/// Registered as a singleton. Both <see cref="IMemoryCache"/> and
/// <see cref="IDistributedCache"/> (StackExchange Redis) are also singletons,
/// so captive-dependency rules are satisfied.
/// </remarks>
internal interface IUserManagementCacheService
{
	/// <summary>
	/// Returns all users with their roles from the nearest cache tier, or invokes
	/// <paramref name="fetch"/> on a complete miss, populates both tiers, and returns the result.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="ValueTask{T}"/> because L1 hits complete synchronously.
	/// Do not await the same <see cref="ValueTask{T}"/> instance more than once.
	/// </remarks>
	ValueTask<IReadOnlyList<UserWithRolesDto>> GetOrFetchUsersAsync(
		Func<Task<IReadOnlyList<UserWithRolesDto>>> fetch,
		CancellationToken ct = default);

	/// <summary>
	/// Returns all available roles from the nearest cache tier, or invokes
	/// <paramref name="fetch"/> on a complete miss, populates both tiers, and returns the result.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="ValueTask{T}"/> because L1 hits complete synchronously.
	/// Do not await the same <see cref="ValueTask{T}"/> instance more than once.
	/// </remarks>
	ValueTask<IReadOnlyList<RoleDto>> GetOrFetchRolesAsync(
		Func<Task<IReadOnlyList<RoleDto>>> fetch,
		CancellationToken ct = default);

	/// <summary>
	/// Removes the users-with-roles entry from both cache tiers.
	/// </summary>
	/// <remarks>
	/// Redis removal uses <see cref="CancellationToken.None"/>: the Auth0 mutation has
	/// already been applied and invalidation must complete regardless of caller cancellation.
	/// </remarks>
	Task InvalidateUsersAsync(CancellationToken ct = default);

	/// <summary>
	/// Removes the available-roles entry from both cache tiers.
	/// </summary>
	/// <remarks>
	/// Redis removal uses <see cref="CancellationToken.None"/>: the Auth0 mutation has
	/// already been applied and invalidation must complete regardless of caller cancellation.
	/// </remarks>
	Task InvalidateRolesAsync(CancellationToken ct = default);
}
