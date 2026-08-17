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
	///     Gets the version number of the article as it evolves over time.
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
			Title = title.Trim(),
			Content = content.Trim(),
			Author = author,
			CreatedAt = DateTime.UtcNow,
			Version = 0,
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
		Touch();
	}

	/// <summary>
	///     Updates the article title, body, and category metadata.
	/// </summary>
	/// <param name="title">The updated article title.</param>
	/// <param name="content">The updated article body content.</param>
	/// <param name="categoryId">The category identifier to assign.</param>
	/// <param name="clearCategory">When true, clears any assigned category.</param>
	public void Update(string title, string content, ObjectId? categoryId = null, bool clearCategory = false)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(content);

		Title = title.Trim();
		Content = content.Trim();

		if (clearCategory)
		{
			CategoryId = null;
		}
		else if (categoryId.HasValue)
		{
			CategoryId = categoryId;
		}

		Touch();
	}

	/// <summary>
	///     Assigns the article to the specified category.
	/// </summary>
	/// <param name="categoryId">The category identifier to assign.</param>
	public void AssignCategory(ObjectId categoryId)
	{
		CategoryId = categoryId;
		Touch();
	}

	/// <summary>
	///     Removes the article from its current category.
	/// </summary>
	public void RemoveCategory()
	{
		CategoryId = null;
		Touch();
	}

	private void Touch()
	{
		UpdatedAt = DateTime.UtcNow;
		Version++;
	}
}
