using AngleSharp.Dom;

using Bunit;

using Domain.Abstractions;

using FluentAssertions;

using MongoDB.Bson;

using Web.MyMediator;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using System.Security.Claims;
using System.Text.RegularExpressions;

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
		// QuickGrid imports its own JS module for column-options positioning; the grid's data,
		// sorting, filtering, and paging behavior under test don't depend on it.
		JSInterop.Mode = JSRuntimeMode.Loose;

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
		SetupAuthState(CreateAdminUser());
		SetupEmptyArticles();

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.Markup.Should().Contain("All Articles");
		cut.Markup.Should().Contain("Show My Articles Only");
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Create New Article");
	}

	[Fact]
	public void ShowsCreateButton_WhenUserIsAuthor()
	{
		// Arrange
		SetupAuthState(CreateAuthorUser("author1"));
		SetupEmptyArticles();

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Create New Article");
	}

	[Fact]
	public void HidesCreateButton_WhenUserIsRegularUser()
	{
		// Arrange
		SetupAuthState(CreateAuthenticatedUser("regularUser"));
		SetupEmptyArticles();

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.FindAll("button").Should().NotContain(b => b.TextContent.Trim() == "Create New Article");
	}

	[Fact]
	public void HidesCreateButton_WhenUserIsAnonymous()
	{
		// Arrange
		SetupAuthState(CreateAnonymousUser());
		SetupEmptyArticles();

		// Act
		var cut = Render<ArticlesPage>();

		// Assert
		cut.FindAll("button").Should().NotContain(b => b.TextContent.Trim() == "Create New Article");
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
		viewLink!.GetAttribute("href").Should().Be($"/articles/{articles[0].Slug}");
	}

	[Fact]
	public void ShowsEditPublishButtons_WhenUserCanEdit()
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
	}

	[Fact]
	public void DisablesEditPublishButtons_WhenUserCannotEdit()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("Test Article 1", "author1", isPublished: true) };
		var user = CreateAuthenticatedUser("regularUser");
		SetupAuthState(user);
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();
		var linkLabels = cut.FindAll("a").Select(a => a.TextContent.Trim()).ToList();
		var unpublishButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Unpublish");

		// Assert
		// The article is visible (published), but this user cannot edit it, so Edit/Unpublish
		// stay in place (same button positions for every row) but are shown disabled.
		cut.Markup.Should().Contain("Test Article 1");
		linkLabels.Should().NotContain("Edit");
		cut.FindAll("span").Should().Contain(s => s.TextContent.Trim() == "Edit" && s.HasAttribute("aria-disabled"));
		unpublishButton.HasAttribute("disabled").Should().BeTrue();
	}

	[Fact]
	public void HidesEditPublishButtons_ForOtherAuthorsArticle_WhenUserIsAuthor()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("Someone Else's Article", "author2", isPublished: true) };
		var user = CreateAuthorUser("author1");
		SetupAuthState(user);
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();
		var linkLabels = cut.FindAll("a").Select(a => a.TextContent.Trim()).ToList();

		// Assert
		linkLabels.Should().NotContain("Edit");
	}

	[Fact]
	public void ShowsEditPublishButtons_ForOwnArticle_WhenUserIsAuthor()
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
	public void ClickingCreateNewArticle_NavigatesToCreatePage()
	{
		// Arrange
		SetupAuthState(CreateAdminUser());
		SetupEmptyArticles();
		var cut = Render<ArticlesPage>();
		var navigation = Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Create New Article").Click();

		// Assert
		navigation.Uri.Should().EndWith("/articles/create");
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
	public void ClickingArchive_RequiresConfirmation_BeforeSendingArchiveArticleCommand()
	{
		// Arrange
		var article = CreateArticle("Test Article", "author1", isPublished: true);
		var articles = new List<ArticleDto> { article };
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);
		_mediator.Send(Arg.Any<ArchiveArticleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(article));

		var cut = Render<ArticlesPage>();

		// Act - first click only requests confirmation
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Archive").Click();

		// Assert
		_mediator.DidNotReceive().Send(Arg.Any<ArchiveArticleCommand>(), Arg.Any<CancellationToken>());
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Confirm");

		// Act - confirming sends the command
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Confirm").Click();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<ArchiveArticleCommand>(command => command.Id == article.Id),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public void CancellingArchiveConfirmation_DoesNotSendArchiveArticleCommand()
	{
		// Arrange
		var article = CreateArticle("Test Article", "author1", isPublished: true);
		var articles = new List<ArticleDto> { article };
		var user = CreateAdminUser();
		SetupAuthState(user);
		SetupArticles(articles);

		var cut = Render<ArticlesPage>();
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Archive").Click();

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel").Click();

		// Assert
		_mediator.DidNotReceive().Send(Arg.Any<ArchiveArticleCommand>(), Arg.Any<CancellationToken>());
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Archive");
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
		FindColumnHeader(cut, "Title").QuerySelector("button.col-title")!.Click();

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

		// Assert first page shows exactly 10 populated rows (QuickGrid pads short pages with blank rows,
		// but page 1 of 15 items at 10/page is already full, so there's nothing to pad here)
		NonEmptyRows(cut).Should().Be(10);
		PaginationText(cut).Should().Be("Page 1 of 2");

		// Act
		cut.Find("button.go-next").Click();

		// Assert second page shows the remaining 5 populated rows
		NonEmptyRows(cut).Should().Be(5);
		PaginationText(cut).Should().Be("Page 2 of 2");
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
	public void DisablesArchiveAction_ForNonAdmin_EvenOnOwnArticle()
	{
		// Arrange
		var articles = new List<ArticleDto> { CreateArticle("My Article", "author1", isPublished: true) };
		SetupAuthState(CreateAuthorUser("author1"));
		SetupArticles(articles);

		// Act
		var cut = Render<ArticlesPage>();
		var archiveButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Archive");

		// Assert
		// Stays visible in the same position as the admin view, just disabled.
		archiveButton.HasAttribute("disabled").Should().BeTrue();
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

		// Act - archiving requires confirmation before the command is sent
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Archive").Click();
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Confirm").Click();

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
		OpenColumnOptions(cut, "Title");

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
		OpenColumnOptions(cut, "Author");

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
		OpenColumnOptions(cut, "Category");
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
		OpenColumnOptions(cut, "Status");
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

	private static IElement FindColumnHeader(IRenderedComponent<ArticlesPage> cut, string columnTitle)
	{
		return cut.FindAll("th")
			.First(th => th.QuerySelector(".col-title-text")?.TextContent.Trim() == columnTitle);
	}

	private static void OpenColumnOptions(IRenderedComponent<ArticlesPage> cut, string columnTitle)
	{
		FindColumnHeader(cut, columnTitle).QuerySelector("button.col-options-button")!.Click();
	}

	private static int NonEmptyRows(IRenderedComponent<ArticlesPage> cut)
	{
		return cut.FindAll("tbody tr").Count(tr => !string.IsNullOrWhiteSpace(tr.TextContent));
	}

	private static string PaginationText(IRenderedComponent<ArticlesPage> cut)
	{
		return Regex.Replace(cut.Find("div.pagination-text").TextContent, @"\s+", " ").Trim();
	}

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
