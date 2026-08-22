using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Web.Components.Features.Categories.Entities;
using Web.Data;

namespace Web.Tests.Features.Categories;

public class CategoryRepositoryTests
{
	[Fact]
	public async Task AddAsync_PersistsCategoryAndReturnsTrackedEntityAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var category = Category.Create("Technology", "Technology description");

		// Act
		var result = await repository.AddAsync(category, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeSameAs(category);
		var persisted = await repository.GetByIdAsync(category.Id, TestContext.Current.CancellationToken);
		persisted.Should().NotBeNull();
		persisted!.Name.Should().Be("Technology");
		persisted.Description.Should().Be("Technology description");
	}

	[Fact]
	public async Task UpdateAsync_UpdatesTrackedEntityWithoutThrowingAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var created = await repository.AddAsync(Category.Create("News", "Old description"),
			TestContext.Current.CancellationToken);
		created.Update("Daily News", "Updated description");

		// Act
		var result = await repository.UpdateAsync(created, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeSameAs(created);
		var updated = await repository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);
		updated.Should().NotBeNull();
		updated!.Name.Should().Be("Daily News");
		updated.Description.Should().Be("Updated description");
		updated.Slug.Should().Be("daily-news");
	}

	[Fact]
	public void Constructor_ThrowsArgumentNullException_WhenContextIsNull()
	{
		// Act
		var act = () => new CategoryRepository(null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task AddAsync_ThrowsArgumentNullException_WhenCategoryIsNullAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);

		// Act
		var act = () => repository.AddAsync(null!, TestContext.Current.CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task UpdateAsync_ThrowsArgumentNullException_WhenCategoryIsNullAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);

		// Act
		var act = () => repository.UpdateAsync(null!, TestContext.Current.CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task UpdateAsync_PersistsArchivedStateAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var created = await repository.AddAsync(Category.Create("Technology", "Technology description"),
			TestContext.Current.CancellationToken);
		created.Archive();

		// Act
		await repository.UpdateAsync(created, TestContext.Current.CancellationToken);

		// Assert
		var updated = await repository.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);
		updated!.IsArchived.Should().BeTrue();
	}

	private static ArticlesMongoDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new ArticlesMongoDbContext(options);
	}
}
