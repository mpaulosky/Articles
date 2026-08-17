// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryDto.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

/// <summary>
///   Represents a data transfer object for a category.
/// </summary>
[Serializable]
public sealed class CategoryDto
{
	/// <summary>
	///   Parameterless constructor for serialization and test data generation.
	/// </summary>
	public CategoryDto() : this(ObjectId.Empty, string.Empty, string.Empty, DateTime.UtcNow, null, false)
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="CategoryDto" /> class.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="categoryName"></param>
	/// <param name="slug"></param>
	/// <param name="createdOn"></param>
	/// <param name="modifiedOn"></param>
	/// <param name="isArchived">Indicates whether the category is archived.</param>
	private CategoryDto(
		ObjectId id,
		string categoryName,
		string slug,
		DateTime createdOn,
		DateTime? modifiedOn,
		bool isArchived)
	{
		Id = id;
		CategoryName = categoryName;
		Slug = slug;
		CreatedOn = createdOn;
		ModifiedOn = modifiedOn;
		IsArchived = isArchived;
	}

	/// <summary>
	///   Gets or sets the unique identifier for the category.
	/// </summary>
	public ObjectId Id { get; set; }

	/// <summary>
	///   Gets the name of the category.
	/// </summary>
	[Display(Name = "Category Name")]
	[Required(ErrorMessage = "Category name is required")]
	[StringLength(80, ErrorMessage = "Category name cannot exceed 80 characters")]
	public string CategoryName { get; set; }

	/// <summary>
	///   Gets or sets the slug for the category, used in the category's URL.
	/// </summary>
	[Display(Name = "Slug")]
	[StringLength(200, ErrorMessage = "Slug cannot exceed200 characters")]
	[RegularExpression(@"^[a-z0-9-]+$",
		ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
	public string Slug { get; set; }

	/// <summary>
	///   Gets the date and time when this entity was created.
	/// </summary>
	/// )]
	[Display(Name = "Created On")]
	public DateTimeOffset CreatedOn { get; set; }

	/// <summary>
	///   Gets or sets the date and time when this entity was last modified.
	/// </summary>
	[Display(Name = "Modified On")]
	public DateTimeOffset? ModifiedOn { get; set; }

	/// <summary>
	///   Gets or sets a value indicating whether the category is archived.
	/// </summary>
	[Display(Name = "Is Archived")]
	public bool IsArchived { get; set; }


	/// <summary>
	///   Gets a fresh empty category instance.
	/// </summary>
	public static CategoryDto Empty => new(ObjectId.Empty, string.Empty, string.Empty, DateTime.UtcNow, null, false);
}
