// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using Domain.Helpers;

using FluentAssertions;

using MongoDB.Bson;

using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Entities;
using Web.Components.Features.Categories.Models;

namespace Web.Tests.Features.Categories.Entities;

public class CategoryBehaviorTests
{
	[Fact]
	public void CategoryDtoEmptyIsFreshBlankState()
	{
		// Arrange

		// Act
		var empty = CategoryDto.Empty;

		// Assert
		empty.Id.Should().Be(ObjectId.Empty);
		empty.CategoryName.Should().BeEmpty();
		empty.Slug.Should().BeEmpty();
		empty.CreatedOn.Should().NotBe(default(DateTime));
		empty.ModifiedOn.Should().BeNull();
		empty.IsArchived.Should().BeFalse();
	}

	[Fact]
	public void CategoryDtoAcceptsAnySlugValue()
	{
		// Arrange
		var category = new CategoryDto { Id = ObjectId.GenerateNewId(), CategoryName = "Technology", Slug = "Technology!" };

		// Act & Assert - CategoryDto has no validation attributes
		category.Slug.Should().Be("Technology!");
		category.CategoryName.Should().Be("Technology");
		category.Id.Should().NotBe(ObjectId.Empty);
	}

	[Fact]
	public void AuthorInfoEmptyUsesBlankValues()
	{
		// Arrange

		// Act
		var empty = AuthorDto.Empty;

		// Assert
		empty.UserId.Should().BeEmpty();
		empty.Name.Should().BeEmpty();
	}

	[Fact]
	public void AuthorInfoRetainsProvidedValues()
	{
		// Arrange
		var authorInfo = new AuthorDto("auth-123", "Ada Lovelace");

		// Act

		// Assert
		authorInfo.UserId.Should().Be("auth-123");
		authorInfo.Name.Should().Be("Ada Lovelace");
	}

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
	public void ArchiveSetsIsArchivedTrue()
	{
		// Arrange
		var category = Category.Create("Tech", "Tech articles");

		// Act
		category.Archive();

		// Assert
		category.IsArchived.Should().BeTrue();
	}

	[Fact]
	public void ArchiveIsIdempotentWhenAlreadyArchived()
	{
		// Arrange
		var category = Category.Create("Tech", "Tech articles");
		category.Archive();
		var firstArchiveTime = category.ModifiedOn;

		// Act
		category.Archive();

		// Assert
		category.IsArchived.Should().BeTrue();
		category.ModifiedOn.Should().Be(firstArchiveTime);
	}

	[Fact]
	public void UnarchiveSetsIsArchivedFalse()
	{
		// Arrange
		var category = Category.Create("Tech", "Tech articles");
		category.Archive();

		// Act
		category.Unarchive();

		// Assert
		category.IsArchived.Should().BeFalse();
	}

	[Fact]
	public void UnarchiveIsIdempotentWhenAlreadyUnarchived()
	{
		// Arrange
		var category = Category.Create("Tech", "Tech articles");
		var beforeModifiedOn = category.ModifiedOn;

		// Act
		category.Unarchive();

		// Assert
		category.IsArchived.Should().BeFalse();
		category.ModifiedOn.Should().Be(beforeModifiedOn);
	}

	[Fact]
	public void CategoryEmpty_WhenRequested_ExpectedFreshBlankState()
	{
		// Arrange

		// Act
		var empty = Category.Empty;

		// Assert
		empty.Id.Should().Be(ObjectId.Empty);
		empty.Name.Should().BeEmpty();
		empty.Description.Should().BeEmpty();
		empty.Slug.Should().BeEmpty();
		empty.CreatedOn.Should().Be(DateTimeOffset.UnixEpoch);
		empty.ModifiedOn.Should().BeNull();
		empty.IsArchived.Should().BeFalse();
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
