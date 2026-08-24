using Domain.Abstractions;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using MongoDB.Bson;

using Web.Components.Features.Articles.Commands;
using Web.Components.Features.Articles.Handlers;
using Web.Components.Features.Articles.Queries;
using Web.Components.Features.Articles.Validators;
using Web.Components.Features.Articles.Services;
using Web.Components.Features.Categories.Models;
using Web.Data;
using Web.TestData;

namespace Web.Tests.Features.Articles.Handlers;

public class ArticleFeatureHandlerTests
{
	[Fact]
	public async Task CreateArticleCommandCreatesArticleAndReturnsDtoAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context));
		var category = CategoryTestData.Dto(categoryName: "Technology", slug: "technology");
		var command = ArticleTestData.CreateCommand(
			title: "My first article",
			slug: "my-first-article",
			content: "This is the article body.",
			author: AuthorTestData.Create(userId: "user-42", name: "Ada Lovelace", email: "ada@example.com"),
			category: category);

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
		var first = ArticleTestData.CreateCommand(
			title: "First article",
			slug: "first-article",
			content: "Body one",
			author: AuthorTestData.Create(userId: "user-10", name: "First Author", email: "one@example.com"),
			category: CategoryTestData.Dto(categoryName: "General", slug: "general"));
		var second = ArticleTestData.CreateCommand(
			title: "Second article",
			slug: "second-article",
			content: "Body two",
			author: AuthorTestData.Create(userId: "user-20", name: "Second Author", email: "two@example.com"),
			category: CategoryTestData.Dto(categoryName: "News", slug: "news"));
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
		var createCommand = ArticleTestData.CreateCommand(
			title: "Existing Article",
			slug: "existing-article",
			content: "Existing Content",
			author: AuthorTestData.Create(userId: "author-1", name: "Author One", email: "author@example.com"));
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
		var createCommand = ArticleTestData.CreateCommand(
			title: "Existing Article",
			slug: "existing-article",
			content: "Existing Content",
			author: AuthorTestData.Create(userId: "author-1", name: "Author One", email: "author@example.com"));
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
		var command = ArticleTestData.UpdateCommand(ObjectId.GenerateNewId().ToString(), title: "", slug: "test-slug", content: "short");

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
		var command = ArticleTestData.UpdateCommand("invalid-id", title: "Valid Title", slug: "valid-title", content: "Valid Content here");

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
		var command = ArticleTestData.UpdateCommand(ObjectId.GenerateNewId().ToString(), title: "Valid Title", slug: "valid-title", content: "Valid Content here");

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
		var category1 = CategoryTestData.Dto(categoryName: "Cat1", slug: "cat1");
		var category2 = CategoryTestData.Dto(categoryName: "Cat2", slug: "cat2");

		var createCommand = ArticleTestData.CreateCommand(
			title: "Initial Title",
			slug: "initial-title",
			content: "Initial Content",
			category: category1);
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var updateCommand = ArticleTestData.UpdateCommand(
			created.Value!.Id,
			title: "Updated Title",
			slug: "updated-title",
			content: "Updated Content",
			category: category2);

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
		var category = CategoryTestData.Dto(categoryName: "Cat1", slug: "cat1");

		var createCommand = ArticleTestData.CreateCommand(
			title: "Initial Title",
			slug: "initial-title",
			content: "Initial Content",
			category: category);
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var updateCommand = ArticleTestData.UpdateCommand(
			created.Value!.Id,
			title: "Updated Title",
			slug: "updated-title",
			content: "Updated Content",
			category: null,
			clearCategory: true);

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
		var createCommand = ArticleTestData.CreateCommand(title: "To Delete", slug: "to-delete", content: "Content to delete");
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
		var createCommand = ArticleTestData.CreateCommand(title: "Draft Article", slug: "draft-article", content: "Draft content");
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
		var createCommand = ArticleTestData.CreateCommand(title: "Live Article", slug: "live-article", content: "Live content");
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
		var createCommand = ArticleTestData.CreateCommand(title: "Article To Archive", slug: "article-to-archive", content: "Content");
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
		var createCommand = ArticleTestData.CreateCommand(title: "Live Archived Article", slug: "live-archived-article", content: "Content");
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
		var createCommand = ArticleTestData.CreateCommand(title: "Already Archived Article", slug: "already-archived-article", content: "Content");
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
		var createCommand = ArticleTestData.CreateCommand(title: "Article To Unarchive", slug: "article-to-unarchive", content: "Content");
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
		var createCommand = ArticleTestData.CreateCommand(title: "Already Unarchived Article", slug: "already-unarchived-article", content: "Content");
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

	[Fact]
	public async Task UpdateArticleCommandDeletesImagesRemovedFromContentAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var fileStorage = new RecordingFileStorage();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context), fileStorage: fileStorage);
		var createCommand = ArticleTestData.CreateCommand(
			title: "Initial Title",
			slug: "initial-title",
			content: "Before ![](https://example.com/uploads/kept.jpg) and ![](https://example.com/uploads/removed.jpg) after");
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var updateCommand = ArticleTestData.UpdateCommand(
			created.Value!.Id,
			title: "Updated Title",
			slug: "updated-title",
			content: "Before ![](https://example.com/uploads/kept.jpg) after");

		// Act
		var updateResult = await handler.Handle(updateCommand, TestContext.Current.CancellationToken);

		// Assert
		updateResult.Success.Should().BeTrue();
		fileStorage.DeletedFileNames.Should().Equal("removed.jpg");
	}

	[Fact]
	public async Task UpdateArticleCommandDeletesNothingWhenNoImagesAreRemovedAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var fileStorage = new RecordingFileStorage();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context), fileStorage: fileStorage);
		var createCommand = ArticleTestData.CreateCommand(
			title: "Initial Title",
			slug: "initial-title",
			content: "Has ![](https://example.com/uploads/kept.jpg) image");
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var updateCommand = ArticleTestData.UpdateCommand(
			created.Value!.Id,
			title: "Updated Title",
			slug: "updated-title",
			content: "Has ![](https://example.com/uploads/kept.jpg) image, still");

		// Act
		var updateResult = await handler.Handle(updateCommand, TestContext.Current.CancellationToken);

		// Assert
		updateResult.Success.Should().BeTrue();
		fileStorage.DeletedFileNames.Should().BeEmpty();
	}

	[Fact]
	public async Task DeleteArticleCommandDeletesAllOfTheArticlesImagesAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var fileStorage = new RecordingFileStorage();
		var handler = new ArticleFeatureHandler(new ArticleRepository(context), fileStorage: fileStorage);
		var createCommand = ArticleTestData.CreateCommand(
			title: "To Delete",
			slug: "to-delete",
			content: "Has ![](https://example.com/uploads/first.jpg) and ![](https://example.com/uploads/second.jpg)");
		var created = await handler.Handle(createCommand, TestContext.Current.CancellationToken);

		var command = new DeleteArticleCommand(created.Value!.Id);

		// Act
		var result = await handler.Handle(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		fileStorage.DeletedFileNames.Should().BeEquivalentTo(["first.jpg", "second.jpg"]);
	}

	private static ArticlesMongoDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new ArticlesMongoDbContext(options);
	}

	private sealed class RecordingFileStorage : IFileStorage
	{
		public List<string> DeletedFileNames { get; } = [];

		public Task<string> AddFile(FileData fileData) => Task.FromResult(Guid.NewGuid().ToString());

		public Task DeleteFile(string fileName)
		{
			DeletedFileNames.Add(fileName);
			return Task.CompletedTask;
		}
	}
}
