// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     HelpersBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

namespace Domain.Helpers;

public class HelpersBehaviorTests
{
	[Fact]
	public void StaticDateReturnsExpectedDeterministicValue()
	{
		// Arrange

		// Act
		var staticDate = DomainHelpers.StaticDate;

		// Assert
		staticDate.Should().Be(new DateTimeOffset(2025, 1, 1, 8, 0, 0, TimeSpan.Zero));
	}

	[Fact]
	public void CollectionNamesGetCollectionNameReturnsExpectedMappingsAndFailsForUnknownName()
	{
		// Arrange

		// Act
		var article = CollectionNames.GetCollectionName("Article");
		var category = CollectionNames.GetCollectionName("Category");
		var unknown = CollectionNames.GetCollectionName("Unknown");

		// Assert
		article.Success.Should().BeTrue();
		article.Value.Should().Be("Articles");
		category.Success.Should().BeTrue();
		category.Value.Should().Be("Categories");
		unknown.Success.Should().BeFalse();
		unknown.Error.Should().Be("Invalid entity name provided.");
	}

	[Fact]
	public void HelpersGenerateSlugProducesExpectedSlugVariants()
	{
		// Arrange
		var emptySlug = string.Empty.GenerateSlug();
		var whitespaceSlug = "   ".GenerateSlug();
		var punctuationSlug = "Hello, World!".GenerateSlug();
		var repeatedSeparatorsSlug = "Hello---World".GenerateSlug();
		var trailingPunctuationSlug = "Hello!".GenerateSlug();
		var uppercaseSlug = "ASP.NET Core".GenerateSlug();

		// Act
		var randomCategoryName = DomainHelpers.GetRandomCategoryName();

		// Assert
		emptySlug.Should().BeEmpty();
		whitespaceSlug.Should().BeEmpty();
		punctuationSlug.Should().Be("hello-world");
		repeatedSeparatorsSlug.Should().Be("hello-world");
		trailingPunctuationSlug.Should().Be("hello");
		uppercaseSlug.Should().Be("asp-net-core");
		randomCategoryName.Should().BeOneOf(MyCategories.First, MyCategories.Second, MyCategories.Third,
			MyCategories.Fourth, MyCategories.Fifth, MyCategories.Sixth, MyCategories.Seventh, MyCategories.Eighth,
			MyCategories.Ninth);
	}

	[Theory]
	[InlineData("Hello, World!   ", "hello-world")]
	[InlineData("!!!", "")]
	public void HelpersGenerateSlugHandlesTrailingWhitespaceAndAllPunctuationInputs(string input, string expectedSlug)
	{
		// Arrange

		// Act
		var slug = input.GenerateSlug();

		// Assert
		slug.Should().Be(expectedSlug);
	}

	[Fact]
	public void HelpersGenerateSlugReturnsEmptyForNullInput()
	{
		// Arrange
		string? input = null;

		// Act
		var slug = DomainHelpers.GenerateSlug(input!);

		// Assert
		slug.Should().BeEmpty();
	}
}