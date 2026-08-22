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
	public void RendersCategoriesList_WithValidHtmlStructure()
	{
		// Arrange
		var categories = CreateTestCategories();
		SetupCategories(categories);

		// Act
		var cut = Render<CategoriesPage>();

		// Assert - Validate HTML structure
		var divCount = CountOccurrences(cut.Markup, "<div");
		var closingDivCount = CountOccurrences(cut.Markup, "</div>");
		divCount.Should().Be(closingDivCount, "all opening divs should have matching closing divs");

		var sectionCount = CountOccurrences(cut.Markup, "<section");
		var closingSectionCount = CountOccurrences(cut.Markup, "</section>");
		sectionCount.Should().Be(closingSectionCount, "all opening sections should have matching closing sections");
	}

	[Fact]
	public void DisplaysCategories_WithNameAndSlug()
	{
		// Arrange
		var categories = CreateTestCategories();
		SetupCategories(categories);

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		cut.Markup.Should().Contain("Technology");
		cut.Markup.Should().Contain("technology");
		cut.Markup.Should().Contain("Lifestyle");
		cut.Markup.Should().Contain("lifestyle");
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
	public void ShowsEditForm_WhenEditButtonClicked()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateCategory("Technology", "technology", "Tech articles") };
		SetupCategories(categories);
		var cut = Render<CategoriesPage>();

		// Act
		cut.Find("button:contains('Edit')").Click();

		// Assert
		cut.Markup.Should().Contain("Save changes");
		cut.Markup.Should().Contain("Cancel");
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
	public void DisplaysCategoryContent_InCards()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateCategory("Technology", "tech-content", "Latest tech news") };
		SetupCategories(categories);

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		cut.Markup.Should().Contain("Technology");
		cut.Markup.Should().Contain("tech-content");
		cut.Markup.Should().Contain("Latest tech news");
	}

	[Fact]
	public void CategoryCard_HasCompleteStructure()
	{
		// Arrange
		var categories = CreateTestCategories();
		SetupCategories(categories);

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		cut.Markup.Should().Contain("rounded-xl border", "category cards should have proper styling");
		cut.Markup.Should().Contain("Archive", "category cards should have archive button");
	}

	[Fact]
	public void DisplaysCreateForm_WithNameAndDescriptionInputs()
	{
		// Arrange
		SetupEmptyCategories();

		// Act
		var cut = Render<CategoriesPage>();

		// Assert
		cut.Markup.Should().Contain("Create category");
		cut.Find("input.rounded-lg").Should().NotBeNull("should have name input");
		cut.Find("textarea.rounded-lg").Should().NotBeNull("should have description textarea");
		cut.FindAll("button[type='submit']").Should().HaveCount(1, "should have submit button");
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
