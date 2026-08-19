// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleRepository.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Microsoft.EntityFrameworkCore;

using MongoDB.Bson;

using Web.Components.Features.Articles.Entities;

namespace Web.Data;

/// <summary>
///     Provides repository operations for article persistence.
/// </summary>
public sealed class ArticleRepository
{
	private readonly ArticlesMongoDbContext _context;

	/// <summary>
	///     Initializes a new instance of the <see cref="ArticleRepository" /> class.
	/// </summary>
	/// <param name="context">The MongoDB data context.</param>
	public ArticleRepository(ArticlesMongoDbContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	/// <summary>
	///     Adds a new article to the repository.
	/// </summary>
	/// <param name="article">The article to create.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The created article.</returns>
	public async Task<Article> AddAsync(Article article, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(article);

		await _context.Articles.AddAsync(article, cancellationToken).ConfigureAwait(false);
		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		return article;
	}

	/// <summary>
	///     Gets all articles ordered by creation date.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The articles in the repository.</returns>
	public async Task<List<Article>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await _context.Articles
			.AsNoTracking()
			.OrderBy(article => article.CreatedAt)
			.ToListAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	/// <summary>
	///     Gets a single article by identifier.
	/// </summary>
	/// <param name="id">The article identifier.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The article if it exists; otherwise, null.</returns>
	public async Task<Article?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
	{
		return await _context.Articles
			.AsNoTracking()
			.FirstOrDefaultAsync(article => article.Id == id, cancellationToken)
			.ConfigureAwait(false);
	}

	/// <summary>
	///     Updates an existing article.
	/// </summary>
	/// <param name="article">The article to update.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The updated article.</returns>
	public async Task<Article> UpdateAsync(Article article, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(article);

		_context.Articles.Update(article);
		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		return article;
	}

	/// <summary>
	///     Deletes an existing article by identifier.
	/// </summary>
	/// <param name="id">The article identifier.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>True when the article was deleted; otherwise, false.</returns>
	public async Task<bool> DeleteAsync(ObjectId id, CancellationToken cancellationToken = default)
	{
		var article = await _context.Articles
			.FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken)
			.ConfigureAwait(false);

		if (article is null)
		{
			return false;
		}

		_context.Articles.Remove(article);
		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		return true;
	}
}
