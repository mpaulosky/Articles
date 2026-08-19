using Bunit;

using Domain.Abstractions;

using FluentAssertions;

using MyMediator.Interfaces;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using System.Security.Claims;

using Web.Components.Features.Articles.Authorization;
using Web.Components.Features.Articles.Commands;
using Web.Components.Features.Articles.Models;
using Web.Components.Features.Articles.Pages;
using Web.Components.Features.Articles.Queries;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Models;

namespace Web.UI.Tests.Features.Articles.Pages;

public class ArticlesPageTests : BunitContext
{
	private readonly IMediator _mediator;
	private readonly AuthenticationStateProvider _authStateProvider;

	public ArticlesPageTests()
	{
		_mediator = Substitute.For<IMediator>();
		_authStateProvider = Substitute.For<AuthenticationStateProvider>();

		Services.AddSingleton(_mediator);
		Services.AddSingleton(_authStateProvider);
	}

	[Fact]
	public void RendersWithoutErrors_WhenNoArticles()
	{
		// Arrange
		SetupAuthState(CreateAnonymousUser());
		SetupEmptyArticles();

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.Markup.Should().NotBeNullOrEmpty();
		cut.Markup.Should().Contain("No published articles yet");
	}

	[Fact]
	public void RendersArticlesList_WithValidHtmlStructure()
	{
		// Arrange
		var articles = CreateTestArticles();
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles, user);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert - This would catch tag mismatch errors
		cut.Markup.Should().NotBeNullOrEmpty();

		// Verify all opening tags have matching closing tags
		var openDivs = CountOccurrences(cut.Markup, "<div");
		var closeDivs = CountOccurrences(cut.Markup, "</div>");
		openDivs.Should().Be(closeDivs, "all <div> tags must have matching closing tags");

		var openSections = CountOccurrences(cut.Markup, "<section");
		var closeSections = CountOccurrences(cut.Markup, "</section>");
		openSections.Should().Be(closeSections, "all <section> tags must have matching closing tags");
	}

	[Fact]
	public void DisplaysArticles_WhenUserIsAdmin()
	{
		// Arrange
		var articles = CreateTestArticles();
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles, user);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.Markup.Should().Contain("Test Article 1");
		cut.Markup.Should().Contain("Test Article 2");
		cut.Markup.Should().Contain("By Test Author");
	}

	[Fact]
	public void ShowsDeleteButton_WhenUserCanEdit()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("Test Article 1", "admin1", isPublished: true) };
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles, user);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		// Admin can edit any article, so delete button should be visible
		cut.Markup.Should().Contain("Delete");
		cut.Markup.Should().Contain("button");
	}

	[Fact]
	public void HidesDeleteButton_WhenUserCannotEdit()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("Test Article 1", "author1", isPublished: true) };
		var user = CreateAnonymousUser();
		SetupAuthState(user);
		SetupArticles(articles, user);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		// Anonymous users cannot edit, so no delete buttons
		var deleteButtons = CountOccurrences(cut.Markup, ">Delete</button>");
		deleteButtons.Should().Be(0, "anonymous users should not see delete buttons");
	}

	[Fact]
	public void FiltersArticles_BasedOnAuthorizationRules()
	{
		// Arrange
		var allArticles = new List<ArticleDto>
		{
			CreateArticle("Published Article", "author1", isPublished: true),
			CreateArticle("Draft Article", "author2", isPublished: false)
		};

		// Regular authenticated user (not Admin, not Author)
		var user = CreateAuthenticatedUser("regularUser");
		SetupAuthState(user);

		// Mock GetArticlesQuery to return all articles
		_mediator.Send(Arg.Any<GetArticlesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<ArticleDto>>(allArticles));

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		// Regular users can only see published articles (ArticleAuthorizationService.CanViewArticle logic)
		cut.Markup.Should().Contain("Published Article");
		cut.Markup.Should().NotContain("Draft Article");
	}

	[Fact]
	public void DisplaysLoadingMessage_Initially()
	{
		// Arrange
		SetupAuthState(CreateAnonymousUser());

		// Delay the mediator response to simulate loading
		var tcs = new TaskCompletionSource<Result<IReadOnlyList<ArticleDto>>>();
		_mediator.Send(Arg.Any<GetArticlesQuery>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.Markup.Should().Contain("Loading articles...");

		// Complete the async operation
		tcs.SetResult(Result.Ok<IReadOnlyList<ArticleDto>>(new List<ArticleDto>()));
	}

	[Fact]
	public void DisplaysArticleContent_InCards()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("Article Title", "author1", isPublished: true, content: "Article content here")
		};

		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles, user);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.Markup.Should().Contain("Article Title");
		cut.Markup.Should().Contain("Article content here");
		cut.Markup.Should().Contain("rounded-xl"); // Tailwind card styling
	}

	[Fact]
	public void ArticleCard_HasCompleteStructure()
	{
		// Arrange
		var articles = CreateTestArticles();
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles, user);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		// Verify card structure elements exist
		cut.Markup.Should().Contain("rounded-xl border"); // Card container
		cut.Markup.Should().Contain("flex items-start justify-between"); // Flex container for title and button
		cut.Markup.Should().Contain("text-lg font-semibold"); // Title styling
		cut.Markup.Should().Contain("text-sm text-slate-600"); // Author styling
	}

	// Helper methods

	private void SetupAuthState(ClaimsPrincipal user)
	{
		var authState = new AuthenticationState(user);
		_authStateProvider.GetAuthenticationStateAsync()
			.Returns(Task.FromResult(authState));
	}

	private void SetupEmptyArticles()
	{
		_mediator.Send(Arg.Any<GetArticlesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<ArticleDto>>(new List<ArticleDto>()));
	}

	private void SetupArticles(List<ArticleDto> articles, ClaimsPrincipal user)
	{
		_mediator.Send(Arg.Any<GetArticlesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<ArticleDto>>(articles));
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

	private static ClaimsPrincipal CreateAuthorUser(string userId)
	{
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Name, "Author User"),
			new Claim(ClaimTypes.Role, "Author")
		};
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
	}

	private static ClaimsPrincipal CreateAuthenticatedUser(string userId)
	{
		var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Name, "Regular User") };
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
	}

	private static List<ArticleDto> CreateTestArticles()
	{
		return
		[
			CreateArticle("Test Article 1", "author1", isPublished: true),
			CreateArticle("Test Article 2", "author1", isPublished: true)
		];
	}

	private static ArticleDto CreateArticle(string title, string authorId, bool isPublished,
		string content = "Test content")
	{
		return new ArticleDto(
			Id: Guid.NewGuid().ToString(),
			Title: title,
			Content: content,
			Author: new AuthorDto(authorId, "Test Author"),
			Category: new CategoryDto
			{
				CategoryName = "Test Category", Slug = "test-category", Description = "Test description"
			},
			CreatedAt: DateTime.UtcNow,
			UpdatedAt: DateTime.UtcNow,
			IsPublished: isPublished
		);
	}

	private static int CountOccurrences(string text, string pattern)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
			return 0;

		int count = 0;
		int index = 0;
		while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
		{
			count++;
			index += pattern.Length;
		}

		return count;
	}
}
