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
	///     Exchanges the configured Auth0 Management API credentials for an access token and returns a client
	///     scoped to that token.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	///     A required setting is missing, or the token endpoint responded without a usable access token.
	/// </exception>
	/// <exception cref="HttpRequestException">The token request failed or returned a non-success status code.</exception>
	Task<IManagementApiClient> CreateAsync(CancellationToken cancellationToken);
}
