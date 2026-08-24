// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleImageBackfillMigrationTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Web.Components.Features.Articles.Entities;
using Web.Components.Features.Articles.Models;
using Web.Components.Features.AuthInfo.Entities;
using Web.Data;

namespace Web.Tests.Data;

public class ArticleImageBackfillMigrationTests
{
	[Fact]
	public async Task RunAsyncThrowsArgumentNullExceptionWhenContextIsNullAsync()
	{
		// Act
		var act = () => ArticleImageBackfillMigration.RunAsync(null!, TestContext.Current.CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task RunAsyncPopulatesArticleImagesForArticlesPersistedBeforeTheFieldExistedAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var article = Article.Create("Post", "![alt text](https://example.com/uploads/a1b2.jpg)",
			new AuthorDto("author-1", "Ada", "ada@example.com"));
		typeof(Article).GetProperty(nameof(Article.ArticleImages))!.SetValue(article, new List<ArticleImage>());
		await context.Articles.AddAsync(article, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		// Act
		await ArticleImageBackfillMigration.RunAsync(context, TestContext.Current.CancellationToken);

		// Assert
		var stored = await context.Articles.AsNoTracking()
			.FirstAsync(a => a.Id == article.Id, TestContext.Current.CancellationToken);
		stored.ArticleImages.Should().ContainSingle(image => image.FileName == "a1b2.jpg");
	}

	[Fact]
	public async Task RunAsyncLeavesArticlesWithAlreadyPopulatedImagesUnchangedAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var article = Article.Create("Post", "![alt text](https://example.com/uploads/a1b2.jpg)",
			new AuthorDto("author-1", "Ada", "ada@example.com"));
		await context.Articles.AddAsync(article, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		var expectedUpdatedAt = article.UpdatedAt;

		// Act
		await ArticleImageBackfillMigration.RunAsync(context, TestContext.Current.CancellationToken);

		// Assert
		var stored = await context.Articles.AsNoTracking()
			.FirstAsync(a => a.Id == article.Id, TestContext.Current.CancellationToken);
		stored.UpdatedAt.Should().Be(expectedUpdatedAt);
	}

	private static ArticlesMongoDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new ArticlesMongoDbContext(options);
	}
}
