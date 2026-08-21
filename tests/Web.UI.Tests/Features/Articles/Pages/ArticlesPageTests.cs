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

public class ArticlesPageTests : BunitContext
{
	private readonly IMediator _mediator;
	private readonly AuthenticationStateProvider _authStateProvider;
	private readonly CategoryDto _category;
	private readonly CategoryDto _archivedCategory;

	public ArticlesPageTests()
	{
		_mediator = Substitute.For<IMediator>();
		_authStateProvider = Substitute.For<AuthenticationStateProvider>();
		_category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(), CategoryName = "Test Category", Slug = "test-category",
			Description = "Test description"
		};
		_archivedCategory = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(), CategoryName = "Retired Category", Slug = "retired-category",
			Description = "Retired description", IsArchived = true
		};

		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<CategoryDto>>(new List<CategoryDto> { _category, _archivedCategory }));

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
		cut.Markup.Should().Contain("No articles to show yet.");
	}

	[Fact]
	public void RendersHeaderBar_WithHeadingCheckboxAndCreateButton()
	{
		// Arrange
		SetupAuthState(CreateAnonymousUser());
		SetupEmptyArticles();

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.Markup.Should().Contain("All Articles");
		cut.Markup.Should().Contain("Show My Articles Only");
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Create New Article");
	}

	[Fact]
	public void RendersArticlesTable_WithValidHtmlStructure()
	{
		// Arrange
		var articles = CreateTestArticles();
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert - This would catch tag mismatch errors
		cut.Markup.Should().NotBeNullOrEmpty();

		var openDivs = CountOccurrences(cut.Markup, "<div");
		var closeDivs = CountOccurrences(cut.Markup, "</div>");
		openDivs.Should().Be(closeDivs, "all <div> tags must have matching closing tags");

		var openRows = CountOccurrences(cut.Markup, "<tr");
		var closeRows = CountOccurrences(cut.Markup, "</tr>");
		openRows.Should().Be(closeRows, "all <tr> tags must have matching closing tags");
	}

	[Fact]
	public void DisplaysArticles_WhenUserIsAdmin()
	{
		// Arrange
		var articles = CreateTestArticles();
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.Markup.Should().Contain("Test Article 1");
		cut.Markup.Should().Contain("Test Article 2");
		cut.Markup.Should().Contain("Test Author");
	}

	[Fact]
	public void ShowsViewButton_ForEveryVisibleArticle_RegardlessOfEditRights()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("Published Article", "author1", isPublished: true) };
		var user = CreateAuthenticatedUser("regularUser");
		SetupAuthState(user);
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		var viewLink = cut.FindAll("a").FirstOrDefault(a => a.TextContent.Trim() == "View");
		viewLink.Should().NotBeNull();
		viewLink!.GetAttribute("href").Should().Be($"/articles/{articles[0].Id}");
	}

	[Fact]
	public void ShowsEditPublishDeleteButtons_WhenUserCanEdit()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("Test Article 1", "admin1", isPublished: true) };
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();
		var buttonLabels = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
		var linkLabels = cut.FindAll("a").Select(a => a.TextContent.Trim()).ToList();

		// Assert
		// Admin can edit any article
		linkLabels.Should().Contain("Edit");
		buttonLabels.Should().Contain("Unpublish");
		buttonLabels.Should().Contain("Delete");
	}

	[Fact]
	public void HidesEditPublishDeleteButtons_WhenUserCannotEdit()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("Test Article 1", "author1", isPublished: true) };
		var user = CreateAuthenticatedUser("regularUser");
		SetupAuthState(user);
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();
		var buttonLabels = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
		var linkLabels = cut.FindAll("a").Select(a => a.TextContent.Trim()).ToList();

		// Assert
		// The article is visible (published) but this user cannot edit it
		cut.Markup.Should().Contain("Test Article 1");
		linkLabels.Should().NotContain("Edit");
		buttonLabels.Should().NotContain("Publish");
		buttonLabels.Should().NotContain("Unpublish");
		buttonLabels.Should().NotContain("Delete");
	}

	[Fact]
	public void HidesEditPublishDeleteButtons_ForOtherAuthorsArticle_WhenUserIsAuthor()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("Someone Else's Article", "author2", isPublished: true) };
		var user = CreateAuthorUser("author1");
		SetupAuthState(user);
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();
		var buttonLabels = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
		var linkLabels = cut.FindAll("a").Select(a => a.TextContent.Trim()).ToList();

		// Assert
		linkLabels.Should().NotContain("Edit");
		buttonLabels.Should().NotContain("Delete");
	}

	[Fact]
	public void ShowsEditPublishDeleteButtons_ForOwnArticle_WhenUserIsAuthor()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("My Article", "author1", isPublished: false) };
		var user = CreateAuthorUser("author1");
		SetupAuthState(user);
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();
		var buttonLabels = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
		var linkLabels = cut.FindAll("a").Select(a => a.TextContent.Trim()).ToList();

		// Assert
		linkLabels.Should().Contain("Edit");
		buttonLabels.Should().Contain("Publish");
		buttonLabels.Should().Contain("Delete");
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
	public void TogglingShowMyArticlesOnly_NarrowsTableToOwnArticles()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("My Article", "admin1", isPublished: true),
			CreateArticle("Other Article", "author2", isPublished: true)
		};
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();
		cut.Markup.Should().Contain("My Article");
		cut.Markup.Should().Contain("Other Article");

		// Act
		cut.Find("input[type=checkbox]").Change(true);

		// Assert
		cut.Markup.Should().Contain("My Article");
		cut.Markup.Should().NotContain("Other Article");
	}

	[Fact]
	public void DisplaysLoadingMessage_Initially()
	{
		// Arrange
		SetupAuthState(CreateAnonymousUser());

		var tcs = new TaskCompletionSource<Result<IReadOnlyList<ArticleDto>>>();
		_mediator.Send(Arg.Any<GetArticlesQuery>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.Markup.Should().Contain("Loading articles...");

		tcs.SetResult(Result.Ok<IReadOnlyList<ArticleDto>>(new List<ArticleDto>()));
	}

	[Fact]
	public void CreatePanel_IsHiddenByDefault_AndOpensOnButtonClick()
	{
		// Arrange
		SetupAuthState(CreateAdminUser());
		SetupEmptyArticles();
		var cut = Render<ArticlesPage>();

		cut.Markup.Should().NotContain("Create article");

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Create New Article").Click();

		// Assert
		cut.Markup.Should().Contain("Create article");
		cut.Find("form").Should().NotBeNull();
	}

	[Fact]
	public void CreateArticleAsync_SendsCreateArticleCommand_WithAuthorFromLoggedInUser_AndCollapsesPanel()
	{
		// Arrange
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupEmptyArticles();

		_mediator.Send(Arg.Any<CreateArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(CreateArticle("New Article", "admin1", isPublished: false)));

		var cut = Render<ArticlesPage>();
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Create New Article").Click();

		cut.Find("input.rounded-lg").Change("New Article");
		cut.Find("textarea.rounded-lg").Change("New article content");
		cut.Find("select.rounded-lg").Change(_category.Id.ToString());

		// Act
		cut.Find("form").Submit();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<CreateArticleCommand>(command =>
				command.Author.UserId == "admin1"
				&& command.Author.Name == "Admin User"),
			Arg.Any<CancellationToken>());

		cut.Markup.Should().NotContain("Create article");
	}

	[Fact]
	public void CreateArticleAsync_KeepsPanelOpen_WhenSaveFails()
	{
		// Arrange
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupEmptyArticles();

		_mediator.Send(Arg.Any<CreateArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<ArticleDto>("Title is too short."));

		var cut = Render<ArticlesPage>();
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Create New Article").Click();

		cut.Find("input.rounded-lg").Change("Up");
		cut.Find("textarea.rounded-lg").Change("New article content");

		// Act
		cut.Find("form").Submit();

		// Assert
		cut.Markup.Should().Contain("Title is too short.");
		cut.Markup.Should().Contain("Create article");
		cut.Find("input.rounded-lg").GetAttribute("value").Should().Be("Up");
	}

	[Fact]
	public void ClickingPublishButton_SendsPublishArticleCommand()
	{
		// Arrange
		var article = CreateArticle("Draft Article", "admin1", isPublished: false);
		var articles = new List<ArticleDto> { article };
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);
		_mediator.Send(Arg.Any<PublishArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(article));

		var cut = Render<ArticlesPage>();
		var publishButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Publish");

		// Act
		publishButton.Click();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<PublishArticleCommand>(command => command.Id == article.Id),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public void ClickingUnpublishButton_SendsUnpublishArticleCommand()
	{
		// Arrange
		var article = CreateArticle("Live Article", "admin1", isPublished: true);
		var articles = new List<ArticleDto> { article };
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);
		_mediator.Send(Arg.Any<UnpublishArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(article));

		var cut = Render<ArticlesPage>();
		var unpublishButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Unpublish");

		// Act
		unpublishButton.Click();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<UnpublishArticleCommand>(command => command.Id == article.Id),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public void ClickingDelete_RequiresConfirmation_BeforeSendingDeleteArticleCommand()
	{
		// Arrange
		var article = CreateArticle("Test Article", "admin1", isPublished: true);
		var articles = new List<ArticleDto> { article };
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);
		_mediator.Send(Arg.Any<DeleteArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		var cut = Render<ArticlesPage>();

		// Act - first click only requests confirmation
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Delete").Click();

		// Assert
		_mediator.DidNotReceive().Send(Arg.Any<DeleteArticleCommand>(), Arg.Any<CancellationToken>());
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Confirm");

		// Act - confirming sends the command
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Confirm").Click();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<DeleteArticleCommand>(command => command.Id == article.Id),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public void CancellingDeleteConfirmation_DoesNotSendDeleteArticleCommand()
	{
		// Arrange
		var article = CreateArticle("Test Article", "admin1", isPublished: true);
		var articles = new List<ArticleDto> { article };
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Delete").Click();

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel").Click();

		// Assert
		_mediator.DidNotReceive().Send(Arg.Any<DeleteArticleCommand>(), Arg.Any<CancellationToken>());
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Delete");
	}

	[Fact]
	public void SortsByTitle_WhenTitleHeaderIsClicked_TogglesDirection()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("Bravo", "admin1", isPublished: true),
			CreateArticle("Alpha", "admin1", isPublished: true)
		};
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();

		// Assert default ascending sort (Alpha before Bravo)
		var titlesBefore = cut.FindAll("td.font-medium").Select(td => td.TextContent.Trim()).ToList();
		titlesBefore.Should().ContainInOrder("Alpha", "Bravo");

		// Act - click Title header to reverse the sort
		cut.FindAll("th").First(th => th.TextContent.Trim().StartsWith("Title", StringComparison.Ordinal)).Click();

		// Assert descending sort (Bravo before Alpha)
		var titlesAfter = cut.FindAll("td.font-medium").Select(td => td.TextContent.Trim()).ToList();
		titlesAfter.Should().ContainInOrder("Bravo", "Alpha");
	}

	[Fact]
	public void PaginatesAtTenRowsPerPage()
	{
		// Arrange
		var articles = Enumerable.Range(1, 15)
			.Select(i => CreateArticle($"Article {i:00}", "admin1", isPublished: true))
			.ToList();
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();

		// Assert first page shows exactly 10 rows
		cut.FindAll("tbody tr").Should().HaveCount(10);
		cut.Markup.Should().Contain("Page 1 of 2");

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Next").Click();

		// Assert second page shows the remaining 5 rows
		cut.FindAll("tbody tr").Should().HaveCount(5);
		cut.Markup.Should().Contain("Page 2 of 2");
	}

	[Fact]
	public void IncludeArchivedCheckbox_UncheckedByDefault_ExcludesArchivedArticles()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("Active Article", "admin1", isPublished: true),
			CreateArticle("Archived Article", "admin1", isPublished: true, isArchived: true)
		};
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.Markup.Should().Contain("Active Article");
		cut.Markup.Should().NotContain("Archived Article");
	}

	[Fact]
	public void CheckingIncludeArchived_IncludesArchivedArticlesInTable()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("Active Article", "admin1", isPublished: true),
			CreateArticle("Archived Article", "admin1", isPublished: true, isArchived: true)
		};
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();

		// Act
		cut.FindAll("input[type=checkbox]")[1].Change(true);

		// Assert
		cut.Markup.Should().Contain("Active Article");
		cut.Markup.Should().Contain("Archived Article");
	}

	[Fact]
	public void ShowsArchivedBadge_NextToTitle_ForArchivedArticle()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("Retired Article", "admin1", isPublished: true, isArchived: true) };
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();
		cut.FindAll("input[type=checkbox]")[1].Change(true);

		// Act
		var titleCell = cut.FindAll("td.font-medium")
			.First(td => td.TextContent.Contains("Retired Article", StringComparison.Ordinal));

		// Assert
		var badge = titleCell.QuerySelector("span.app-badge");
		badge.Should().NotBeNull();
		badge!.TextContent.Trim().Should().Be("Archived");
	}

	[Fact]
	public void ShowsArchiveAndUnarchiveActions_ForAdmin_OnEveryRow()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("Active Article", "author1", isPublished: true),
			CreateArticle("Archived Article", "author1", isPublished: true, isArchived: true)
		};
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();
		cut.FindAll("input[type=checkbox]")[1].Change(true);

		// Assert
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Archive");
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Unarchive");
	}

	[Fact]
	public void HidesArchiveAndUnarchiveActions_ForNonAdmin_EvenOnOwnArticle()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("My Article", "author1", isPublished: true) };
		SetupAuthState(CreateAuthorUser("author1"));
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.FindAll("button").Should()
			.NotContain(b => b.TextContent.Trim() == "Archive" || b.TextContent.Trim() == "Unarchive");
	}

	[Fact]
	public void ClickingArchiveButton_SendsArchiveArticleCommand()
	{
		// Arrange
		var article = CreateArticle("Test Article", "author1", isPublished: true);
		var articles = new List<ArticleDto> { article };
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);
		_mediator.Send(Arg.Any<ArchiveArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(article));

		var cut = Render<ArticlesPage>();
		var archiveButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Archive");

		// Act
		archiveButton.Click();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<ArchiveArticleCommand>(command => command.Id == article.Id),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public void ClickingUnarchiveButton_SendsUnarchiveArticleCommand()
	{
		// Arrange
		var article = CreateArticle("Test Article", "author1", isPublished: true, isArchived: true);
		var articles = new List<ArticleDto> { article };
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);
		_mediator.Send(Arg.Any<UnarchiveArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(article));

		var cut = Render<ArticlesPage>();
		cut.FindAll("input[type=checkbox]")[1].Change(true);
		var unarchiveButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Unarchive");

		// Act
		unarchiveButton.Click();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<UnarchiveArticleCommand>(command => command.Id == article.Id),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public void GlobalSearch_NarrowsTable_ByTitleOrAuthor()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("Alpha Article", "admin1", isPublished: true, authorName: "Jane Doe"),
			CreateArticle("Beta Article", "admin1", isPublished: true, authorName: "John Smith")
		};
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();

		// Act - matches by title
		cut.Find("input[type=search]").Input("Alpha");

		// Assert
		cut.Markup.Should().Contain("Alpha Article");
		cut.Markup.Should().NotContain("Beta Article");

		// Act - matches by author
		cut.Find("input[type=search]").Input("Smith");

		// Assert
		cut.Markup.Should().Contain("Beta Article");
		cut.Markup.Should().NotContain("Alpha Article");
	}

	[Fact]
	public void TitleColumnFilter_NarrowsTable_ByPartialTitle()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("Alpha Article", "admin1", isPublished: true),
			CreateArticle("Beta Article", "admin1", isPublished: true)
		};
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();

		// Act
		cut.Find("input[aria-label='Filter by title']").Input("Alph");

		// Assert
		cut.Markup.Should().Contain("Alpha Article");
		cut.Markup.Should().NotContain("Beta Article");
	}

	[Fact]
	public void AuthorColumnFilter_NarrowsTable_ByPartialAuthorName()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("Alpha Article", "admin1", isPublished: true, authorName: "Jane Doe"),
			CreateArticle("Beta Article", "admin1", isPublished: true, authorName: "John Smith")
		};
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();

		// Act
		cut.Find("input[aria-label='Filter by author']").Input("Jane");

		// Assert
		cut.Markup.Should().Contain("Alpha Article");
		cut.Markup.Should().NotContain("Beta Article");
	}

	[Fact]
	public void CategoryColumnFilter_ListsArchivedCategories_AndNarrowsTableToSelectedCategory()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("Alpha Article", "admin1", isPublished: true, category: _category),
			CreateArticle("Beta Article", "admin1", isPublished: true, category: _archivedCategory)
		};
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();
		var categorySelect = cut.Find("select[aria-label='Filter by category']");

		// Assert - dropdown includes the archived category
		categorySelect.QuerySelectorAll("option").Should().Contain(
			o => o.TextContent.Contains(_archivedCategory.CategoryName, StringComparison.Ordinal));

		// Act
		categorySelect.Change(_archivedCategory.Id.ToString());

		// Assert
		cut.Markup.Should().Contain("Beta Article");
		cut.Markup.Should().NotContain("Alpha Article");
	}

	[Fact]
	public void StatusColumnFilter_NarrowsTableToPublishedOrDraft()
	{
		// Arrange
		var articles = new List<ArticleDto>
		{
			CreateArticle("Published Article", "admin1", isPublished: true),
			CreateArticle("Draft Article", "admin1", isPublished: false)
		};
		SetupAuthState(CreateAdminUser());
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();
		var statusSelect = cut.Find("select[aria-label='Filter by status']");

		// Act
		statusSelect.Change("Published");

		// Assert
		cut.Markup.Should().Contain("Published Article");
		cut.Markup.Should().NotContain("Draft Article");

		// Act
		statusSelect.Change("Draft");

		// Assert
		cut.Markup.Should().Contain("Draft Article");
		cut.Markup.Should().NotContain("Published Article");

		// Act
		statusSelect.Change("All");

		// Assert
		cut.Markup.Should().Contain("Published Article");
		cut.Markup.Should().Contain("Draft Article");
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

	private void SetupArticles(List<ArticleDto> articles)
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
		string content = "Test content", bool isArchived = false, string authorName = "Test Author",
		CategoryDto? category = null)
	{
		return new ArticleDto(
			Id: ObjectId.GenerateNewId().ToString(),
			Title: title,
			Slug: "test-slug",
			Content: content,
			Author: new AuthorDto(authorId, authorName),
			Category: category ?? new CategoryDto
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
