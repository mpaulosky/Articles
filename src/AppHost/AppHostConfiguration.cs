// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AppHostConfiguration.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost
// =============================================

using Microsoft.Extensions.Hosting;

namespace AppHost;

/// <summary>
/// Provides common AppHost configuration helpers for environment and feature toggles.
/// </summary>
public static class AppHostConfiguration
{
	/// <summary>
	/// Resolves the ASP.NET Core environment value from configuration or the process environment.
	/// </summary>
	public static string GetAspNetCoreEnvironment(IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		var configuredValue = configuration["ASPNETCORE_ENVIRONMENT"];
		if (!string.IsNullOrWhiteSpace(configuredValue))
		{
			return configuredValue;
		}

		var environmentValue = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
		return string.IsNullOrWhiteSpace(environmentValue) ? Environments.Development : environmentValue;
	}

	/// <summary>
	/// Determines whether a feature switch is disabled by configuration or environment variable.
	/// </summary>
	public static bool IsDisabled(IConfiguration configuration, string settingName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(settingName);
		ArgumentNullException.ThrowIfNull(configuration);

		var configuredValue = configuration[settingName];
		if (!string.IsNullOrWhiteSpace(configuredValue))
		{
			return IsTruthy(configuredValue);
		}

		var environmentValue = Environment.GetEnvironmentVariable(settingName);
		if (!string.IsNullOrWhiteSpace(environmentValue))
		{
			return IsTruthy(environmentValue);
		}

		return false;
	}

	private static bool IsTruthy(string value)
	{
		var normalized = value.Trim();
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return false;
		}

		return normalized.Equals("true", StringComparison.OrdinalIgnoreCase)
			|| normalized.Equals("1", StringComparison.OrdinalIgnoreCase)
			|| normalized.Equals("yes", StringComparison.OrdinalIgnoreCase)
			|| normalized.Equals("on", StringComparison.OrdinalIgnoreCase);
	}
}
