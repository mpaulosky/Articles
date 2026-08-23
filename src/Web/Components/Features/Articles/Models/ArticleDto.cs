// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleDto.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Web.Components.Features.Articles.Entities;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Models;

namespace Web.Components.Features.Articles.Models;

/// <summary>
///     Represents an article in its serialized DTO form.
/// </summary>
public sealed record ArticleDto(
	string Id,
	string Title,
	string Slug,
	string Content,
	AuthorDto Author,
	CategoryDto Category,
	DateTime CreatedAt,
	DateTime? UpdatedAt,
	bool IsPublished,
	DateTime? PublishedOn,
	bool IsArchived = false)
{
	/// <summary>
	///     Maps an entity to a DTO using explicit application logic instead of a mapper library.
	/// </summary>
	/// <param name="article">The entity to map.</param>
	/// <returns>The article DTO.</returns>
	public static ArticleDto FromEntity(Article article)
	{
		ArgumentNullException.ThrowIfNull(article);

		return new ArticleDto(
			article.Id.ToString(),
			article.Title,
			article.Slug,
			article.Content,
			article.Author,
			article.Category,
			article.CreatedAt,
			article.UpdatedAt,
			article.IsPublished,
			article.PublishedOn,
			article.IsArchived);
	}
}
