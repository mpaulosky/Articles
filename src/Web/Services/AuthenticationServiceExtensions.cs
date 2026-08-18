// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AuthenticationExtensions.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Web
// =======================================================

using Auth0.AspNetCore.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

using Web.Security;

namespace Web.Services;

/// <summary>
/// Extension methods for configuring authentication and authorization services.
/// </summary>
internal static class AuthenticationServiceExtensions
{
	/// <summary>
	/// Adds and configures authentication and authorization services using Auth0 when configured.
	/// Falls back to a local cookie scheme when the Auth0 placeholders are still in place.
	/// </summary>
	public static void AddAuthenticationAndAuthorization(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		services.AddHttpContextAccessor();
		services.AddAuthorization();

		var domain = configuration["Auth0:Domain"];
		var clientId = configuration["Auth0:ClientId"];
		var clientSecret = configuration["Auth0:ClientSecret"];
		var authEnabled = Auth0ConfigurationHelper.IsAuthenticationEnabled(domain, clientId, clientSecret);

		if (!authEnabled)
		{
			services.AddAuthentication(options =>
			{
				options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			})
				.AddCookie(options =>
				{
					options.LoginPath = "/Account/Login";
					options.AccessDeniedPath = "/Account/AccessDenied";
				});

			services.AddScoped<AuthenticationStateProvider, Auth0AuthenticationStateProvider>();
			services.AddCascadingAuthenticationState();
			services.AddScoped<IClaimsTransformation, RoleClaimNormalizer>();
			return;
		}

		services.AddAuth0WebAppAuthentication(options =>
		{
			options.Domain = domain!;
			options.ClientId = clientId!;
			options.ClientSecret = clientSecret!;
			options.Scope = "openid profile email";
		});

		services.AddScoped<AuthenticationStateProvider, Auth0AuthenticationStateProvider>();
		services.AddCascadingAuthenticationState();
		services.AddScoped<IClaimsTransformation, RoleClaimNormalizer>();
	}
}