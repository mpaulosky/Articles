// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryDtoBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

using System.ComponentModel.DataAnnotations;

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
	public void CategoryNameValidationAttributes()
	{
		// Arrange
		var category = new CategoryDto
		{
			CategoryName = "Test Category",
			Slug = "test-category"
		};

		// Act
		var context = new ValidationContext(category) { MemberName = nameof(CategoryDto.CategoryName) };
		var results = new List<ValidationResult>();
		Validator.TryValidateProperty(category.CategoryName, context, results);

		// Assert
		results.Should().BeEmpty(); // Valid name should pass

		// Test required validation
		category.CategoryName = string.Empty;
		results.Clear();
		Validator.TryValidateProperty(category.CategoryName, context, results);
		results.Should().ContainSingle().Which.ErrorMessage.Should().Contain("required");

		// Test max length validation
		category.CategoryName = new string('a', 81);
		results.Clear();
		Validator.TryValidateProperty(category.CategoryName, context, results);
		results.Should().ContainSingle().Which.ErrorMessage.Should().Contain("80 characters");
	}

	[Fact]
	public void SlugValidationRejectsInvalidFormats()
	{
		// Arrange
		var category = new CategoryDto
		{
			CategoryName = "Test Category",
			Slug = "valid-slug-123"
		};

		// Act
		var context = new ValidationContext(category) { MemberName = nameof(CategoryDto.Slug) };
		var results = new List<ValidationResult>();
		Validator.TryValidateProperty(category.Slug, context, results);

		// Assert
		results.Should().BeEmpty(); // Valid slug should pass

		// Test invalid slug with uppercase
		category.Slug = "Invalid-Slug";
		results.Clear();
		Validator.TryValidateProperty(category.Slug, context, results);
		results.Should().ContainSingle().Which.ErrorMessage.Should().Contain("lowercase");

		// Test invalid slug with spaces
		category.Slug = "invalid slug";
		results.Clear();
		Validator.TryValidateProperty(category.Slug, context, results);
		results.Should().ContainSingle().Which.ErrorMessage.Should().Contain("lowercase");

		// Test invalid slug with special characters
		category.Slug = "invalid_slug!";
		results.Clear();
		Validator.TryValidateProperty(category.Slug, context, results);
		results.Should().ContainSingle().Which.ErrorMessage.Should().Contain("lowercase");
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
