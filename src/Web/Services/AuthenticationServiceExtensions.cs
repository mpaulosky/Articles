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
		services.AddLogging();
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

		// AddAuth0WebAppAuthentication registers its own cookie scheme with the ASP.NET Core
		// default AccessDeniedPath ("/Account/AccessDenied", a route this app never defined), which
		// wins the redirect for a full page load of an [Authorize]-attributed component before
		// Blazor's own AuthorizeRouteView/NotAuthorized branch ever runs. Point it at the app's real
		// not-authorized page instead.
		services.Configure<CookieAuthenticationOptions>(
			CookieAuthenticationDefaults.AuthenticationScheme,
			options => options.AccessDeniedPath = "/not-authorized");

		services.AddScoped<AuthenticationStateProvider, Auth0AuthenticationStateProvider>();
		services.AddCascadingAuthenticationState();
		services.AddScoped<IClaimsTransformation, RoleClaimNormalizer>();
	}
}
