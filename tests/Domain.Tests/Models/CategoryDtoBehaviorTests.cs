// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryDtoBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

using MongoDB.Bson;
using Web.Components.Features.Categories.Models;

namespace Web.Components.Features.Categories.Models;

public class CategoryDtoBehaviorTests
{
	[Fact]
	public void EmptyReturnsStableInstance()
	{
		// Arrange

		// Act
		var empty1 = CategoryDto.Empty;
		var empty2 = CategoryDto.Empty;

		// Assert
		empty1.Id.Should().Be(ObjectId.Empty);
		empty1.CategoryName.Should().BeEmpty();
		empty1.Slug.Should().BeEmpty();
		empty1.CreatedOn.Should().Be(DateTime.UnixEpoch);
		empty1.ModifiedOn.Should().BeNull();
		empty1.IsArchived.Should().BeFalse();

		// Both calls should return equivalent instances
		empty1.Id.Should().Be(empty2.Id);
		empty1.CategoryName.Should().Be(empty2.CategoryName);
		empty1.CreatedOn.Should().Be(empty2.CreatedOn);
	}

	[Fact]
	public void CategoryNameCanBeSetAndRetrieved()
	{
		// Arrange
		var category = new CategoryDto
		{
			CategoryName = "Test Category",
			Slug = "test-category"
		};

		// Act & Assert
		category.CategoryName.Should().Be("Test Category");
		
		// Test empty name
		category.CategoryName = string.Empty;
		category.CategoryName.Should().BeEmpty();

		// Test long name
		var longName = new string('a', 100);
		category.CategoryName = longName;
		category.CategoryName.Should().Be(longName);
	}

	[Fact]
	public void SlugCanBeSetToAnyValue()
	{
		// Arrange
		var category = new CategoryDto
		{
			CategoryName = "Test Category",
			Slug = "valid-slug-123"
		};

		// Act & Assert
		category.Slug.Should().Be("valid-slug-123");

		// Test slug with uppercase (no validation)
		category.Slug = "Invalid-Slug";
		category.Slug.Should().Be("Invalid-Slug");

		// Test slug with spaces (no validation)
		category.Slug = "invalid slug";
		category.Slug.Should().Be("invalid slug");

		// Test slug with special characters (no validation)
		category.Slug = "invalid_slug!";
		category.Slug.Should().Be("invalid_slug!");
	}

	[Fact]
	public void IsArchivedDefaultsToFalse()
	{
		// Arrange

		// Act
		var category = new CategoryDto();

		// Assert
		category.IsArchived.Should().BeFalse();
	}
}
