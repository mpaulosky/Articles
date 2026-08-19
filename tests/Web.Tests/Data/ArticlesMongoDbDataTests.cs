using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using MongoDB.Bson;

using Web.Components.Features.Articles.Entities;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Entities;
using Web.Components.Features.Categories.Models;
using Web.Data;

namespace Web.Tests.Data;

public class ArticlesMongoDbDataTests
{
	[Fact]
	public void ArticlesMongoDbContextFactoryUsesDatabaseNameFromConfiguration()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["ConnectionStrings:articlesdb"] = "mongodb://localhost:27017", ["MONGODB_DATABASE_NAME"] = "articlesdb-test"
			})
			.Build();

		// Act
		var factory = new ArticlesMongoDbContextFactory(configuration);
		using var context = factory.Create();

		// Assert
		context.Database.Should().NotBeNull();
		context.Articles.Should().NotBeNull();
		context.Categories.Should().NotBeNull();
	}

	[Fact]
	public async Task ArticleRepositoryCanAddAndLoadArticlesAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);
		var article = Article.Create("Testing repository", "Repository content",
			new AuthorDto("user-123", "Ada Lovelace", "ada@example.com"));

		// Act
		var created = await repository.AddAsync(article, TestContext.Current.CancellationToken);
		var loaded = await repository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);

		// Assert
		created.Id.Should().NotBe(ObjectId.Empty);
		loaded.Should().NotBeNull();
		loaded!.Title.Should().Be("Testing repository");
		loaded.Content.Should().Be("Repository content");
	}

	[Fact]
	public async Task CategoryRepositoryCanPersistAndDeleteCategoriesAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var category = Category.Create("Technology", "Code and architecture");

		// Act
		var created = await repository.AddAsync(category, TestContext.Current.CancellationToken);
		var loaded = await repository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);
		await repository.DeleteAsync(created.Id, TestContext.Current.CancellationToken);
		var deleted = await repository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);

		// Assert
		loaded.Should().NotBeNull();
		loaded!.Name.Should().Be("Technology");
		deleted.Should().BeNull();
	}

	[Fact]
	public async Task ArticleRepositoryCanUpdateAndDeleteArticlesAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new ArticleRepository(context);
		var article = Article.Create("Initial title", "Initial content",
			new AuthorDto("user-456", "Grace Hopper", "grace@example.com"));
		var category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology",
			CreatedOn = DateTime.UtcNow,
			IsArchived = false
		};

		// Act
		var created = await repository.AddAsync(article, TestContext.Current.CancellationToken);
		created.Update("Updated title", "Updated content", category);
		var updated = await repository.UpdateAsync(created, TestContext.Current.CancellationToken);
		var changed = await repository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);
		var deleted = await repository.DeleteAsync(created.Id, TestContext.Current.CancellationToken);
		var missing = await repository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);

		// Assert
		updated.Title.Should().Be("Updated title");
		updated.Content.Should().Be("Updated content");
		updated.Category.CategoryName.Should().Be("Technology");
		changed.Should().NotBeNull();
		deleted.Should().BeTrue();
		missing.Should().BeNull();
	}

	[Fact]
	public async Task CategoryRepositoryCanUpdateAndListCategoriesAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var first = Category.Create("Alpha", "One");
		var second = Category.Create("Zulu", "Three");

		// Act
		await repository.AddAsync(first, TestContext.Current.CancellationToken);
		await repository.AddAsync(second, TestContext.Current.CancellationToken);
		first.Update("Beta", "Updated description");
		await repository.UpdateAsync(first, TestContext.Current.CancellationToken);
		var categories = await repository.GetAllAsync(TestContext.Current.CancellationToken);

		// Assert
		categories.Should().HaveCount(2);
		categories.Select(category => category.Name).Should().ContainInOrder("Beta", "Zulu");
		categories.Should().Contain(category => category.Name == "Beta" && category.ModifiedOn.HasValue);
	}

	[Fact]
	public void ArticleLifecycleTransitionsUpdateTimestampAndVisibility()
	{
		// Arrange
		var article = Article.Create("Lifecycle", "Body",
			new AuthorDto("user-789", "Linus Torvalds", "linus@example.com"));

		// Act
		article.Publish();
		var publishedAt = article.UpdatedAt;
		article.Unpublish();

		// Assert
		article.IsPublished.Should().BeFalse();
		publishedAt.Should().NotBeNull();
		article.UpdatedAt.Should().NotBeNull();
		article.UpdatedAt.Should().BeAfter(publishedAt!.Value);
	}

	private static ArticlesMongoDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new ArticlesMongoDbContext(options);
	}
}
