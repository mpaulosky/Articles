using Domain.Abstractions;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using MongoDB.Bson;

using Web.Components.Features.Categories.Commands;
using Web.Components.Features.Categories.Entities;
using Web.Components.Features.Categories.Handlers;
using Web.Components.Features.Categories.Queries;
using Web.Components.Features.Categories.Validators;
using Web.Data;

namespace Web.Tests.Features.Categories.Handlers;

public class CategoryFeatureHandlerTests
{
	[Fact]
	public async Task CreateCategoryCommandCreatesCategoryAndReturnsDtoAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context));
		var command = new CreateCategoryCommand("Technology", "Technology articles");

		// Act
		var result = await handler.Handle(command, cancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.CategoryName.Should().Be("Technology");
		result.Value.Description.Should().Be("Technology articles");
		result.Value.Slug.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task GetCategoriesQueryReturnsCategoriesOrderedByNameAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var handler = new CategoryFeatureHandler(repository);
		await repository.AddAsync(Category.Create("Zebra", "Zebra description"), cancellationToken);
		await repository.AddAsync(Category.Create("Alpha", "Alpha description"), cancellationToken);

		// Act
		var result = await handler.Handle(new GetCategoriesQuery(), cancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value!.Select(category => category.CategoryName).Should().ContainInOrder("Alpha", "Zebra");
	}

	[Fact]
	public async Task UpdateCategoryCommandUpdatesExistingCategoryAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var handler = new CategoryFeatureHandler(repository);
		var created = await repository.AddAsync(Category.Create("News", "Old description"), cancellationToken);
		var command = new UpdateCategoryCommand(created.Id.ToString(), "Daily News", "Updated description");

		// Act
		var result = await handler.Handle(command, cancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.CategoryName.Should().Be("Daily News");
		result.Value.Description.Should().Be("Updated description");
	}

	[Fact]
	public async Task ArchiveCategoryCommandArchivesExistingCategoryAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var handler = new CategoryFeatureHandler(repository);
		var created = await repository.AddAsync(Category.Create("Archive", "To archive"), cancellationToken);

		// Act
		var result = await handler.Handle(new ArchiveCategoryCommand(created.Id.ToString()), cancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeTrue();
		(await repository.GetByIdAsync(created.Id, cancellationToken))!.IsArchived.Should().BeTrue();
	}

	[Fact]
	public async Task UnarchiveCategoryCommandUnarchivesExistingCategoryAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var handler = new CategoryFeatureHandler(repository);
		var created = await repository.AddAsync(Category.Create("Archive", "To unarchive"), cancellationToken);
		created.Archive();
		await repository.UpdateAsync(created, cancellationToken);

		// Act
		var result = await handler.Handle(new UnarchiveCategoryCommand(created.Id.ToString()), cancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeFalse();
		(await repository.GetByIdAsync(created.Id, cancellationToken))!.IsArchived.Should().BeFalse();
	}

	[Fact]
	public async Task CreateCategoryCommandReturnsValidationFailureForInvalidInputAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context), new CreateCategoryCommandValidator());

		// Act
		var result = await handler.Handle(new CreateCategoryCommand("A", "bad"), cancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task GetCategoryByIdQueryReturnsNotFoundForUnknownIdAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context));

		// Act
		var result = await handler.Handle(new GetCategoryByIdQuery(ObjectId.GenerateNewId().ToString()), cancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task GetCategoryByIdQueryReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context));

		// Act
		var result = await handler.Handle(new GetCategoryByIdQuery("invalid-category-id"), cancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Be("The category id is not valid.");
	}

	[Fact]
	public async Task GetCategoryByIdQueryReturnsCategoryWhenFoundAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var handler = new CategoryFeatureHandler(repository);
		var created = await repository.AddAsync(Category.Create("Tech", "Tech news"), cancellationToken);

		// Act
		var result = await handler.Handle(new GetCategoryByIdQuery(created.Id.ToString()), cancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(created.Id);
		result.Value.CategoryName.Should().Be("Tech");
	}

	[Fact]
	public async Task UpdateCategoryCommandReturnsValidationFailureForInvalidInputAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context), updateValidator: new UpdateCategoryCommandValidator());
		var command = new UpdateCategoryCommand(ObjectId.GenerateNewId().ToString(), "", "bad description");

		// Act
		var result = await handler.Handle(command, cancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task UpdateCategoryCommandReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context));
		var command = new UpdateCategoryCommand("invalid-id", "Valid Name", "Valid description");

		// Act
		var result = await handler.Handle(command, cancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Be("The category id is not valid.");
	}

	[Fact]
	public async Task UpdateCategoryCommandReturnsNotFoundWhenCategoryDoesNotExistAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context));
		var command = new UpdateCategoryCommand(ObjectId.GenerateNewId().ToString(), "Valid Name", "Valid description");

		// Act
		var result = await handler.Handle(command, cancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Be("Category not found.");
	}

	[Fact]
	public async Task ArchiveCategoryCommandReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context));
		var command = new ArchiveCategoryCommand("invalid-id");

		// Act
		var result = await handler.Handle(command, cancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Be("The category id is not valid.");
	}

	[Fact]
	public async Task ArchiveCategoryCommandReturnsNotFoundWhenCategoryDoesNotExistAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context));
		var command = new ArchiveCategoryCommand(ObjectId.GenerateNewId().ToString());

		// Act
		var result = await handler.Handle(command, cancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Be("Category not found.");
	}

	[Fact]
	public async Task UnarchiveCategoryCommandReturnsValidationFailureForInvalidIdAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context));
		var command = new UnarchiveCategoryCommand("invalid-id");

		// Act
		var result = await handler.Handle(command, cancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Be("The category id is not valid.");
	}

	[Fact]
	public async Task UnarchiveCategoryCommandReturnsNotFoundWhenCategoryDoesNotExistAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var handler = new CategoryFeatureHandler(new CategoryRepository(context));
		var command = new UnarchiveCategoryCommand(ObjectId.GenerateNewId().ToString());

		// Act
		var result = await handler.Handle(command, cancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Be("Category not found.");
	}

	private static ArticlesMongoDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new ArticlesMongoDbContext(options);
	}
}
