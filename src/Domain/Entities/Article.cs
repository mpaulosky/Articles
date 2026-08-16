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
	///     Gets the optimistic concurrency version for the article.
	/// </summary>
	public int Version { get; private set; }

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

	/// <summary>
	///     Updates the article title, content, and optional category assignment.
	/// </summary>
	/// <param name="title">The updated article title.</param>
	/// <param name="content">The updated article body content.</param>
	/// <param name="categoryId">The category identifier to assign when provided.</param>
	/// <param name="clearCategory">Whether to remove the existing category when no category identifier is provided.</param>
	public void Update(string title, string content, ObjectId? categoryId = null, bool clearCategory = false)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(content);
		Title = title;
		Content = content;
		UpdatedAt = DateTime.UtcNow;

		if (categoryId.HasValue)
		{
			CategoryId = categoryId.Value;
		}
		else if (clearCategory)
		{
			CategoryId = null;
		}

		Version++;
	}

	/// <summary>
	///     Marks the article as published.
	/// </summary>
	public void Publish() => IsPublished = true;

	/// <summary>
	///     Marks the article as unpublished.
	/// </summary>
	public void Unpublish() => IsPublished = false;

	/// <summary>
	///     Assigns the article to a category.
	/// </summary>
	/// <param name="categoryId">The category identifier to assign.</param>
	public void AssignCategory(ObjectId categoryId)
	{
		CategoryId = categoryId;
		Version++;
	}

	/// <summary>
	///     Removes the article category assignment.
	/// </summary>
	public void RemoveCategory()
	{
		CategoryId = null;
		Version++;
	}
}