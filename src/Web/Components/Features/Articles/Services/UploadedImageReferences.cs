// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UploadedImageReferences.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using System.Text.RegularExpressions;

namespace Web.Components.Features.Articles.Services;

/// <summary>
///     Finds which uploaded files (saved under wwwroot/uploads by <see cref="IFileStorage" />) are
///     referenced by a block of article content, so callers can tell which uploads a content edit
///     stopped using.
/// </summary>
public static partial class UploadedImageReferences
{
	public static IReadOnlySet<string> ExtractFileNames(string? content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		return UploadPathPattern()
			.Matches(content)
			.Select(match => match.Groups["fileName"].Value)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	///     File names that <paramref name="oldContent" /> referenced but <paramref name="newContent" />
	///     no longer does.
	/// </summary>
	public static IReadOnlySet<string> FindRemoved(string? oldContent, string? newContent)
	{
		var removed = new HashSet<string>(ExtractFileNames(oldContent), StringComparer.OrdinalIgnoreCase);
		removed.ExceptWith(ExtractFileNames(newContent));
		return removed;
	}

	[GeneratedRegex(@"/uploads/(?<fileName>[A-Za-z0-9\-]+\.[A-Za-z0-9]+)")]
	private static partial Regex UploadPathPattern();
}
