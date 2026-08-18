//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CachingServiceExtensions.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Web.Components.Features.UserManagement.Caching.Interfaces;
using Web.Infrastructure.Caching;

namespace Web.Components.Features.UserManagement.Caching.Extensions;

internal static class CachingServiceExtensions
{

	/// <summary>
	/// Registers the two-tier (L1 in-memory 30s + L2 Redis 2min) <see cref="IUserManagementCacheService"/>
	/// implementation. Call this after <c>AddMemoryCache()</c> and
	/// <c>AddRedisDistributedCache()</c> are already registered.
	/// </summary>
	public static IServiceCollection AddUserManagementCaching(this IServiceCollection services)
	{
		services.AddSingleton<IUserManagementCacheService, UserManagementCacheService>();
		return services;
	}
}
