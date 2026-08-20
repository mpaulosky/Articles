//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RoleClaimsHelper.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using System.Security.Claims;
using System.Text.Json;

namespace Web.Security;

internal static class RoleClaimsHelper
{
	private static readonly string[] DefaultRoleClaimTypes =
	[
			"https://myblog/roles",
				"roles",
				"role"
	];

	public static IReadOnlyList<string> GetRoleClaimTypes(IConfiguration configuration)
	{
		var configured = configuration.GetSection("Auth0:RoleClaimTypes").Get<string[]>();

		return configured is { Length: > 0 }
				? configured
						.Where(value => !string.IsNullOrWhiteSpace(value))
						.Select(value => value.Trim())
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.ToArray()
				: DefaultRoleClaimTypes;
	}


	private static bool IsRoleClaimType(string? claimType)
	{
		if (string.IsNullOrWhiteSpace(claimType))
		{
			return false;
		}

		if (claimType.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)
				|| claimType.Equals("roles", StringComparison.OrdinalIgnoreCase)
				|| claimType.Equals("role", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		var lastSlash = claimType.LastIndexOf('/');
		var lastColon = claimType.LastIndexOf(':');
		var separatorIndex = Math.Max(lastSlash, lastColon);
		var tail = separatorIndex >= 0 ? claimType[(separatorIndex + 1)..] : claimType;

		return tail.Equals("roles", StringComparison.OrdinalIgnoreCase)
				|| tail.Equals("role", StringComparison.OrdinalIgnoreCase);
	}

	private static string[] GetEffectiveRoleClaimTypes(IEnumerable<Claim> claims, IEnumerable<string>? roleClaimTypes)
	{
		return (roleClaimTypes ?? DefaultRoleClaimTypes)
				.Append(ClaimTypes.Role)
				.Concat(claims.Select(claim => claim.Type).Where(IsRoleClaimType))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
	}

	private static string[] ExpandRoleValues(string? claimValue)
	{
		if (string.IsNullOrWhiteSpace(claimValue))
		{
			return Array.Empty<string>();
		}

		var trimmed = claimValue.Trim();

		if (trimmed.StartsWith('['))
		{
			try
			{
				using var document = JsonDocument.Parse(trimmed);

				if (document.RootElement.ValueKind == JsonValueKind.Array)
				{
					return document.RootElement
							.EnumerateArray()
							.Select(element => element.ValueKind == JsonValueKind.String ? element.GetString() : null)
							.Where(role => !string.IsNullOrWhiteSpace(role))
							.Select(role => role!.Trim())
							.Distinct(StringComparer.OrdinalIgnoreCase)
							.ToArray();
				}
			}
			catch (JsonException)
			{
				return Array.Empty<string>();
			}
		}

		if (trimmed.Contains(',', StringComparison.Ordinal))
		{
			return trimmed
					.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToArray();
		}

		return [trimmed];
	}

	public static void AddRoleClaims(ClaimsIdentity identity, IEnumerable<string> roleClaimTypes)
	{
		ArgumentNullException.ThrowIfNull(identity);

		foreach (var roleClaimType in GetEffectiveRoleClaimTypes(identity.Claims, roleClaimTypes))
		{
			foreach (var claim in identity.FindAll(roleClaimType).ToList())
			{
				foreach (var role in ExpandRoleValues(claim.Value))
				{
					if (!identity.HasClaim(ClaimTypes.Role, role))
					{
						identity.AddClaim(new Claim(ClaimTypes.Role, role));
					}
				}
			}
		}
	}

	public static IReadOnlyList<string> GetRoles(ClaimsPrincipal user, IEnumerable<string>? roleClaimTypes = null)
	{
		var types = GetEffectiveRoleClaimTypes(user.Claims, roleClaimTypes);

		return user.Claims
				.Where(claim => types.Contains(claim.Type, StringComparer.OrdinalIgnoreCase))
				.SelectMany(claim => ExpandRoleValues(claim.Value))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(role => role)
				.ToList();
	}
}
