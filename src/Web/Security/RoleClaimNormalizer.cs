// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RoleClaimNormalizer.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : Articles
// Project Name :  Web
// =======================================================

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Web.Security;

public sealed class RoleClaimNormalizer : IClaimsTransformation
{
	private const string Auth0RoleClaimType = "https://articlesite.com/roles";

	private static readonly string[] SupportedRoleClaimTypes =
	[
		Auth0RoleClaimType,
		"roles",
		"role"
	];

	public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
	{
		ArgumentNullException.ThrowIfNull(principal);

		var identity = principal.Identities.FirstOrDefault();
		if (identity is null)
		{
			return Task.FromResult(principal);
		}

		var existingRoles = identity.FindAll(ClaimTypes.Role)
			.Select(claim => claim.Value)
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		var transformed = false;

		foreach (var claimType in SupportedRoleClaimTypes)
		{
			foreach (var claim in identity.FindAll(claimType).ToList())
			{
				foreach (var role in ExpandRoleValues(claim.Value))
				{
					if (existingRoles.Add(role))
					{
						identity.AddClaim(new Claim(ClaimTypes.Role, role));
						transformed = true;
					}
				}
			}
		}

		return transformed ? Task.FromResult(principal) : Task.FromResult(principal);
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
}
