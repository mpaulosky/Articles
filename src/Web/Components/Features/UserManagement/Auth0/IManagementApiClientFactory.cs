//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     IManagementApiClientFactory.cs
//Company :       mpaulosky
//Author :        Teqslamer
//Solution Name : Articles
//Project Name :  Web
//=======================================================

using Auth0.ManagementApi;

namespace Web.Components.Features.UserManagement.Auth0;

/// <summary>
///     Builds an authenticated <see cref="IManagementApiClient" /> for the Auth0 Management API.
/// </summary>
/// <remarks>
///     Isolated behind an interface so <see cref="Handlers.UserManagementHandler" /> can be tested against a
///     substituted Auth0 client instead of exercising the real token exchange and HTTP pipeline.
/// </remarks>
internal interface IManagementApiClientFactory
{
	/// <summary>
	///     Returns a client authenticated against the configured Auth0 Management API credentials.
	/// </summary>
	/// <remarks>
	///     The access token is exchanged lazily, on the client's first actual API call, and then cached and
	///     auto-refreshed by the underlying token provider — not eagerly during this call. Only the presence
	///     of the required configuration settings is validated eagerly here.
	/// </remarks>
	/// <exception cref="InvalidOperationException">A required setting is missing.</exception>
	Task<IManagementApiClient> CreateAsync(CancellationToken cancellationToken);
}
