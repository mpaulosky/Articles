// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleFeatureHandlerTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

using Web.Components.Features.Articles.Commands;
using Web.Components.Features.Articles.Queries;
using Web.Components.Features.Categories.Models;
using Web.Integration.Tests.Fixtures;
using Web.TestData;

namespace Web.Integration.Tests.Features.Articles.Handlers;

/// <summary>
///     Exercises <c>ArticleFeatureHandler</c> through the real mediator pipeline (as wired in
///     <c>Program.cs</c>: <c>AddMyMediator</c> plus <c>LoggingBehavior</c>) against a real MongoDB
///     container, instead of calling the handler directly. Business-logic edge cases (NotFound,
///     idempotency, category swaps, etc.) are already covered at the unit level in
///     <c>Web.Tests</c>; these tests confirm the pipeline wiring itself works for every request
///     type the handler serves.
/// </summary>
[Collection(MongoTestCollectionDefinition.Name)]
public class ArticleFeatureHandlerTests
{
	private readonly MongoContainerFixture _fixture;

	public ArticleFeatureHandlerTests(MongoContainerFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task SendAsync_CreateArticleCommand_PersistsArticleAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var command = ArticleTestData.CreateCommand(
			title: "First Article",
			slug: "first-article",
			category: CategoryDto.Empty);

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Title.Should().Be("First Article");
	}

	[Fact]
	public async Task SendAsync_CreateArticleCommand_ReturnsValidationFailure_WhenTitleIsEmptyAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var command = ArticleTestData.CreateCommand(
			title: string.Empty,
			slug: "first-article",
			category: CategoryDto.Empty);

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task SendAsync_GetArticlesQuery_ReturnsSeededArticlesAsync()
	{
		// Arrange
		await using var host = CreateHost();
		await host.Mediator.Send(CreateCommand("first-article"), TestContext.Current.CancellationToken);
		await host.Mediator.Send(CreateCommand("second-article"), TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(new GetArticlesQuery(), TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
	}

	[Fact]
	public async Task SendAsync_GetArticleByIdQuery_ReturnsArticle_WhenFoundAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(CreateCommand("first-article"), TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(
			new GetArticleByIdQuery(created.Value!.Id),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Id.Should().Be(created.Value.Id);
	}

	[Fact]
	public async Task SendAsync_GetArticleByIdQuery_ReturnsNotFound_WhenMissingAsync()
	{
		// Arrange
		await using var host = CreateHost();

		// Act
		var result = await host.Mediator.Send(
			new GetArticleByIdQuery(ObjectId.GenerateNewId().ToString()),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task SendAsync_GetArticleBySlugQuery_ReturnsArticle_WhenFoundAsync()
	{
		// Arrange
		await using var host = CreateHost();
		await host.Mediator.Send(CreateCommand("first-article"), TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(
			new GetArticleBySlugQuery("first-article"),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Slug.Should().Be("first-article");
	}

	[Fact]
	public async Task SendAsync_UpdateArticleCommand_PersistsChangesAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(CreateCommand("first-article"), TestContext.Current.CancellationToken);
		var command = ArticleTestData.UpdateCommand(created.Value!.Id, title: "Updated Title", slug: "first-article", content: "Updated content.");

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Title.Should().Be("Updated Title");
	}

	[Fact]
	public async Task SendAsync_UpdateArticleCommand_ReturnsValidationFailure_WhenTitleIsEmptyAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(CreateCommand("first-article"), TestContext.Current.CancellationToken);
		var command = ArticleTestData.UpdateCommand(created.Value!.Id, title: string.Empty, slug: "first-article", content: "Updated content.");

		// Act
		var result = await host.Mediator.Send(command, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task SendAsync_DeleteArticleCommand_RemovesArticleAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(CreateCommand("first-article"), TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(
			new DeleteArticleCommand(created.Value!.Id),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		var lookup = await host.Mediator.Send(
			new GetArticleByIdQuery(created.Value.Id),
			TestContext.Current.CancellationToken);
		lookup.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task SendAsync_PublishArticleCommand_SetsPublishedOnAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(CreateCommand("first-article"), TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(
			new PublishArticleCommand(created.Value!.Id),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsPublished.Should().BeTrue();
		result.Value.PublishedOn.Should().NotBeNull();
	}

	[Fact]
	public async Task SendAsync_UnpublishArticleCommand_ClearsPublishedOnAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(CreateCommand("first-article"), TestContext.Current.CancellationToken);
		await host.Mediator.Send(new PublishArticleCommand(created.Value!.Id), TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(
			new UnpublishArticleCommand(created.Value.Id),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsPublished.Should().BeFalse();
		result.Value.PublishedOn.Should().BeNull();
	}

	[Fact]
	public async Task SendAsync_ArchiveArticleCommand_SetsIsArchivedAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(CreateCommand("first-article"), TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(
			new ArchiveArticleCommand(created.Value!.Id),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeTrue();
	}

	[Fact]
	public async Task SendAsync_UnarchiveArticleCommand_ClearsIsArchivedAgainstTheRealMongoContainerAsync()
	{
		// Arrange
		await using var host = CreateHost();
		var created = await host.Mediator.Send(CreateCommand("first-article"), TestContext.Current.CancellationToken);
		await host.Mediator.Send(new ArchiveArticleCommand(created.Value!.Id), TestContext.Current.CancellationToken);

		// Act
		var result = await host.Mediator.Send(
			new UnarchiveArticleCommand(created.Value.Id),
			TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsArchived.Should().BeFalse();
	}

	private static CreateArticleCommand CreateCommand(string slug)
	{
		return ArticleTestData.CreateCommand(title: "First Article", slug: slug, category: CategoryDto.Empty);
	}

	private MediatorTestHost CreateHost()
	{
		return MediatorTestHost.Create(_fixture, $"{nameof(ArticleFeatureHandlerTests)}-{Guid.NewGuid()}");
	}
}
