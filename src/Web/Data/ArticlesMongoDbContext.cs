// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticlesMongoDbContext.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Microsoft.EntityFrameworkCore;

using MongoDB.EntityFrameworkCore.Extensions;

using Web.Components.Features.Articles.Entities;
using Web.Components.Features.Categories.Entities;

namespace Web.Data;

/// <summary>
///     Provides the MongoDB-backed EF Core data context for article and category persistence.
/// </summary>
public sealed class ArticlesMongoDbContext : DbContext
{
	/// <summary>
	///     Initializes a new instance of the <see cref="ArticlesMongoDbContext" /> class.
	/// </summary>
	/// <param name="options">The EF Core database options.</param>
	public ArticlesMongoDbContext(DbContextOptions<ArticlesMongoDbContext> options)
		: base(options)
	{
	}

	/// <summary>
	///     Gets the article set managed by the context.
	/// </summary>
	public DbSet<Article> Articles { get; set; } = null!;

	/// <summary>
	///     Gets the category set managed by the context.
	/// </summary>
	public DbSet<Category> Categories { get; set; } = null!;

	/// <inheritdoc />
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		ArgumentNullException.ThrowIfNull(modelBuilder);

		modelBuilder.Entity<Article>().ToCollection("articles");
		modelBuilder.Entity<Article>().OwnsOne(article => article.Author);
		modelBuilder.Entity<Article>().OwnsOne(article => article.Category);
		modelBuilder.Entity<Article>().OwnsMany(article => article.ArticleImages, image => image.HasKey(articleImage => articleImage.FileName));
		modelBuilder.Entity<Category>().ToCollection("categories");

		base.OnModelCreating(modelBuilder);
	}
}
