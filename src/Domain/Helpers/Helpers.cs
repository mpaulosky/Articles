// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Helpers.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain
// =============================================

using System.Text.RegularExpressions;

namespace Domain.Helpers;

/// <summary>
///     Provides shared domain helper methods for deterministic values, slugs, and sample categories.
/// </summary>
public static partial class DomainHelpers
{
	private static readonly DateTimeOffset StaticDateValue = new(2025, 1, 1, 8, 0, 0, TimeSpan.Zero);

	/// <summary>
	///     Gets a static date for testing purposes.
	/// </summary>
	/// <returns>A static date of January 1, 2025, at 08:00 AM.</returns>
	public static DateTimeOffset StaticDate => StaticDateValue;

	/// <summary>
	///     Converts a string to a URL-friendly slug.
	/// </summary>
	/// <param name="item">The string to convert to a slug.</param>
	/// <returns>A URL-friendly slug using hyphen separators.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
		Justification = "URL slugs are conventionally lowercase for SEO and readability")]
	public static string GenerateSlug(this string item)
	{
		if (string.IsNullOrWhiteSpace(item))
		{
			return string.Empty;
		}

		string slug = item.Trim();
		slug = slug.ToLowerInvariant();
		slug = Regex.Replace(slug, "[^a-z0-9]+", "-");
		slug = Regex.Replace(slug, "-+", "-");
		slug = slug.Trim('-');

		return slug;
	}

	/// <summary>
	///     Gets a random category name from predefined categories.
	/// </summary>
	/// <returns>A random category name.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness",
		Justification = "Used only for test data generation, not security-sensitive operations")]
	public static string GetRandomCategoryName()
	{
		List<string> categories =
		[
			MyCategories.First,
			MyCategories.Second,
			MyCategories.Third,
			MyCategories.Fourth,
			MyCategories.Fifth,
			MyCategories.Sixth,
			MyCategories.Seventh,
			MyCategories.Eighth,
			MyCategories.Ninth
		];

		return categories[Random.Shared.Next(categories.Count)];
	}
}