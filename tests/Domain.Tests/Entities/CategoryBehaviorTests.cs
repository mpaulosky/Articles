// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

namespace Domain.Entities;

public class CategoryBehaviorTests
{
	[Fact]
	public void CreateSetsTrimmedValues()
	{
		// Arrange
		var name = "  Tech  ";
		var description = "  Domain knowledge  ";

		// Act
		var category = Category.Create(name, description);

		// Assert
		category.Id.Should().NotBe(ObjectId.Empty);
		category.Name.Should().Be("Tech");
		category.Description.Should().Be("Domain knowledge");
	}

	[Fact]
	public void CreateThrowsForInvalidNameOrDescription()
	{
		// Arrange

		// Act
		Action actWithBlankName = () => Category.Create("   ", "description");
		Action actWithBlankDescription = () => Category.Create("name", "   ");

		// Assert
		actWithBlankName.Should().Throw<ArgumentException>();
		actWithBlankDescription.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void UpdateTrimsAndSetsValues()
	{
		// Arrange
		var category = Category.Create("Original", "Original description");

		// Act
		category.Update("  Updated  ", "  New description  ");

		// Assert
		category.Name.Should().Be("Updated");
		category.Description.Should().Be("New description");
	}

	[Fact]
	public void MyCategoriesContainsExpectedValues()
	{
		// Arrange

		// Act
		var categories = new[]
		{
			MyCategories.First, MyCategories.Second, MyCategories.Third, MyCategories.Fourth, MyCategories.Fifth,
			MyCategories.Sixth, MyCategories.Seventh, MyCategories.Eighth, MyCategories.Ninth
		};

		// Assert
		categories.Should().HaveCount(9);
		categories.Should().Contain("ASP.NET Core");
		categories.Should().Contain("General Programming");
		categories.Should().Contain("Other .NET Topics");
	}
}