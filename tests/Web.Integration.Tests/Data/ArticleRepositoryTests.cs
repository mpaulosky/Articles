// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleRepositoryTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

using Web.Components.Features.Articles.Entities;
using Web.Components.Features.AuthInfo.Entities;
using Web.Integration.Tests.Fixtures;

namespace Web.Integration.Tests.Data;

[Collection(MongoTestCollectionDefinition.Name)]
public class ArticleRepositoryTests
{
	private readonly MongoContainerFixture _fixture;

	public ArticleRepositoryTests(MongoContainerFixture fixture)
	{
		_fixture = fixture;
	}

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
	public async Task AddAsync_PersistsArticle_AgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);
		var article = Article.Create("Title", "Content", new AuthorDto("user-1", "Author", "author@example.com"));

		// Act
		await repository.AddAsync(article, TestContext.Current.CancellationToken);

		// Assert
		var stored = await repository.GetByIdAsync(article.Id, TestContext.Current.CancellationToken);
		stored.Should().NotBeNull();
		stored!.Title.Should().Be("Title");
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

	[Fact]
	public async Task GetByIdAsync_ReturnsArticle_WhenFoundAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);
		var article = Article.Create("Title", "Content", new AuthorDto("user-1", "Author", "author@example.com"));
		await repository.AddAsync(article, TestContext.Current.CancellationToken);

		// Act
		var found = await repository.GetByIdAsync(article.Id, TestContext.Current.CancellationToken);

		// Assert
		found.Should().NotBeNull();
		found!.Id.Should().Be(article.Id);
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsNull_WhenNotFoundAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);

		// Act
		var found = await repository.GetByIdAsync(ObjectId.GenerateNewId(), TestContext.Current.CancellationToken);

		// Assert
		found.Should().BeNull();
	}

	[Fact]
	public async Task GetBySlugAsync_ReturnsArticle_WhenFoundAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);
		var article = Article.Create("Title", "Content", new AuthorDto("user-1", "Author", "author@example.com"));
		await repository.AddAsync(article, TestContext.Current.CancellationToken);

		// Act
		var found = await repository.GetBySlugAsync(article.Slug, TestContext.Current.CancellationToken);

		// Assert
		found.Should().NotBeNull();
		found!.Slug.Should().Be(article.Slug);
	}

	[Fact]
	public async Task GetBySlugAsync_ReturnsNull_WhenNotFoundAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);

		// Act
		var found = await repository.GetBySlugAsync("does-not-exist", TestContext.Current.CancellationToken);

		// Assert
		found.Should().BeNull();
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
	public async Task UpdateAsync_PersistsChanges_AgainstTheRealMongoContainerAsync()
	{
		// Arrange
		var databaseName = $"{nameof(ArticleRepositoryTests)}-{Guid.NewGuid()}";
		await using var writeContext = _fixture.CreateContext(databaseName);
		var repository = new ArticleRepository(writeContext);
		var article = Article.Create("Title", "Content", new AuthorDto("user-1", "Author", "author@example.com"));
		await repository.AddAsync(article, TestContext.Current.CancellationToken);

		article.Update("Title", "Updated Content");

		// Act
		await repository.UpdateAsync(article, TestContext.Current.CancellationToken);

		// Assert
		var stored = await repository.GetByIdAsync(article.Id, TestContext.Current.CancellationToken);
		stored.Should().NotBeNull();
		stored!.Content.Should().Be("Updated Content");
	}

	[Fact]
	public async Task UpdateAsync_HandlesDetachedEntity_WhenFetchedFromADifferentContextAsync()
	{
		// Arrange
		var databaseName = $"{nameof(ArticleRepositoryTests)}-{Guid.NewGuid()}";
		await using var seedContext = _fixture.CreateContext(databaseName);
		var seedRepository = new ArticleRepository(seedContext);
		var article = Article.Create("Title", "Content", new AuthorDto("user-1", "Author", "author@example.com"));
		await seedRepository.AddAsync(article, TestContext.Current.CancellationToken);

		await using var readContext = _fixture.CreateContext(databaseName);
		var readRepository = new ArticleRepository(readContext);
		var detached = await readRepository.GetByIdAsync(article.Id, TestContext.Current.CancellationToken);
		detached!.Update("Title", "Detached Update");

		await using var updateContext = _fixture.CreateContext(databaseName);
		var updateRepository = new ArticleRepository(updateContext);
		var trackedCopy = await updateContext.Articles
			.FirstAsync(a => a.Id == article.Id, TestContext.Current.CancellationToken);

		// Act
		var act = () => updateRepository.UpdateAsync(detached, TestContext.Current.CancellationToken);

		// Assert
		await act.Should().NotThrowAsync();
		var stored = await updateRepository.GetByIdAsync(article.Id, TestContext.Current.CancellationToken);
		stored!.Content.Should().Be("Detached Update");
		trackedCopy.Should().NotBeNull();
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
		await repository.AddAsync(article, TestContext.Current.CancellationToken);

		// Act
		var result = await repository.DeleteAsync(article.Id, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		var retrieved = await repository.GetByIdAsync(article.Id, TestContext.Current.CancellationToken);
		retrieved.Should().BeNull();
	}

	private ArticlesMongoDbContext CreateContext()
	{
		return _fixture.CreateContext($"{nameof(ArticleRepositoryTests)}-{Guid.NewGuid()}");
	}
}
