// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Auth0StartupExtensions.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// ============================================

using Auth0.AspNetCore.Authentication;

using Microsoft.AspNetCore.Authentication.Cookies;

namespace Web;

public static class Auth0StartupExtensions
{
	public static void AddAuth0Authentication(this WebApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		builder.Services.AddAuthorization();

		if (builder.Environment.IsEnvironment("Testing"))
		{
			builder.Services.AddAuthentication(options =>
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

			return;
		}

		var missingSettings = new List<string>();
		if (string.IsNullOrWhiteSpace(builder.Configuration["Auth0:Domain"]))
		{
			missingSettings.Add("Auth0:Domain");
		}
		if (string.IsNullOrWhiteSpace(builder.Configuration["Auth0:ClientId"]))
		{
			missingSettings.Add("Auth0:ClientId");
		}
		if (string.IsNullOrWhiteSpace(builder.Configuration["Auth0:ClientSecret"]))
		{
			missingSettings.Add("Auth0:ClientSecret");
		}

		if (missingSettings.Count > 0)
		{
			throw new InvalidOperationException(
				$"Missing required Auth0 configuration values: {string.Join(", ", missingSettings)}");
		}

		builder.Services.AddAuth0WebAppAuthentication(options =>
		{
			options.Domain = builder.Configuration["Auth0:Domain"]!;
			options.ClientId = builder.Configuration["Auth0:ClientId"]!;
			options.ClientSecret = builder.Configuration["Auth0:ClientSecret"]!;
			options.Scope = "openid profile email";
		});
	}

	public static void UseAuth0Authentication(this WebApplication app)
	{
		ArgumentNullException.ThrowIfNull(app);

		app.UseAuthentication();
		app.UseAuthorization();
	}
}
