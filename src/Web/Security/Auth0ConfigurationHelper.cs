//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     Auth0ConfigurationHelper.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : Articles
//Project Name :  Web
//=======================================================

namespace Web.Security;

internal static class Auth0ConfigurationHelper
{
	private const string PlaceholderDomain = "test.auth0.com";
	private const string PlaceholderClientId = "test-client-id";
	private const string PlaceholderClientSecret = "test-client-secret";

	private static bool IsPlaceholderValue(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return true;
		}

		var trimmed = value.Trim();
		return trimmed.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(trimmed, PlaceholderDomain, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(trimmed, PlaceholderClientId, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(trimmed, PlaceholderClientSecret, StringComparison.Ordinal);
	}

	public static bool UsesPlaceholderWebAppLogin(string? domain, string? clientId)
	{
		return IsPlaceholderValue(domain) || IsPlaceholderValue(clientId);
	}

	public static bool UsesPlaceholderWebAppLogin(string? domain, string? clientId, string? clientSecret)
	{
		return IsPlaceholderValue(domain)
			|| IsPlaceholderValue(clientId)
			|| IsPlaceholderValue(clientSecret);
	}

	public static bool IsAuthenticationEnabled(string? domain, string? clientId)
	{
		return IsAuthenticationEnabled(domain, clientId, null);
	}

	public static bool IsAuthenticationEnabled(string? domain, string? clientId, string? clientSecret)
	{
		return !UsesPlaceholderWebAppLogin(domain, clientId, clientSecret)
			&& !string.IsNullOrWhiteSpace(domain)
			&& !string.IsNullOrWhiteSpace(clientId)
			&& !string.IsNullOrWhiteSpace(clientSecret);
	}

	public static bool ShouldUseLocalTestLogin(bool isTestingEnvironment, string? domain, string? clientId)
	{
		return isTestingEnvironment && UsesPlaceholderWebAppLogin(domain, clientId);
	}

	public static bool ShouldUseLocalTestLogin(bool isTestingEnvironment, string? domain, string? clientId, string? clientSecret)
	{
		return isTestingEnvironment && UsesPlaceholderWebAppLogin(domain, clientId, clientSecret);
	}
}
