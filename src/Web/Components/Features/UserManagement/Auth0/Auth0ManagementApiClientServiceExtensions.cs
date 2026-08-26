//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     Auth0ManagementApiClientServiceExtensions.cs
//Company :       mpaulosky
//Author :        Teqslamer
//Solution Name : Articles
//Project Name :  Web
//=======================================================

namespace Web.Components.Features.UserManagement.Auth0;

internal static class Auth0ManagementApiClientServiceExtensions
{
	/// <summary>
	///     Registers <see cref="IManagementApiClientFactory" /> for building authenticated Auth0 Management API
	///     clients on demand.
	/// </summary>
	/// <remarks>
	///     Registered as a singleton so the factory's cached <c>ClientCredentialsTokenProvider</c> (and the
	///     Auth0 access token it holds) is reused across requests instead of being re-exchanged on every scope.
	/// </remarks>
	public static IServiceCollection AddAuth0ManagementApiClient(this IServiceCollection services)
	{
		services.AddSingleton<IManagementApiClientFactory, Auth0ManagementApiClientFactory>();
		return services;
	}
}
