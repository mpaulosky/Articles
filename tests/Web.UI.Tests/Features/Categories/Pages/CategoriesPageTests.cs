using Bunit;

using Domain.Abstractions;

using FluentAssertions;

using Web.MyMediator;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson;

using NSubstitute;

using Web.Components.Features.Categories.Commands;
using Web.Components.Features.Categories.Models;
using Web.Components.Features.Categories.Pages;
using Web.Components.Features.Categories.Queries;

namespace Web.UI.Tests.Features.Categories.Pages;

public sealed class CategoriesPageTests : BunitContext
{
	private readonly IMediator _mediator = Substitute.For<IMediator>();

	public CategoriesPageTests()
	{
		// QuickGrid imports its own JS module for column-options positioning; the grid's data,
		// sorting, and rendering behavior under test don't depend on it.
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void RendersWithoutErrors_WhenNoCategories()
	{
		// Arrange
		SetupEmptyCategories();

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		cut.Should().NotBeNull();
		cut.Markup.Should().Contain("Categories");
		cut.Markup.Should().Contain("No categories yet");
	}

	[Fact]
	public void RendersHeaderBar_WithHeadingCheckboxAndCreateButton()
	{
		// Arrange
		SetupEmptyCategories();

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		cut.Markup.Should().Contain("Categories");
		cut.Markup.Should().Contain("Include Archived");
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Create Category");
	}

	[Fact]
	public void RendersCategoriesTable_WithValidHtmlStructure()
	{
		// Arrange
		var categories = CreateTestCategories();
		SetupCategories(categories);

		// Act
		var cut = Render<CategoriesPage>();

		// Assert - This would catch tag mismatch errors
		var divCount = CountOccurrences(cut.Markup, "<div");
		var closingDivCount = CountOccurrences(cut.Markup, "</div>");
		divCount.Should().Be(closingDivCount, "all opening divs should have matching closing divs");

		var openRows = CountOccurrences(cut.Markup, "<tr");
		var closeRows = CountOccurrences(cut.Markup, "</tr>");
		openRows.Should().Be(closeRows, "all <tr> tags must have matching closing tags");
	}

	[Fact]
	public void DisplaysCategories_WithNameSlugAndDescription()
	{
		// Arrange
		var categories = CreateTestCategories();
		SetupCategories(categories);

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		cut.Markup.Should().Contain("Technology");
		cut.Markup.Should().Contain("technology");
		cut.Markup.Should().Contain("Tech articles");
		cut.Markup.Should().Contain("Lifestyle");
		cut.Markup.Should().Contain("lifestyle");
		cut.Markup.Should().Contain("Lifestyle content");
	}

	[Fact]
	public void ShowsEditAndArchiveButtons_ForEachCategory()
	{
		// Arrange
		var categories = CreateTestCategories();
		SetupCategories(categories);

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		var editButtons = cut.FindAll("button:contains('Edit')");
		editButtons.Count.Should().Be(2, "each category should have an edit button");
		var archiveButtons = cut.FindAll("button:contains('Archive')");
		archiveButtons.Count.Should().Be(2, "each category should have an archive button");
	}

	[Fact]
	public void HidesArchivedCategories_ByDefault()
	{
		// Arrange
		var categories = new List<CategoryDto>
		{
			CreateCategory("Technology", "technology", "Tech articles"),
			CreateCategory("Lifestyle", "lifestyle", "Lifestyle content", isArchived: true)
		};
		SetupCategories(categories);

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		cut.Markup.Should().Contain("Technology");
		cut.Markup.Should().NotContain("Lifestyle");
	}

	[Fact]
	public void ShowsArchivedCategories_WhenIncludeArchivedIsChecked()
	{
		// Arrange
		var categories = new List<CategoryDto>
		{
			CreateCategory("Technology", "technology", "Tech articles"),
			CreateCategory("Lifestyle", "lifestyle", "Lifestyle content", isArchived: true)
		};
		SetupCategories(categories);
		var cut = Render<CategoriesPage>();

		// Act
		cut.Find("input[type='checkbox']").Change(true);

		// Assert
		cut.Markup.Should().Contain("Lifestyle");
		cut.Markup.Should().Contain("Archived");
	}

	[Fact]
	public void ShowsArchivedBadge_NextToName_ForArchivedCategory()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateCategory("Retired", "retired", "Old", isArchived: true) };
		SetupCategories(categories);

		var cut = Render<CategoriesPage>();
		cut.Find("input[type='checkbox']").Change(true);

		// Act
		var nameCell = cut.FindAll("td.font-medium").First(td => td.TextContent.Contains("Retired", StringComparison.Ordinal));

		// Assert
		var badge = nameCell.QuerySelector("span.app-badge");
		badge.Should().NotBeNull();
		badge!.TextContent.Trim().Should().Be("Archived");
	}

	[Fact]
	public void CreatePanel_IsHiddenByDefault_AndOpensOnButtonClick()
	{
		// Arrange
		SetupEmptyCategories();
		var cut = Render<CategoriesPage>();

		cut.Markup.Should().NotContain("Create category");

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Create Category").Click();

		// Assert
		cut.Markup.Should().Contain("Create category");
		cut.Find("form").Should().NotBeNull();
	}

	[Fact]
	public void CreateCategoryAsync_SendsCreateCategoryCommand_AndCollapsesPanel()
	{
		// Arrange
		SetupEmptyCategories();

		_mediator.Send(Arg.Any<CreateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(CreateCategory("New Category", "new-category", "New description")));

		var cut = Render<CategoriesPage>();
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Create Category").Click();

		cut.Find("input.rounded-lg").Change("New Category");
		cut.Find("textarea.rounded-lg").Change("New description");

		// Act
		cut.Find("form").Submit();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<CreateCategoryCommand>(command => command.Name == "New Category" && command.Description == "New description"),
			Arg.Any<CancellationToken>());

		cut.Markup.Should().NotContain("Create category");
	}

	[Fact]
	public void CreateCategoryAsync_KeepsPanelOpen_WhenSaveFails()
	{
		// Arrange
		SetupEmptyCategories();

		_mediator.Send(Arg.Any<CreateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<CategoryDto>("Name is too short."));

		var cut = Render<CategoriesPage>();
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Create Category").Click();

		cut.Find("input.rounded-lg").Change("N");
		cut.Find("textarea.rounded-lg").Change("New description");

		// Act
		cut.Find("form").Submit();

		// Assert
		cut.Markup.Should().Contain("Name is too short.");
		cut.Markup.Should().Contain("Create category");
	}

	[Fact]
	public void ClickingEdit_OpensModal_PrefilledWithCategory()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateCategory("Technology", "technology", "Tech articles") };
		SetupCategories(categories);
		var cut = Render<CategoriesPage>();

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Edit").Click();

		// Assert
		cut.Find("div[role='dialog']").Should().NotBeNull();
		cut.Markup.Should().Contain("Edit category");
		cut.Markup.Should().Contain("Save changes");
		cut.Find("input.rounded-lg").GetAttribute("value").Should().Be("Technology");
	}

	[Fact]
	public void CancellingEditModal_ClosesModal_WithoutSendingUpdate()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateCategory("Technology", "technology", "Tech articles") };
		SetupCategories(categories);
		var cut = Render<CategoriesPage>();
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Edit").Click();

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel").Click();

		// Assert
		cut.FindAll("div[role='dialog']").Should().BeEmpty();
		_mediator.DidNotReceive().Send(Arg.Any<UpdateCategoryCommand>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public void UpdateCategoryAsync_SendsUpdateCategoryCommand_AndClosesModal()
	{
		// Arrange
		var category = CreateCategory("Technology", "technology", "Tech articles");
		var categories = new List<CategoryDto> { category };
		SetupCategories(categories);

		_mediator.Send(Arg.Any<UpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(category));

		var cut = Render<CategoriesPage>();
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Edit").Click();
		cut.Find("input.rounded-lg").Change("Technology Updated");

		// Act
		cut.Find("form").Submit();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<UpdateCategoryCommand>(command => command.Id == category.Id.ToString() && command.Name == "Technology Updated"),
			Arg.Any<CancellationToken>());

		cut.Markup.Should().NotContain("Edit category");
	}

	[Fact]
	public void DisplaysLoadingMessage_Initially()
	{
		// Arrange
		var tcs = new TaskCompletionSource<Result<IReadOnlyList<CategoryDto>>>();
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		cut.Markup.Should().Contain("Loading categories...");

		// Complete the async operation
		tcs.SetResult(Result.Ok<IReadOnlyList<CategoryDto>>(new List<CategoryDto>()));
	}

	[Fact]
	public void DisplaysCategoryCount_InListHeader()
	{
		// Arrange
		var categories = CreateTestCategories();
		SetupCategories(categories);

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		cut.Markup.Should().Contain("2 total", "should show count of categories");
	}

	[Fact]
	public void ClickingArchive_RequiresConfirmation_BeforeSendingArchiveCategoryCommand()
	{
		// Arrange
		var category = CreateCategory("Technology", "technology", "Tech articles");
		var categories = new List<CategoryDto> { category };
		SetupCategories(categories);
		_mediator.Send(Arg.Any<ArchiveCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(category));

		var cut = Render<CategoriesPage>();

		// Act - first click only requests confirmation
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Archive").Click();

		// Assert
		_mediator.DidNotReceive().Send(Arg.Any<ArchiveCategoryCommand>(), Arg.Any<CancellationToken>());
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Confirm");

		// Act - confirming sends the command
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Confirm").Click();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<ArchiveCategoryCommand>(command => command.Id == category.Id.ToString()),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public void CancellingArchiveConfirmation_DoesNotSendArchiveCategoryCommand()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateCategory("Technology", "technology", "Tech articles") };
		SetupCategories(categories);
		var cut = Render<CategoriesPage>();
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Archive").Click();

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel").Click();

		// Assert
		_mediator.DidNotReceive().Send(Arg.Any<ArchiveCategoryCommand>(), Arg.Any<CancellationToken>());
		cut.FindAll("button").Should().Contain(b => b.TextContent.Trim() == "Archive");
	}

	[Fact]
	public void ClickingUnarchiveButton_SendsUnarchiveCategoryCommand()
	{
		// Arrange
		var category = CreateCategory("Technology", "technology", "Tech articles", isArchived: true);
		var categories = new List<CategoryDto> { category };
		SetupCategories(categories);
		_mediator.Send(Arg.Any<UnarchiveCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(category));

		var cut = Render<CategoriesPage>();
		cut.Find("input[type='checkbox']").Change(true);
		var unarchiveButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Unarchive");

		// Act
		unarchiveButton.Click();

		// Assert
		_mediator.Received(1).Send(
			Arg.Is<UnarchiveCategoryCommand>(command => command.Id == category.Id.ToString()),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public void SortsByName_WhenNameHeaderIsClicked_TogglesDirection()
	{
		// Arrange
		var categories = new List<CategoryDto>
		{
			CreateCategory("Bravo", "bravo", "Second"),
			CreateCategory("Alpha", "alpha", "First")
		};
		SetupCategories(categories);

		var cut = Render<CategoriesPage>();

		// Assert default ascending sort (Alpha before Bravo)
		var namesBefore = cut.FindAll("td.font-medium").Select(td => td.TextContent.Trim()).ToList();
		namesBefore.Should().ContainInOrder("Alpha", "Bravo");

		// Act - click Name header to reverse the sort
		FindColumnHeader(cut, "Name").QuerySelector("button.col-title")!.Click();

		// Assert descending sort (Bravo before Alpha)
		var namesAfter = cut.FindAll("td.font-medium").Select(td => td.TextContent.Trim()).ToList();
		namesAfter.Should().ContainInOrder("Bravo", "Alpha");
	}

	private static AngleSharp.Dom.IElement FindColumnHeader(IRenderedComponent<CategoriesPage> cut, string columnTitle)
	{
		return cut.FindAll("th")
			.First(th => th.QuerySelector(".col-title-text")?.TextContent.Trim() == columnTitle);
	}

	private void SetupEmptyCategories()
	{
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<CategoryDto>>(new List<CategoryDto>()));
	}

	private void SetupCategories(List<CategoryDto> categories)
	{
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<CategoryDto>>(categories));
	}

	private IRenderedComponent<CategoriesPage> Render<T>() where T : CategoriesPage
	{
		Services.AddSingleton(_mediator);
		return base.Render<T>();
	}

	private static List<CategoryDto> CreateTestCategories()
	{
		return
		[
			CreateCategory("Technology", "technology", "Tech articles"),
			CreateCategory("Lifestyle", "lifestyle", "Lifestyle content")
		];
	}

	private static CategoryDto CreateCategory(string name, string slug, string description, bool isArchived = false)
	{
		return new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = name,
			Slug = slug,
			Description = description,
			IsArchived = isArchived
		};
	}

	private static int CountOccurrences(string text, string searchTerm)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchTerm))
		{
			return 0;
		}

		var count = 0;
		var index = 0;

		while ((index = text.IndexOf(searchTerm, index, StringComparison.OrdinalIgnoreCase)) != -1)
		{
			count++;
			index += searchTerm.Length;
		}

		return count;
	}
}
