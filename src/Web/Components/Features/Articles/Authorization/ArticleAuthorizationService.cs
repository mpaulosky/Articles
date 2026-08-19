using System.Security.Claims;

using Web.Components.Features.Articles.Models;

namespace Web.Components.Features.Articles.Authorization;

/// <summary>
///     Enforces the article access rules for the application.
/// </summary>
public static class ArticleAuthorizationService
{
	private static readonly string[] SupportedRoleClaimTypes =
	[
		ClaimTypes.Role,
		"roles",
		"role",
		"https://articles/roles",
		"https://myblog/roles"
	];

	/// <summary>
	///     Gets a value indicating whether the supplied user can view the article.
	/// </summary>
	/// <param name="user">The current principal.</param>
	/// <param name="article">The article being evaluated.</param>
	/// <returns><c>true</c> when the user can view the article; otherwise <c>false</c>.</returns>
	public static bool CanViewArticle(ClaimsPrincipal? user, ArticleDto article)
	{
		ArgumentNullException.ThrowIfNull(article);

		if (user is null || !user.Identity?.IsAuthenticated == true)
		{
			return false;
		}

		if (IsInRole(user, "Admin"))
		{
			return true;
		}

		if (IsInRole(user, "Author"))
		{
			return article.Author.UserId == GetCurrentUserId(user);
		}

		return article.IsPublished;
	}

	/// <summary>
	///     Gets a value indicating whether the supplied user can edit the article.
	/// </summary>
	/// <param name="user">The current principal.</param>
	/// <param name="article">The article being evaluated.</param>
	/// <returns><c>true</c> when the user can edit the article; otherwise <c>false</c>.</returns>
	public static bool CanEditArticle(ClaimsPrincipal? user, ArticleDto article)
	{
		ArgumentNullException.ThrowIfNull(article);

		if (user is null || !user.Identity?.IsAuthenticated == true)
		{
			return false;
		}

		if (IsInRole(user, "Admin"))
		{
			return true;
		}

		if (IsInRole(user, "Author"))
		{
			return article.Author.UserId == GetCurrentUserId(user);
		}

		return false;
	}

	/// <summary>
	///     Gets the current authenticated user identifier from the principal.
	/// </summary>
	/// <param name="user">The current principal.</param>
	/// <returns>The user identifier or <c>null</c> when the principal does not identify a user.</returns>
	public static string? GetCurrentUserId(ClaimsPrincipal? user)
	{
		if (user is null)
		{
			return null;
		}

		return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
		       ?? user.FindFirst("sub")?.Value
		       ?? user.FindFirst("user_id")?.Value
		       ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
	}

	/// <summary>
	///     Gets a value indicating whether the principal includes the supplied role.
	/// </summary>
	/// <param name="user">The principal to inspect.</param>
	/// <param name="role">The role to check.</param>
	/// <returns><c>true</c> when the role is present; otherwise <c>false</c>.</returns>
	public static bool IsInRole(ClaimsPrincipal? user, string role)
	{
		if (user is null || string.IsNullOrWhiteSpace(role))
		{
			return false;
		}

		var normalized = role.Trim();
		foreach (var claim in user.Claims)
		{
			if (!SupportedRoleClaimTypes.Contains(claim.Type, StringComparer.OrdinalIgnoreCase)
			    && !claim.Type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)
			    && !claim.Type.Equals("roles", StringComparison.OrdinalIgnoreCase)
			    && !claim.Type.Equals("role", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			foreach (var value in ExpandClaimValues(claim.Value))
			{
				if (string.Equals(value, normalized, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static IEnumerable<string> ExpandClaimValues(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return Array.Empty<string>();
		}

		var trimmed = raw.Trim();
		if (trimmed.StartsWith('['))
		{
			try
			{
				using var document = System.Text.Json.JsonDocument.Parse(trimmed);
				if (document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
				{
					return document.RootElement
						.EnumerateArray()
						.Select(element => element.ValueKind == System.Text.Json.JsonValueKind.String ? element.GetString() : null)
						.Where(value => !string.IsNullOrWhiteSpace(value))
						.Select(value => value!.Trim())
						.ToArray();
				}
			}
			catch (System.Text.Json.JsonException)
			{
				return Array.Empty<string>();
			}
		}

		if (trimmed.Contains(',', StringComparison.Ordinal))
		{
			return trimmed
				.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
				.ToArray();
		}

		return [trimmed];
	}
}
