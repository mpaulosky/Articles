using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using MongoDB.Bson;

using Web.Components.Features.Articles.Entities;
using Web.Components.Features.AuthInfo.Entities;
using Web.Data;

namespace Web.Tests.Features.Articles;

public class ArticleRepositoryTests
{
	[Fact]
	public void Constructor_ThrowsArgumentNullException_WhenContextIsNull()
	{
		// Act
		var act = () => new ArticleRepository(null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task AddAsync_ThrowsArgumentNullException_WhenArticleIsNullAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);

		// Act
		var act = () => repository.AddAsync(null!, TestContext.Current.CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task UpdateAsync_ThrowsArgumentNullException_WhenArticleIsNullAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);

		// Act
		var act = () => repository.UpdateAsync(null!, TestContext.Current.CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task DeleteAsync_ReturnsFalse_WhenArticleDoesNotExistAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);

		// Act
		var result = await repository.DeleteAsync(ObjectId.GenerateNewId(), TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteAsync_DeletesExistingArticle_AndReturnsTrueAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);
		var article = Article.Create("Title", "Content", new AuthorDto("user-1", "Author", "author@example.com"));
		var created = await repository.AddAsync(article, TestContext.Current.CancellationToken);

		// Act
		var result = await repository.DeleteAsync(created.Id, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		var retrieved = await repository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);
		retrieved.Should().BeNull();
	}

	[Fact]
	public async Task GetAllAsync_ReturnsArticlesOrderedByCreatedAtAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);
		var article1 = Article.Create("First", "Content 1", new AuthorDto("user-1", "Author", "author@example.com"));
		var article2 = Article.Create("Second", "Content 2", new AuthorDto("user-2", "Author 2", "author2@example.com"));

		await repository.AddAsync(article1, TestContext.Current.CancellationToken);
		await repository.AddAsync(article2, TestContext.Current.CancellationToken);

		// Act
		var articles = await repository.GetAllAsync(TestContext.Current.CancellationToken);

		// Assert
		articles.Should().HaveCount(2);
		articles.Select(a => a.Title).Should().ContainInOrder("First", "Second");
	}

	private static ArticlesMongoDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new ArticlesMongoDbContext(options);
	}
}
