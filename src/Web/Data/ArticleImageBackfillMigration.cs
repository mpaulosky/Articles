// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleImageBackfillMigration.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Microsoft.EntityFrameworkCore;

namespace Web.Data;

/// <summary>
///     One-time backfill that populates <c>Article.ArticleImages</c> for articles persisted
///     before that field existed, by parsing their <c>Content</c>. Idempotent: articles that
///     already have a populated array are left untouched, so it is safe to run on every
///     application startup. See ADR-0003.
/// </summary>
public static class ArticleImageBackfillMigration
{
	/// <summary>
	///     Backfills <c>ArticleImages</c> for every article whose array is still empty.
	/// </summary>
	/// <param name="context">The MongoDB data context.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	public static async Task RunAsync(ArticlesMongoDbContext context, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(context);

		var articles = await context.Articles
			.ToListAsync(cancellationToken)
			.ConfigureAwait(false);

		var articlesNeedingBackfill = articles.Where(article => article.ArticleImages.Count == 0).ToList();

		if (articlesNeedingBackfill.Count == 0)
		{
			return;
		}

		foreach (var article in articlesNeedingBackfill)
		{
			article.BackfillArticleImages();
		}

		await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}
}
