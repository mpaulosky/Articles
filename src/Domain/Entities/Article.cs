// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Article.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain
// =============================================

using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
///     Represents an article authored for publication in the blog.
/// </summary>
public sealed class Article
{
	/// <summary>
	///     Gets the unique MongoDB identifier for the article.
	/// </summary>
	public ObjectId Id { get; private set; }

	/// <summary>
	///     Gets the article title.
	/// </summary>
	public string Title { get; private set; } = string.Empty;

	/// <summary>
	///     Gets the article body content.
	/// </summary>
	public string Content { get; private set; } = string.Empty;

	/// <summary>
	///     Gets the author snapshot captured when the article was created.
	/// </summary>
	public PostAuthor Author { get; private set; } = PostAuthor.Empty;

	/// <summary>
	///     Gets the UTC date and time when the article was created.
	/// </summary>
	public DateTime CreatedAt { get; private set; }

	/// <summary>
	///     Gets the UTC date and time when the article was last updated.
	/// </summary>
	public DateTime? UpdatedAt { get; private set; }

	/// <summary>
	///     Gets a value indicating whether the article is published.
	/// </summary>
	public bool IsPublished { get; private set; }

	/// <summary>
	///     Gets the assigned category identifier, when the article has a category.
	/// </summary>
	public ObjectId? CategoryId { get; private set; }

	private Article()
	{
	}

	/// <summary>
	///     Creates a new article with the supplied title, content, and author.
	/// </summary>
	/// <param name="title">The article title.</param>
	/// <param name="content">The article body content.</param>
	/// <param name="author">The author snapshot for the article.</param>
	/// <returns>A new <see cref="Article" /> instance.</returns>
	public static Article Create(string title, string content, PostAuthor author)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(content);
		ArgumentNullException.ThrowIfNull(author);
		ArgumentException.ThrowIfNullOrWhiteSpace(author.Name);

		return new Article
		{
			Id = ObjectId.GenerateNewId(),
			Title = title,
			Content = content,
			Author = author,
			CreatedAt = DateTime.UtcNow,
		};
	}
}
