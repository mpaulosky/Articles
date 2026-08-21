using Bunit;

using Domain.Abstractions;

using FluentAssertions;

using Web.MyMediator;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using System.Security.Claims;

using Web.Components.Features.Articles.Models;
using Web.Components.Features.Articles.Pages;
using Web.Components.Features.Articles.Queries;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Models;

namespace Web.UI.Tests.Features.Articles.Pages;

public class ArticleViewPageTests : BunitContext
{
	private readonly IMediator _mediator;
	private readonly AuthenticationStateProvider _authStateProvider;

	public ArticleViewPageTests()
	{
		_mediator = Substitute.For<IMediator>();
		_authStateProvider = Substitute.For<AuthenticationStateProvider>();

		Services.AddSingleton(_mediator);
		Services.AddSingleton(_authStateProvider);
	}

	[Fact]
	public void RendersArticleDetails_WhenUserCanViewArticle()
	{
		// Arrange
		var article = CreateArticle("Test Article", "author1", isPublished: true);
		SetupAuthState(CreateAdminUser());
		SetupArticle(article);

		// Act
		var cut = Render<ArticleViewPage>(parameters => parameters.Add(p => p.Id, article.Id));

		// Assert
		cut.Markup.Should().Contain("Test Article");
		cut.Markup.Should().Contain("Test content");
		cut.Markup.Should().Contain("Test Author");
		cut.Markup.Should().Contain("Test Category");
		cut.Markup.Should().Contain("Published");
	}

	[Fact]
	public void ShowsDraftStatus_WhenArticleIsNotPublished()
	{
		// Arrange
		var article = CreateArticle("Draft Article", "author1", isPublished: false);
		SetupAuthState(CreateAdminUser());
		SetupArticle(article);

		// Act
		var cut = Render<ArticleViewPage>(parameters => parameters.Add(p => p.Id, article.Id));

		// Assert
		cut.Markup.Should().Contain("Draft Article");
		cut.Markup.Should().Contain("Draft");
	}

	[Fact]
	public void ShowsArchivedIndicator_WhenArticleIsArchived()
	{
		// Arrange
		var article = CreateArticle("Archived Article", "author1", isPublished: true, isArchived: true);
		SetupAuthState(CreateAdminUser());
		SetupArticle(article);

		// Act
		var cut = Render<ArticleViewPage>(parameters => parameters.Add(p => p.Id, article.Id));

		// Assert
		cut.Markup.Should().Contain("Archived");
	}

	[Fact]
	public void DoesNotShowArchivedIndicator_WhenArticleIsNotArchived()
	{
		// Arrange
		var article = CreateArticle("Live Article", "author1", isPublished: true, isArchived: false);
		SetupAuthState(CreateAdminUser());
		SetupArticle(article);

		// Act
		var cut = Render<ArticleViewPage>(parameters => parameters.Add(p => p.Id, article.Id));

		// Assert
		cut.Markup.Should().NotContain(">Archived<");
	}

	[Fact]
	public void DisplaysErrorMessage_WhenArticleDoesNotExist()
	{
		// Arrange
		SetupAuthState(CreateAdminUser());
		_mediator.Send(Arg.Any<GetArticleByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<ArticleDto>("Article not found.", ResultErrorCode.NotFound));

		// Act
		var cut = Render<ArticleViewPage>(parameters => parameters.Add(p => p.Id, "missing-id"));

		// Assert
		cut.Markup.Should().Contain("Article not found.");
	}

	[Fact]
	public void DisplaysPermissionMessage_WhenUserCannotViewArticle()
	{
		// Arrange
		var article = CreateArticle("Draft Article", "author1", isPublished: false);
		SetupAuthState(CreateAnonymousUser());
		SetupArticle(article);

		// Act
		var cut = Render<ArticleViewPage>(parameters => parameters.Add(p => p.Id, article.Id));

		// Assert
		cut.Markup.Should().Contain("don't have permission");
		cut.Markup.Should().NotContain("Draft Article");
	}

	[Fact]
	public void DisplaysLoadingMessage_Initially()
	{
		// Arrange
		SetupAuthState(CreateAnonymousUser());
		var tcs = new TaskCompletionSource<Result<ArticleDto>>();
		_mediator.Send(Arg.Any<GetArticleByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<ArticleViewPage>(parameters => parameters.Add(p => p.Id, "some-id"));

		// Assert
		cut.Markup.Should().Contain("Loading article...");

		// Complete the async operation
		tcs.SetResult(Result.Fail<ArticleDto>("Article not found.", ResultErrorCode.NotFound));
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
		_mediator.Send(Arg.Any<GetArticleByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(article));
	}

	private static ClaimsPrincipal CreateAnonymousUser()
	{
		return new ClaimsPrincipal(new ClaimsIdentity());
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

	private static ArticleDto CreateArticle(string title, string authorId, bool isPublished,
		string content = "Test content", bool isArchived = false)
	{
		return new ArticleDto(
			Id: Guid.NewGuid().ToString(),
			Title: title,
			Slug: "test-slug",
			Content: content,
			Author: new AuthorDto(authorId, "Test Author"),
			Category: new CategoryDto
			{
				CategoryName = "Test Category", Slug = "test-category", Description = "Test description"
			},
			CreatedAt: DateTime.UtcNow,
			UpdatedAt: DateTime.UtcNow,
			IsPublished: isPublished,
			PublishedOn: isPublished ? DateTime.UtcNow : null,
			IsArchived: isArchived
		);
	}
}
