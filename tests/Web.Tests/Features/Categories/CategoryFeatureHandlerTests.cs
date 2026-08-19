using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Web.Components.Features.Categories.Commands;
using Web.Components.Features.Categories.Entities;
using Web.Components.Features.Categories.Handlers;
using Web.Components.Features.Categories.Queries;
using Web.Data;

namespace Web.Tests.Features.Categories;

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
	public async Task DeleteCategoryCommandDeletesExistingCategoryAsync()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var handler = new CategoryFeatureHandler(repository);
		var created = await repository.AddAsync(Category.Create("Archive", "To remove"), cancellationToken);

		// Act
		var result = await handler.Handle(new DeleteCategoryCommand(created.Id.ToString()), cancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		(await repository.GetByIdAsync(created.Id, cancellationToken)).Should().BeNull();
	}

	private static ArticlesMongoDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new ArticlesMongoDbContext(options);
	}
}
