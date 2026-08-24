// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleImageParser.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using System.Text.RegularExpressions;

using Web.Components.Features.Articles.Models;

namespace Web.Components.Features.Articles.Services;

/// <summary>
///     Parses an article's <c>Content</c> markdown for uploaded image references (see
///     <see cref="IFileStorage" />) and produces the <see cref="ArticleImage" /> array that should
///     replace <c>Article.ArticleImages</c> on that save. See ADR-0003.
/// </summary>
public static partial class ArticleImageParser
{
	/// <summary>
	///     Parses <paramref name="content" /> for uploaded image references, reusing the upload
	///     metadata (size, mime type, uploaded timestamp) of any <paramref name="previousImages" />
	///     entry still referenced by the same URL, so unchanged images keep their recorded metadata.
	/// </summary>
	public static List<ArticleImage> Parse(string? content, IReadOnlyList<ArticleImage> previousImages)
	{
		if (string.IsNullOrEmpty(content))
		{
			return [];
		}

		var previousByUrl = previousImages.ToDictionary(image => image.Url, StringComparer.OrdinalIgnoreCase);
		var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var images = new List<ArticleImage>();

		foreach (Match match in UploadedImagePattern().Matches(content))
		{
			var url = match.Groups["url"].Value;
			if (!seenUrls.Add(url))
			{
				continue;
			}

			var fileName = match.Groups["fileName"].Value;
			var altText = match.Groups["alt"].Value;

			images.Add(previousByUrl.TryGetValue(url, out var previous)
				? ArticleImage.Create(fileName, url, previous.SizeInBytes, previous.MimeType, altText, previous.UploadedAt)
				: ArticleImage.Create(fileName, url, sizeInBytes: 0, mimeType: string.Empty, altText, DateTime.UtcNow));
		}

		return images;
	}

	/// <summary>
	///     File names of every uploaded image referenced in <paramref name="content" />, without
	///     resolving their metadata. Used where only the set of currently-referenced files is
	///     needed, such as tracking uploads that haven't been persisted to an article yet.
	/// </summary>
	public static IReadOnlySet<string> ExtractFileNames(string? content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		return UploadedImagePattern()
			.Matches(content)
			.Select(match => match.Groups["fileName"].Value)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	///     File names that <paramref name="previousImages" /> referenced but
	///     <paramref name="updatedImages" /> no longer does.
	/// </summary>
	public static IReadOnlySet<string> FindRemoved(
		IReadOnlyList<ArticleImage> previousImages,
		IReadOnlyList<ArticleImage> updatedImages)
	{
		var removed = new HashSet<string>(
			previousImages.Select(image => image.FileName),
			StringComparer.OrdinalIgnoreCase);
		removed.ExceptWith(updatedImages.Select(image => image.FileName));
		return removed;
	}

	[GeneratedRegex(
		"""!\[(?<alt>[^\]]*)\]\((?<url>[^)\s]*/uploads/(?<fileName>[A-Za-z0-9\-]+\.[A-Za-z0-9]+))(?:\s+"[^"]*")?\)""")]
	private static partial Regex UploadedImagePattern();
}
