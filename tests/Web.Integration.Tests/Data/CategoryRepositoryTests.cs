// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryRepositoryTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

using Web.Components.Features.Categories.Entities;
using Web.Integration.Tests.Fixtures;

namespace Web.Integration.Tests.Data;

[Collection(MongoTestCollectionDefinition.Name)]
public class CategoryRepositoryTests
{
	private readonly MongoContainerFixture _fixture;

	public CategoryRepositoryTests(MongoContainerFixture fixture)
	{
		_fixture = fixture;
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
	public async Task AddAsync_PersistsCategory_AgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var category = Category.Create("Name", "Description");

		// Act
		await repository.AddAsync(category, TestContext.Current.CancellationToken);

		// Assert
		var stored = await repository.GetByIdAsync(category.Id, TestContext.Current.CancellationToken);
		stored.Should().NotBeNull();
		stored!.Name.Should().Be("Name");
	}

	[Fact]
	public async Task GetAllAsync_ReturnsCategoriesOrderedByNameAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var category1 = Category.Create("Second", "Description 1");
		var category2 = Category.Create("First", "Description 2");

		await repository.AddAsync(category1, TestContext.Current.CancellationToken);
		await repository.AddAsync(category2, TestContext.Current.CancellationToken);

		// Act
		var categories = await repository.GetAllAsync(TestContext.Current.CancellationToken);

		// Assert
		categories.Should().HaveCount(2);
		categories.Select(c => c.Name).Should().ContainInOrder("First", "Second");
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsCategory_WhenFoundAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);
		var category = Category.Create("Name", "Description");
		await repository.AddAsync(category, TestContext.Current.CancellationToken);

		// Act
		var found = await repository.GetByIdAsync(category.Id, TestContext.Current.CancellationToken);

		// Assert
		found.Should().NotBeNull();
		found!.Id.Should().Be(category.Id);
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsNull_WhenNotFoundAsync()
	{
		// Arrange
		await using var context = CreateContext();
		var repository = new CategoryRepository(context);

		// Act
		var found = await repository.GetByIdAsync(ObjectId.GenerateNewId(), TestContext.Current.CancellationToken);

		// Assert
		found.Should().BeNull();
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
	public async Task UpdateAsync_PersistsChanges_AgainstTheRealMongoContainerAsync()
	{
		// Arrange
		var databaseName = $"{nameof(CategoryRepositoryTests)}-{Guid.NewGuid()}";
		await using var writeContext = _fixture.CreateContext(databaseName);
		var repository = new CategoryRepository(writeContext);
		var category = Category.Create("Name", "Description");
		await repository.AddAsync(category, TestContext.Current.CancellationToken);

		category.Update("Name", "Updated Description");

		// Act
		await repository.UpdateAsync(category, TestContext.Current.CancellationToken);

		// Assert
		var stored = await repository.GetByIdAsync(category.Id, TestContext.Current.CancellationToken);
		stored.Should().NotBeNull();
		stored!.Description.Should().Be("Updated Description");
	}

	[Fact]
	public async Task UpdateAsync_HandlesDetachedEntity_WhenFetchedFromADifferentContextAsync()
	{
		// Arrange
		var databaseName = $"{nameof(CategoryRepositoryTests)}-{Guid.NewGuid()}";
		await using var seedContext = _fixture.CreateContext(databaseName);
		var seedRepository = new CategoryRepository(seedContext);
		var category = Category.Create("Name", "Description");
		await seedRepository.AddAsync(category, TestContext.Current.CancellationToken);

		await using var readContext = _fixture.CreateContext(databaseName);
		var readRepository = new CategoryRepository(readContext);
		var detached = await readRepository.GetByIdAsync(category.Id, TestContext.Current.CancellationToken);
		detached!.Update("Name", "Detached Update");

		await using var updateContext = _fixture.CreateContext(databaseName);
		var updateRepository = new CategoryRepository(updateContext);

		// Act
		var act = () => updateRepository.UpdateAsync(detached, TestContext.Current.CancellationToken);

		// Assert
		await act.Should().NotThrowAsync();
		var stored = await updateRepository.GetByIdAsync(category.Id, TestContext.Current.CancellationToken);
		stored!.Description.Should().Be("Detached Update");
	}

	private ArticlesMongoDbContext CreateContext()
	{
		return _fixture.CreateContext($"{nameof(CategoryRepositoryTests)}-{Guid.NewGuid()}");
	}
}
