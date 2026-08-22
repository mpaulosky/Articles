//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UserManagementCacheService.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

using Web.Components.Features.UserManagement.Caching.Interfaces;
using Web.Components.Features.UserManagement.GetUserRoles;
using Web.Components.Features.UserManagement.GetUserWithRoles;

namespace Web.Infrastructure.Caching;

internal sealed class UserManagementCacheService(
	IMemoryCache localCache,
	IDistributedCache distributedCache) : IUserManagementCacheService
{
	private static readonly MemoryCacheEntryOptions LocalOpts =
		new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30));

	private static readonly DistributedCacheEntryOptions RedisOpts =
		new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(2));

	private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

	public async ValueTask<IReadOnlyList<UserWithRolesDto>> GetOrFetchUsersAsync(
		Func<Task<IReadOnlyList<UserWithRolesDto>>> fetch,
		CancellationToken ct = default)
	{
		// L1 hit (synchronous — no heap allocation)
		if (localCache.TryGetValue(UserManagementCacheKeys.AllUsers, out List<UserWithRolesDto>? cached) &&
		    cached is not null)
			return cached;

		// L2 hit
		var bytes = await distributedCache.GetAsync(UserManagementCacheKeys.AllUsers, ct).ConfigureAwait(false);
		if (bytes is not null)
		{
			try
			{
				var fromRedis = JsonSerializer.Deserialize<List<UserWithRolesDto>>(bytes, JsonOpts);
				if (fromRedis is not null)
				{
					localCache.Set(UserManagementCacheKeys.AllUsers, fromRedis, LocalOpts);
					return fromRedis;
				}
			}
			catch (JsonException)
			{
				// Stale or corrupt bytes — remove and fall through to the source
				await distributedCache.RemoveAsync(UserManagementCacheKeys.AllUsers, ct).ConfigureAwait(false);
			}
		}

		// Auth0 via caller-supplied fetch
		var result = await fetch().ConfigureAwait(false);
		var list = result as List<UserWithRolesDto> ?? result.ToList();
		localCache.Set(UserManagementCacheKeys.AllUsers, list, LocalOpts);
		await distributedCache.SetAsync(
			UserManagementCacheKeys.AllUsers,
			JsonSerializer.SerializeToUtf8Bytes(list, JsonOpts),
			RedisOpts,
			ct).ConfigureAwait(false);
		return result;
	}

	public async ValueTask<IReadOnlyList<RoleDto>> GetOrFetchRolesAsync(
		Func<Task<IReadOnlyList<RoleDto>>> fetch,
		CancellationToken ct = default)
	{
		// L1 hit (synchronous — no heap allocation)
		if (localCache.TryGetValue(UserManagementCacheKeys.AllRoles, out List<RoleDto>? cached) && cached is not null)
			return cached;

		// L2 hit
		var bytes = await distributedCache.GetAsync(UserManagementCacheKeys.AllRoles, ct).ConfigureAwait(false);
		if (bytes is not null)
		{
			try
			{
				var fromRedis = JsonSerializer.Deserialize<List<RoleDto>>(bytes, JsonOpts);
				if (fromRedis is not null)
				{
					localCache.Set(UserManagementCacheKeys.AllRoles, fromRedis, LocalOpts);
					return fromRedis;
				}
			}
			catch (JsonException)
			{
				// Stale or corrupt bytes — remove and fall through to the source
				await distributedCache.RemoveAsync(UserManagementCacheKeys.AllRoles, ct).ConfigureAwait(false);
			}
		}

		// Auth0 via caller-supplied fetch
		var result = await fetch().ConfigureAwait(false);
		var list = result as List<RoleDto> ?? result.ToList();
		localCache.Set(UserManagementCacheKeys.AllRoles, list, LocalOpts);
		await distributedCache.SetAsync(
			UserManagementCacheKeys.AllRoles,
			JsonSerializer.SerializeToUtf8Bytes(list, JsonOpts),
			RedisOpts,
			ct).ConfigureAwait(false);
		return result;
	}

	public async Task InvalidateUsersAsync(CancellationToken ct = default)
	{
		localCache.Remove(UserManagementCacheKeys.AllUsers);
		await distributedCache.RemoveAsync(UserManagementCacheKeys.AllUsers, ct).ConfigureAwait(false);
	}

	public async Task InvalidateRolesAsync(CancellationToken ct = default)
	{
		localCache.Remove(UserManagementCacheKeys.AllRoles);
		await distributedCache.RemoveAsync(UserManagementCacheKeys.AllRoles, ct).ConfigureAwait(false);
	}
}
