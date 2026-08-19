using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using MongoDB.Bson;

using Web.Components.Features.Articles.Commands;
using Web.Components.Features.Articles.Handlers;
using Web.Components.Features.Articles.Queries;
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
			"Body one",
			new AuthorDto("user-10", "First Author", "one@example.com"),
			new CategoryDto
			{
				Id = ObjectId.GenerateNewId(), CategoryName = "General", Slug = "general", CreatedOn = DateTime.UtcNow
			});
		var second = new CreateArticleCommand(
			"Second article",
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

	private static ArticlesMongoDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new ArticlesMongoDbContext(options);
	}
}
