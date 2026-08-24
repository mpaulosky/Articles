// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryTestData.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.TestData
// =============================================

namespace Web.TestData;

/// <summary>
///   Builds <see cref="CategoryDto" /> instances and category mediator commands for tests, with sensible
///   defaults for every field.
/// </summary>
public static class CategoryTestData
{

	/// <summary>
	///   Creates a <see cref="CategoryDto" />, overriding only the fields a test cares about.
	/// </summary>
	public static CategoryDto Dto(
		string categoryName = "Technology",
		string slug = "technology",
		string description = "",
		DateTime? createdOn = null,
		DateTime? modifiedOn = null,
		bool isArchived = false) =>
		new()
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = categoryName,
			Slug = slug,
			Description = description,
			CreatedOn = createdOn ?? DateTime.UtcNow,
			ModifiedOn = modifiedOn,
			IsArchived = isArchived
		};

	/// <summary>
	///   Creates a <see cref="CreateCategoryCommand" />, overriding only the fields a test cares about.
	/// </summary>
	internal static CreateCategoryCommand CreateCommand(
		string name = "Technology",
		string description = "A technology category.") =>
		new(name, description);

	/// <summary>
	///   Creates an <see cref="UpdateCategoryCommand" />, overriding only the fields a test cares about.
	/// </summary>
	internal static UpdateCategoryCommand UpdateCommand(
		string id,
		string name = "Technology",
		string description = "A technology category.") =>
		new(id, name, description);

}
