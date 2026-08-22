using Bunit;

using Domain.Abstractions;

using FluentAssertions;

using MongoDB.Bson;

using Web.MyMediator;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using System.Security.Claims;

using Web.Components.Features.Articles.Commands;
using Web.Components.Features.Articles.Models;
using Web.Components.Features.Articles.Pages;
using Web.Components.Features.Articles.Queries;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Models;
using Web.Components.Features.Categories.Queries;

namespace Web.UI.Tests.Features.Articles.Pages;

public class ArticleEditPageTests : BunitContext
{
	private readonly IMediator _mediator;
	private readonly AuthenticationStateProvider _authStateProvider;
	private readonly CategoryDto _category;

	public ArticleEditPageTests()
	{
		_mediator = Substitute.For<IMediator>();
		_authStateProvider = Substitute.For<AuthenticationStateProvider>();
		_category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(), CategoryName = "Test Category", Slug = "test-category",
			Description = "Test description"
		};

		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<CategoryDto>>(new List<CategoryDto> { _category }));

		Services.AddSingleton(_mediator);
		Services.AddSingleton(_authStateProvider);
	}

	[Fact]
	public void PrefillsForm_WhenAuthorOwnsArticle()
	{
		// Arrange
		var article = CreateArticle("Original Title", "author1", "Original content");
		SetupAuthState(CreateAuthorUser("author1"));
		SetupArticle(article);

		// Act
		var cut = Render<ArticleEditPage>(parameters => parameters.Add(p => p.Slug, article.Slug));

		// Assert
		cut.Find("input.rounded-lg").GetAttribute("value").Should().Be("Original Title");
		cut.Find("textarea.rounded-lg").GetAttribute("value").Should().Be("Original content");
	}

	[Fact]
	public void PrefillsForm_WhenUserIsAdmin()
	{
		// Arrange
		var article = CreateArticle("Original Title", "author1", "Original content");
		SetupAuthState(CreateAdminUser());
		SetupArticle(article);

		// Act
		var cut = Render<ArticleEditPage>(parameters => parameters.Add(p => p.Slug, article.Slug));

		// Assert
		cut.Find("input.rounded-lg").GetAttribute("value").Should().Be("Original Title");
	}

	[Fact]
	public void DisplaysPermissionMessage_WhenAuthorDoesNotOwnArticle()
	{
		// Arrange
		var article = CreateArticle("Original Title", "author1", "Original content");
		SetupAuthState(CreateAuthorUser("author2"));
		SetupArticle(article);

		// Act
		var cut = Render<ArticleEditPage>(parameters => parameters.Add(p => p.Slug, article.Slug));

		// Assert
		cut.Markup.Should().Contain("don't have permission");
		cut.Markup.Should().NotContain("Original Title");
	}

	[Fact]
	public void DisplaysErrorMessage_WhenArticleDoesNotExist()
	{
		// Arrange
		SetupAuthState(CreateAdminUser());
		_mediator.Send(Arg.Any<GetArticleBySlugQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<ArticleDto>("Article not found.", ResultErrorCode.NotFound));

		// Act
		var cut = Render<ArticleEditPage>(parameters => parameters.Add(p => p.Slug, "missing-slug"));

		// Assert
		cut.Markup.Should().Contain("Article not found.");
	}

	[Fact]
	public void DisplaysLoadingMessage_Initially()
	{
		// Arrange
		SetupAuthState(CreateAdminUser());
		var tcs = new TaskCompletionSource<Result<ArticleDto>>();
		_mediator.Send(Arg.Any<GetArticleBySlugQuery>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<ArticleEditPage>(parameters => parameters.Add(p => p.Slug, "some-slug"));

		// Assert
		cut.Markup.Should().Contain("Loading article...");

		// Complete the async operation
		tcs.SetResult(Result.Fail<ArticleDto>("Article not found.", ResultErrorCode.NotFound));
	}

	[Fact]
	public void SubmittingChanges_SendsUpdateArticleCommand_AndNavigatesToViewPage()
	{
		// Arrange
		var article = CreateArticle("Original Title", "author1", "Original content");
		SetupAuthState(CreateAuthorUser("author1"));
		SetupArticle(article);
		_mediator.Send(Arg.Any<UpdateArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(article));

		var cut = Render<ArticleEditPage>(parameters => parameters.Add(p => p.Slug, article.Slug));
		cut.Find("input.rounded-lg").Change("Updated Title");
		cut.Find("textarea.rounded-lg").Change("Updated content");

		// Act
		cut.Find("form").Submit();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<UpdateArticleCommand>(command =>
				command.Id == article.Id
				&& command.Title == "Updated Title"
				&& command.Content == "Updated content"),
			Arg.Any<CancellationToken>());

		var navigation = Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();
		navigation.Uri.Should().EndWith($"/articles/{article.Slug}");
	}

	[Fact]
	public void KeepsEnteredValues_WhenUpdateFails()
	{
		// Arrange
		var article = CreateArticle("Original Title", "author1", "Original content");
		SetupAuthState(CreateAuthorUser("author1"));
		SetupArticle(article);
		_mediator.Send(Arg.Any<UpdateArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<ArticleDto>("Title is too short."));

		var cut = Render<ArticleEditPage>(parameters => parameters.Add(p => p.Slug, article.Slug));
		cut.Find("input.rounded-lg").Change("Up");

		// Act
		cut.Find("form").Submit();

		// Assert
		cut.Markup.Should().Contain("Title is too short.");
		cut.Find("input.rounded-lg").GetAttribute("value").Should().Be("Up");
	}

	// Helper methods

	private void SetupAuthState(ClaimsPrincipal user)
	{
		var authState = new AuthenticationState(user);
		_authStateProvider.GetAuthenticationStateAsync()
			.Returns(Task.FromResult(authState));
	}

	private void SetupArticle(ArticleDto article)
	{
		_mediator.Send(Arg.Any<GetArticleBySlugQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(article));
	}

	private static ClaimsPrincipal CreateAdminUser()
	{
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "admin1"), new Claim(ClaimTypes.Name, "Admin User"),
			new Claim(ClaimTypes.Role, "Admin")
		};
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
	}

	private static ClaimsPrincipal CreateAuthorUser(string userId)
	{
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Name, "Author User"),
			new Claim(ClaimTypes.Role, "Author")
		};
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
	}

	private ArticleDto CreateArticle(string title, string authorId, string content)
	{
		return new ArticleDto(
			Id: Guid.NewGuid().ToString(),
			Title: title,
			Slug: "test-slug",
			Content: content,
			Author: new AuthorDto(authorId, "Test Author"),
			Category: _category,
			CreatedAt: DateTime.UtcNow,
			UpdatedAt: DateTime.UtcNow,
			IsPublished: true,
			PublishedOn: DateTime.UtcNow,
			IsArchived: false
		);
	}
}
