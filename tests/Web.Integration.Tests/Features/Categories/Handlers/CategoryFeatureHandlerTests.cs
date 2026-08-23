// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryFeatureHandlerTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

using Web.Components.Features.Categories.Commands;
using Web.Components.Features.Categories.Queries;
using Web.Integration.Tests.Fixtures;

namespace Web.Integration.Tests.Features.Categories.Handlers;

/// <summary>
///     Exercises <c>CategoryFeatureHandler</c> through the real mediator pipeline (as wired in
///     <c>Program.cs</c>: <c>AddMyMediator</c> plus <c>LoggingBehavior</c>) against a real MongoDB
///     container, instead of calling the handler directly. Business-logic edge cases are already
///     covered at the unit level in <c>Web.Tests</c>; these tests confirm the pipeline wiring itself
///     works for every request type the handler serves.
/// </summary>
[Collection(MongoTestCollectionDefinition.Name)]
public class CategoryFeatureHandlerTests
{
	private readonly MongoContainerFixture _fixture;

	public CategoryFeatureHandlerTests(MongoContainerFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task SendAsync_CreateCategoryCommand_PersistsCategoryAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var command = new CreateCategoryCommand("First Category", "This is the category description.");

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.CategoryName.Should().Be("First Category");
	}

	[Fact]
	public async Task SendAsync_CreateCategoryCommand_ReturnsValidationFailure_WhenNameIsEmptyAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var command = new CreateCategoryCommand(string.Empty, "This is the category description.");

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task SendAsync_GetCategoriesQuery_ReturnsSeededCategoriesAsync()
	{
		// Arrange
		await using var host = CreateHost();
		await host.Mediator.Send(CreateCommand("First Category"), TestContext.Current.CancellationToken);
		await host.Mediator.Send(CreateCommand("Second Category"), TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(new GetCategoriesQuery(), TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
	}

	[Fact]
	public async Task SendAsync_GetCategoryByIdQuery_ReturnsCategory_WhenFoundAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(
			CreateCommand("First Category"),
			TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(
			new GetCategoryByIdQuery(created.Value!.Id.ToString()),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Id.Should().Be(created.Value.Id);
	}

	[Fact]
	public async Task SendAsync_GetCategoryByIdQuery_ReturnsNotFound_WhenMissingAsync()
	{
		// Arrange
		await using var host = CreateHost();

		// Act
		var result = await host.Mediator.Send(
			new GetCategoryByIdQuery(ObjectId.GenerateNewId().ToString()),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task SendAsync_UpdateCategoryCommand_PersistsChangesAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(
			CreateCommand("First Category"),
			TestContext.Current.CancellationToken);
		var command = new UpdateCategoryCommand(
			created.Value!.Id.ToString(),
			"Updated Category",
			"Updated description.");

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.CategoryName.Should().Be("Updated Category");
	}

	[Fact]
	public async Task SendAsync_UpdateCategoryCommand_ReturnsValidationFailure_WhenNameIsEmptyAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(
			CreateCommand("First Category"),
			TestContext.Current.CancellationToken);
		var command = new UpdateCategoryCommand(created.Value!.Id.ToString(), string.Empty, "Updated description.");

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task SendAsync_UpdateCategoryCommand_ReturnsNotFound_WhenMissingAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var command = new UpdateCategoryCommand(
			ObjectId.GenerateNewId().ToString(),
			"Updated Category",
			"Updated description.");

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task SendAsync_ArchiveCategoryCommand_SetsIsArchivedAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(
			CreateCommand("First Category"),
			TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(
			new ArchiveCategoryCommand(created.Value!.Id.ToString()),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeTrue();
	}

	[Fact]
	public async Task SendAsync_UnarchiveCategoryCommand_ClearsIsArchivedAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(
			CreateCommand("First Category"),
			TestContext.Current.CancellationToken);
		await host.Mediator.Send(
			new ArchiveCategoryCommand(created.Value!.Id.ToString()),
			TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(
			new UnarchiveCategoryCommand(created.Value.Id.ToString()),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeFalse();
	}

	private static CreateCategoryCommand CreateCommand(string name)
	{
		return new CreateCategoryCommand(name, "This is the category description.");
	}

	private MediatorTestHost CreateHost()
	{
		// MongoDB caps database names at 63 characters; the full class name plus a GUID overruns
		// that, so this ticket uses a shorter prefix than the class name.
		return MediatorTestHost.Create(_fixture, $"CategoryHandlerTests-{Guid.NewGuid()}");
	}
}
