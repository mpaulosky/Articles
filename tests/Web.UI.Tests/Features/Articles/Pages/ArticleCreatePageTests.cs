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
using Web.Components.Features.Articles.Components;
using Web.Components.Features.Articles.Services;
using Web.Components.Features.Categories.Models;
using Web.Components.Features.Categories.Queries;

namespace Web.UI.Tests.Features.Articles.Pages;

public class ArticleCreatePageTests : BunitContext
{
	private readonly IMediator _mediator;
	private readonly AuthenticationStateProvider _authStateProvider;
	private readonly CategoryDto _category;

	public ArticleCreatePageTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;

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
		Services.AddSingleton(Substitute.For<IFileStorage>());
	}

	[Fact]
	public void RendersForm_WhenUserCanCreateArticles()
	{
		// Arrange
		SetupAuthState(CreateAdminUser());

		// Act
		var cut = Render<ArticleCreatePage>();

		// Assert
		cut.Find("form").Should().NotBeNull();
	}

	[Fact]
	public void DisplaysPermissionMessage_WhenUserCannotCreateArticles()
	{
		// Arrange
		SetupAuthState(CreateAnonymousUser());

		// Act
		var cut = Render<ArticleCreatePage>();

		// Assert
		cut.Markup.Should().Contain("don't have permission");
		cut.FindAll("form").Should().BeEmpty();
	}

	[Fact]
	public void SubmittingForm_SendsCreateArticleCommand_WithAuthorFromLoggedInUser_AndNavigatesToViewPage()
	{
		// Arrange
		var user = CreateAdminUser();
		SetupAuthState(user);
		var created = CreateArticle("New Article", "admin1", "New article content");
		_mediator.Send(Arg.Any<CreateArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(created));

		var cut = Render<ArticleCreatePage>();
		cut.Find("input.rounded-lg").Change("New Article");
		cut.InvokeAsync(() => cut.FindComponent<TextEditor>().Instance.MyContent = "New article content");
		cut.Find("select.rounded-lg").Change(_category.Id.ToString());

		// Act
		cut.Find("form").Submit();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<CreateArticleCommand>(command =>
				command.Author.UserId == "admin1"
				&& command.Author.Name == "Admin User"
				&& command.Content == "New article content"),
			Arg.Any<CancellationToken>());

		var navigation = Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();
		navigation.Uri.Should().EndWith($"/articles/{created.Slug}");
	}

	[Fact]
	public void ClickingCancel_NavigatesToArticlesPage()
	{
		// Arrange
		SetupAuthState(CreateAdminUser());
		var cut = Render<ArticleCreatePage>();
		var navigation = Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel").Click();

		// Assert
		navigation.Uri.Should().EndWith("/articles");
	}

	[Fact]
	public void SubmittingForm_KeepsEnteredValues_WhenSaveFails()
	{
		// Arrange
		SetupAuthState(CreateAdminUser());
		_mediator.Send(Arg.Any<CreateArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<ArticleDto>("Title is too short."));

		var cut = Render<ArticleCreatePage>();
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

	private static ClaimsPrincipal CreateAdminUser()
	{
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "admin1"), new Claim(ClaimTypes.Name, "Admin User"),
			new Claim(ClaimTypes.Role, "Admin")
		};
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
	}

	private static ClaimsPrincipal CreateAnonymousUser()
	{
		return new ClaimsPrincipal(new ClaimsIdentity());
	}

	private ArticleDto CreateArticle(string title, string authorId, string content)
	{
		return new ArticleDto(
			Id: Guid.NewGuid().ToString(),
			Title: title,
			Slug: "new-article",
			Content: content,
			Author: new Web.Components.Features.AuthInfo.Entities.AuthorDto(authorId, "Admin User"),
			Category: _category,
			CreatedAt: DateTime.UtcNow,
			UpdatedAt: null,
			IsPublished: false,
			PublishedOn: null,
			IsArchived: false
		);
	}
}
