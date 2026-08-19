// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryDto.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Web.Components.Features.Categories.Models;

/// <summary>
///   Represents a data transfer object for a category.
/// </summary>
[Serializable]
public sealed class CategoryDto
{
	private static readonly DateTime EmptyCreatedOn = DateTime.UnixEpoch;

	/// <summary>
	///   Initializes a new empty category snapshot for serialization and test data generation.
	/// </summary>
	public CategoryDto() : this(ObjectId.Empty, string.Empty, string.Empty, EmptyCreatedOn, null, false)
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="CategoryDto" /> class.
	/// </summary>
	/// <param name="id">The unique MongoDB identifier for the category.</param>
	/// <param name="categoryName">The display name of the category.</param>
	/// <param name="slug">The URL-friendly slug for the category.</param>
	/// <param name="createdOn">The UTC creation time for the category snapshot.</param>
	/// <param name="modifiedOn">The last modification time for the category snapshot, if any.</param>
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
	[BsonId]
	[BsonElement("_id")]
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; set; }

	/// <summary>
	///   Gets the name of the category.
	/// </summary>
	[BsonElement("categoryName")]
	[BsonRepresentation(BsonType.String)]
	public string CategoryName { get; set; }

	/// <summary>
	///   Gets or sets the slug for the category, used in the category's URL.
	/// </summary>
	[BsonElement("slug")]
	[BsonRepresentation(BsonType.String)]
	public string Slug { get; set; }

	/// <summary>
	///   Gets the date and time when this entity was created.
	/// </summary>
	[BsonElement("createdOn")]
	[BsonRepresentation(BsonType.DateTime)]
	public DateTime CreatedOn { get; set; }

	/// <summary>
	///   Gets or sets the date and time when this entity was last modified.
	/// </summary>
	[BsonElement("modifiedOn")]
	[BsonRepresentation(BsonType.DateTime)]
	public DateTime? ModifiedOn { get; set; }

	/// <summary>
	///   Gets or sets a value indicating whether the category is archived.
	/// </summary>
	[BsonElement("isArchived")]
	[BsonRepresentation(BsonType.Boolean)]
	public bool IsArchived { get; set; }

	/// <summary>
	///   Gets a fresh empty category snapshot with a stable creation timestamp.
	/// </summary>
	public static CategoryDto Empty => new(
		ObjectId.Empty,
		string.Empty,
		string.Empty,
		EmptyCreatedOn,
		null,
		false);
}
