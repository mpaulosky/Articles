using Domain.Abstractions;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using MongoDB.Bson;

using Web.Components.Features.Articles.Commands;
using Web.Components.Features.Articles.Handlers;
using Web.Components.Features.Articles.Queries;
using Web.Components.Features.Articles.Validators;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Models;
using Web.Data;

namespace Web.Tests.Features.Articles;

public class ArticleFeatureHandlerTests
{
	[Fact]
	public async Task CreateArticleCommandCreatesArticleAndReturnsDtoAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology",
			CreatedOn = DateTime.UtcNow,
			IsArchived = false
		};
		var command = new CreateArticleCommand(
			"My first article",
			"my-first-article",
			"This is the article body.",
			new AuthorDto("user-42", "Ada Lovelace", "ada@example.com"),
			category);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Title.Should().Be("My first article");
		result.Value.Content.Should().Contain("article body");
		result.Value.Author.Name.Should().Be("Ada Lovelace");
		result.Value.Category.CategoryName.Should().Be("Technology");
	}

	[Fact]
	public async Task GetArticlesQueryReturnsArticlesOrderedByCreatedDateAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var first = new CreateArticleCommand(
			"First article",
			"first-article",
			"Body one",
			new AuthorDto("user-10", "First Author", "one@example.com"),
			new CategoryDto
			{
				Id = ObjectId.GenerateNewId(), CategoryName = "General", Slug = "general", CreatedOn = DateTime.UtcNow
			});
		var second = new CreateArticleCommand(
			"Second article",
			"second-article",
			"Body two",
			new AuthorDto("user-20", "Second Author", "two@example.com"),
			new CategoryDto
			{
				Id = ObjectId.GenerateNewId(), CategoryName = "News", Slug = "news", CreatedOn = DateTime.UtcNow
			});
		await handler.Handle(first, TestContext.Current.CancellationToken);
		await handler.Handle(second, TestContext.Current.CancellationToken);

		// Act
		var result = await handler.Handle(new GetArticlesQuery(), TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value!.Select(article => article.Title).Should().ContainInOrder("First article", "Second article");
	}

	[Fact]
	public async Task CreateArticleCommandReturnsValidationFailureForInvalidInputAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context), new CreateArticleCommandValidator());
		var command = new CreateArticleCommand("A", "a", "short", null!);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task GetArticleByIdQueryReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));

		// Act
		var result =
			await handler.Handle(new GetArticleByIdQuery("not-an-object-id"), TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task GetArticleByIdQueryReturnsNotFoundWhenArticleDoesNotExistAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var randomId = ObjectId.GenerateNewId().ToString();

		// Act
		var result = await handler.Handle(new GetArticleByIdQuery(randomId), TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Be("Article not found.");
	}

	[Fact]
	public async Task GetArticleByIdQueryReturnsArticleWhenFoundAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var createCommand = new CreateArticleCommand(
			"Existing Article",
			"existing-article",
			"Existing Content",
			new AuthorDto("author-1", "Author One", "author@example.com"));
		var createdResult = await handler.Handle(createCommand, TestContext.Current.CancellationToken);
		var articleId = createdResult.Value!.Id;

		// Act
		var result = await handler.Handle(new GetArticleByIdQuery(articleId), TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(articleId);
		result.Value.Title.Should().Be("Existing Article");
	}

	[Fact]
	public async Task GetArticleBySlugQueryReturnsNotFoundWhenArticleDoesNotExistAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));

		// Act
		var result =
			await handler.Handle(new GetArticleBySlugQuery("missing-slug"), TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Be("Article not found.");
	}

	[Fact]
	public async Task GetArticleBySlugQueryReturnsArticleWhenFoundAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var createCommand = new CreateArticleCommand(
			"Existing Article",
			"existing-article",
			"Existing Content",
			new AuthorDto("author-1", "Author One", "author@example.com"));
		var createdResult = await handler.Handle(createCommand, TestContext.Current.CancellationToken);
		var articleId = createdResult.Value!.Id;

		// Act
		var result =
			await handler.Handle(new GetArticleBySlugQuery("existing-article"), TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(articleId);
		result.Value.Title.Should().Be("Existing Article");
	}

	[Fact]
	public async Task UpdateArticleCommandReturnsValidationFailureForInvalidInputAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context), updateValidator: new UpdateArticleCommandValidator());
		var command = new UpdateArticleCommand(ObjectId.GenerateNewId().ToString(), "", "test-slug", "short");

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task UpdateArticleCommandReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new UpdateArticleCommand("invalid-id", "Valid Title", "valid-title", "Valid Content here");

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Be("The article id is not valid.");
	}

	[Fact]
	public async Task UpdateArticleCommandReturnsNotFoundWhenArticleDoesNotExistAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new UpdateArticleCommand(ObjectId.GenerateNewId().ToString(), "Valid Title", "valid-title", "Valid Content here");

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Be("Article not found.");
	}

	[Fact]
	public async Task UpdateArticleCommandUpdatesArticleSuccessfullyAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var category1 = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Cat1",
			Slug = "cat1",
			CreatedOn = DateTime.UtcNow,
			IsArchived = false
		};
		var category2 = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Cat2",
			Slug = "cat2",
			CreatedOn = DateTime.UtcNow,
			IsArchived = false
		};

		var createCommand = new CreateArticleCommand(
			"Initial Title",
			"initial-title",
			"Initial Content",
			new AuthorDto("user-1", "Author", "author@example.com"),
			category1);
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var updateCommand = new UpdateArticleCommand(
			created.Value!.Id,
			"Updated Title",
			"updated-title",
			"Updated Content",
			category2);

		// Act
		var updateResult = await handler.Handle(updateCommand, TestContext.Current.CancellationToken);

		// Assert
		updateResult.Success.Should().BeTrue();
		updateResult.Value!.Title.Should().Be("Updated Title");
		updateResult.Value.Content.Should().Be("Updated Content");
		updateResult.Value.Category.CategoryName.Should().Be("Cat2");
	}

	[Fact]
	public async Task UpdateArticleCommandClearsCategorySuccessfullyAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Cat1",
			Slug = "cat1",
			CreatedOn = DateTime.UtcNow,
			IsArchived = false
		};

		var createCommand = new CreateArticleCommand(
			"Initial Title",
			"initial-title",
			"Initial Content",
			new AuthorDto("user-1", "Author", "author@example.com"),
			category);
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var updateCommand = new UpdateArticleCommand(
			created.Value!.Id,
			"Updated Title",
			"updated-title",
			"Updated Content",
			Category: null,
			ClearCategory: true);

		// Act
		var updateResult = await handler.Handle(updateCommand, TestContext.Current.CancellationToken);

		// Assert
		updateResult.Success.Should().BeTrue();
		updateResult.Value!.Category.Should().BeEquivalentTo(CategoryDto.Empty);
	}

	[Fact]
	public async Task DeleteArticleCommandReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new DeleteArticleCommand("invalid-id");

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Be("The article id is not valid.");
	}

	[Fact]
	public async Task DeleteArticleCommandReturnsNotFoundWhenArticleDoesNotExistAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new DeleteArticleCommand(ObjectId.GenerateNewId().ToString());

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Be("Article not found.");
	}

	[Fact]
	public async Task DeleteArticleCommandDeletesArticleSuccessfullyAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var createCommand = new CreateArticleCommand(
			"To Delete",
			"to-delete",
			"Content to delete",
			new AuthorDto("user-1", "Author", "author@example.com"));
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var command = new DeleteArticleCommand(created.Value!.Id);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		var getResult = await handler.Handle(new GetArticleByIdQuery(created.Value.Id), TestContext.Current.CancellationToken);
		getResult.Success.Should().BeFalse();
		getResult.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task PublishArticleCommandPublishesArticleAndSetsPublishedOnAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var createCommand = new CreateArticleCommand(
			"Draft Article",
			"draft-article",
			"Draft content",
			new AuthorDto("user-1", "Author", "author@example.com"));
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var command = new PublishArticleCommand(created.Value!.Id);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsPublished.Should().BeTrue();
		result.Value.PublishedOn.Should().NotBeNull();
	}

	[Fact]
	public async Task PublishArticleCommandReturnsNotFoundWhenArticleDoesNotExistAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new PublishArticleCommand(ObjectId.GenerateNewId().ToString());

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task PublishArticleCommandReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new PublishArticleCommand("invalid-id");

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task UnpublishArticleCommandUnpublishesArticleAndClearsPublishedOnAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var createCommand = new CreateArticleCommand(
			"Live Article",
			"live-article",
			"Live content",
			new AuthorDto("user-1", "Author", "author@example.com"));
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);
		await handler.Handle(new PublishArticleCommand(created.Value!.Id), TestContext.Current.CancellationToken);

		var command = new UnpublishArticleCommand(created.Value.Id);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsPublished.Should().BeFalse();
		result.Value.PublishedOn.Should().BeNull();
	}

	[Fact]
	public async Task UnpublishArticleCommandReturnsNotFoundWhenArticleDoesNotExistAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new UnpublishArticleCommand(ObjectId.GenerateNewId().ToString());

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task UnpublishArticleCommandReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new UnpublishArticleCommand("invalid-id");

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task ArchiveArticleCommandArchivesArticleAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var createCommand = new CreateArticleCommand(
			"Article To Archive",
			"article-to-archive",
			"Content",
			new AuthorDto("user-1", "Author", "author@example.com"));
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var command = new ArchiveArticleCommand(created.Value!.Id);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeTrue();
	}

	[Fact]
	public async Task ArchiveArticleCommandDoesNotChangePublishedStateAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var createCommand = new CreateArticleCommand(
			"Live Archived Article",
			"live-archived-article",
			"Content",
			new AuthorDto("user-1", "Author", "author@example.com"));
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);
		await handler.Handle(new PublishArticleCommand(created.Value!.Id), TestContext.Current.CancellationToken);

		var command = new ArchiveArticleCommand(created.Value.Id);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeTrue();
		result.Value.IsPublished.Should().BeTrue();
	}

	[Fact]
	public async Task ArchiveArticleCommandIsIdempotentWhenArticleIsAlreadyArchivedAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var createCommand = new CreateArticleCommand(
			"Already Archived Article",
			"already-archived-article",
			"Content",
			new AuthorDto("user-1", "Author", "author@example.com"));
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);
		await handler.Handle(new ArchiveArticleCommand(created.Value!.Id), TestContext.Current.CancellationToken);

		var command = new ArchiveArticleCommand(created.Value.Id);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeTrue();
	}

	[Fact]
	public async Task ArchiveArticleCommandReturnsNotFoundWhenArticleDoesNotExistAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new ArchiveArticleCommand(ObjectId.GenerateNewId().ToString());

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task ArchiveArticleCommandReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new ArchiveArticleCommand("invalid-id");

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task UnarchiveArticleCommandUnarchivesArticleAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var createCommand = new CreateArticleCommand(
			"Article To Unarchive",
			"article-to-unarchive",
			"Content",
			new AuthorDto("user-1", "Author", "author@example.com"));
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);
		await handler.Handle(new ArchiveArticleCommand(created.Value!.Id), TestContext.Current.CancellationToken);

		var command = new UnarchiveArticleCommand(created.Value.Id);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeFalse();
	}

	[Fact]
	public async Task UnarchiveArticleCommandIsIdempotentWhenArticleIsAlreadyUnarchivedAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var createCommand = new CreateArticleCommand(
			"Already Unarchived Article",
			"already-unarchived-article",
			"Content",
			new AuthorDto("user-1", "Author", "author@example.com"));
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var command = new UnarchiveArticleCommand(created.Value!.Id);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeFalse();
	}

	[Fact]
	public async Task UnarchiveArticleCommandReturnsNotFoundWhenArticleDoesNotExistAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new UnarchiveArticleCommand(ObjectId.GenerateNewId().ToString());

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task UnarchiveArticleCommandReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var command = new UnarchiveArticleCommand("invalid-id");

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	private static ArticlesMongoDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new ArticlesMongoDbContext(options);
	}
}
