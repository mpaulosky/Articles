// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Article.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using static Domain.Helpers.DomainHelpers;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Articles.Models;
using Web.Components.Features.Articles.Services;
using Web.Components.Features.Categories.Models;

namespace Web.Components.Features.Articles.Entities;

/// <summary>
///     Represents an article authored for publication in the blog.
/// </summary>
[Serializable]
public sealed class Article
{
	/// <summary>
	///     Gets the unique MongoDB identifier for the article.
	/// </summary>
	[BsonId]
	[BsonElement("_id")]
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; private set; }

	/// <summary>
	///     Gets the article title.
	/// </summary>
	[BsonElement("title")]
	[BsonRepresentation(BsonType.String)]
	public string Title { get; private set; } = string.Empty;

	/// <summary>
	///   Gets or sets the slug for the article, used in the article's URL.
	/// </summary>
	[BsonElement("Slug")]
	[BsonRepresentation(BsonType.String)]
	public string Slug { get; private set; } = string.Empty;

	/// <summary>
	///     Gets the article body content.
	/// </summary>
	[BsonElement("content")]
	[BsonRepresentation(BsonType.String)]
	public string Content { get; private set; } = string.Empty;

	/// <summary>
	///     Gets the author snapshot captured when the article was created.
	/// </summary>
	[BsonElement("author")]
	public AuthorDto Author { get; private set; } = AuthorDto.Empty;

	/// <summary>
	///     Gets the UTC date and time when the article was created.
	/// </summary>
	[BsonElement("createdAt")]
	[BsonRepresentation(BsonType.DateTime)]
	public DateTime CreatedAt { get; private set; }

	/// <summary>
	///     Gets the UTC date and time when the article was last updated.
	/// </summary>
	[BsonElement("updatedAt")]
	[BsonRepresentation(BsonType.DateTime)]
	public DateTime? UpdatedAt { get; private set; }

	/// <summary>
	///     Gets a value indicating whether the article is published.
	/// </summary>
	[BsonElement("isPublished")]
	[BsonRepresentation(BsonType.Boolean)]
	public bool IsPublished { get; private set; }

	/// <summary>
	///     Gets the UTC date and time when the article was published, when <see cref="IsPublished" /> is true.
	/// </summary>
	[BsonElement("publishedOn")]
	[BsonRepresentation(BsonType.DateTime)]
	public DateTime? PublishedOn { get; private set; }

	/// <summary>
	///     Gets the assigned category identifier, when the article has a category.
	/// </summary>
	[BsonElement("category")]
	public CategoryDto Category { get; private set; } = CategoryDto.Empty;

	/// <summary>
	///     Gets a value indicating whether the article is archived. Archiving is independent of
	///     <see cref="IsPublished" /> and hides the article from the default article list until unarchived.
	/// </summary>
	[BsonElement("isArchived")]
	[BsonRepresentation(BsonType.Boolean)]
	public bool IsArchived { get; private set; }

	/// <summary>
	///     Gets the structured record of images referenced inside <see cref="Content" />. See ADR-0003.
	/// </summary>
	[BsonElement("articleImages")]
	public List<ArticleImage> ArticleImages { get; private set; } = [];

	private Article()
	{
	}

	/// <summary>
	///     Creates a new article with the supplied title, content, and author.
	/// </summary>
	/// <param name="title">The article title.</param>
	/// <param name="content">The article body content.</param>
	/// <param name="author">The author snapshot for the article.</param>
	/// <param name="slug">An explicit slug to use; when omitted, one is generated from <paramref name="title" />.</param>
	/// <returns>A new <see cref="Article" /> instance.</returns>
	public static Article Create(string title, string content, AuthorDto author, string? slug = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(content);
		ArgumentNullException.ThrowIfNull(author);
		ArgumentException.ThrowIfNullOrWhiteSpace(author.Name);

		var trimmedContent = content.Trim();

		return new Article
		{
			Id = ObjectId.GenerateNewId(),
			Title = title.Trim(),
			Slug = string.IsNullOrWhiteSpace(slug) ? title.GenerateSlug() : slug.Trim(),
			Content = trimmedContent,
			Author = author,
			CreatedAt = DateTime.UtcNow,
			ArticleImages = ArticleImageParser.Parse(trimmedContent, [])
		};
	}

	/// <summary>
	///     Publishes the article, making it visible to readers.
	/// </summary>
	public void Publish()
	{
		if (IsPublished)
		{
			return;
		}

		IsPublished = true;
		PublishedOn = DateTime.UtcNow;
		Touch();
	}

	/// <summary>
	///     Unpublishes the article, hiding it from readers.
	/// </summary>
	public void Unpublish()
	{
		if (!IsPublished)
		{
			return;
		}

		IsPublished = false;
		PublishedOn = null;
		Touch();
	}

	/// <summary>
	///     Archives the article, hiding it from the default article list. Independent of <see cref="IsPublished" />.
	/// </summary>
	public void Archive()
	{
		if (IsArchived)
		{
			return;
		}

		IsArchived = true;
		Touch();
	}

	/// <summary>
	///     Unarchives the article, restoring it to the default article list. Independent of <see cref="IsPublished" />.
	/// </summary>
	public void Unarchive()
	{
		if (!IsArchived)
		{
			return;
		}

		IsArchived = false;
		Touch();
	}

	/// <summary>
	///     Updates the article title, body, and category metadata.
	/// </summary>
	/// <param name="title">The updated article title.</param>
	/// <param name="content">The updated article body content.</param>
	/// <param name="category">The category metadata to assign.</param>
	/// <param name="clearCategory">When true, clears any assigned category.</param>
	/// <param name="slug">An explicit slug to use; when omitted, one is generated from <paramref name="title" />.</param>
	public void Update(string title, string content, CategoryDto? category = null, bool clearCategory = false,
		string? slug = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(content);
		if (category is not null)
		{
			ArgumentNullException.ThrowIfNull(category);
		}

		Title = title.Trim();
		Slug = string.IsNullOrWhiteSpace(slug) ? title.GenerateSlug() : slug.Trim();
		Content = content.Trim();
		ArticleImages = ArticleImageParser.Parse(Content, ArticleImages);

		if (clearCategory)
		{
			Category = CategoryDto.Empty;
		}
		else if (category != null)
		{
			Category = category;
		}

		Touch();
	}

	/// <summary>
	///     Assigns the article to the specified category.
	/// </summary>
	/// <param name="category">The category metadata to assign.</param>
	public void AssignCategory(CategoryDto category)
	{
		ArgumentNullException.ThrowIfNull(category);
		Category = category;
		Touch();
	}

	/// <summary>
	///     Removes the article from its current category.
	/// </summary>
	public void RemoveCategory()
	{
		Category = CategoryDto.Empty;
		Touch();
	}

	private void Touch()
	{
		UpdatedAt = DateTime.UtcNow;
	}
}
