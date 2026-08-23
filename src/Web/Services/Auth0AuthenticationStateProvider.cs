// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Auth0AuthenticationStateProvider.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Web
// =======================================================

using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace Web.Services;

internal partial class Auth0AuthenticationStateProvider(
	IHttpContextAccessor httpContextAccessor,
	ILogger<Auth0AuthenticationStateProvider> logger)
	: AuthenticationStateProvider
{
	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		HttpContext? httpContext = httpContextAccessor.HttpContext;

		if (httpContext?.User.Identity?.IsAuthenticated == true)
		{
			ClaimsPrincipal user = httpContext.User;

			// Log user claims for debugging
			LogUserAuthenticatedWithClaims();

			foreach (Claim claim in user.Claims)
			{
				LogClaimTypeValue(claim.Type, claim.Value);
			}

			// Create a new ClaimsIdentity with the existing claims plus any additional processing
			ClaimsIdentity identity = new(user.Identity);

			// Add role claims if they exist
			string? rolesClaim = user.FindFirst("https://articlesite.com/roles")?.Value;

			if (!string.IsNullOrEmpty(rolesClaim))
			{
				string[] roles = rolesClaim.Split(',', StringSplitOptions.RemoveEmptyEntries);

				foreach (string role in roles)
				{
					string trimmedRole = role.Trim();

					if (!identity.HasClaim(ClaimTypes.Role, trimmedRole))
					{
						identity.AddClaim(new Claim(ClaimTypes.Role, trimmedRole));
					}
				}
			}

			// Check for Auth0 roles in the standard location
			IEnumerable<Claim> auth0Roles = user.FindAll("roles");

			foreach (Claim roleClaim in auth0Roles)
			{
				if (!identity.HasClaim(ClaimTypes.Role, roleClaim.Value))
				{
					identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
				}
			}

			ClaimsPrincipal claimsPrincipal = new(identity);

			return Task.FromResult(new AuthenticationState(claimsPrincipal));
		}

		// Return an anonymous user if not authenticated
		ClaimsPrincipal anonymous = new(new ClaimsIdentity());

		return Task.FromResult(new AuthenticationState(anonymous));
	}

	[LoggerMessage(LogLevel.Information, "User authenticated with claims:")]
	partial void LogUserAuthenticatedWithClaims();

	[LoggerMessage(LogLevel.Information, "Claim: {Type} = {Value}")]
	partial void LogClaimTypeValue(string type, string value);
}
