// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Category.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Web.Components.Features.Categories.Entities;

/// <summary>
///     Represents a category that groups related articles.
/// </summary>
[Serializable]
public sealed class Category
{
	private static readonly DateTimeOffset EmptyCreatedOn = DateTimeOffset.UnixEpoch;

	/// <summary>
	///     Gets the unique MongoDB identifier for the category.
	/// </summary>
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; private set; }

	/// <summary>
	///     Gets the category display name.
	/// </summary>
	[BsonElement("name")]
	[BsonRepresentation(BsonType.String)]
	public string Name { get; private set; } = string.Empty;

	/// <summary>
	///     Gets the category description.
	/// </summary>
	[BsonElement("description")]
	[BsonRepresentation(BsonType.String)]
	public string Description { get; private set; } = string.Empty;

	/// <summary>
	///   Gets or sets the slug for the category, used in the category's URL.
	/// </summary>
	[BsonElement("slug")]
	[BsonRepresentation(BsonType.String)]
	public string Slug { get; set; } = string.Empty;

	/// <summary>
	///   Gets the date and time when this entity was created.
	/// </summary>
	[BsonElement("createdOn")]
	[BsonRepresentation(BsonType.DateTime)]
	public DateTimeOffset CreatedOn { get; set; }

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

	private Category()
	{
	}

	/// <summary>
	///   Gets a fresh empty category instance with a stable creation timestamp.
	/// </summary>
	/// <returns>A new <see cref="Category" /> instance.</returns>
	public static Category Empty => new()
	{
		Id = ObjectId.Empty,
		Name = string.Empty,
		Description = string.Empty,
		Slug = string.Empty,
		CreatedOn = EmptyCreatedOn,
		ModifiedOn = null,
		IsArchived = false
	};

	/// <summary>
	///     Creates a new category with the supplied name and description.
	/// </summary>
	/// <param name="name">The category display name.</param>
	/// <param name="description">The category description.</param>
	/// <returns>A new <see cref="Category" /> instance.</returns>
	public static Category Create(string name, string description)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentException.ThrowIfNullOrWhiteSpace(description);

		return new Category
		{
			Id = ObjectId.GenerateNewId(),
			Name = name.Trim(),
			Description = description.Trim(),
			CreatedOn = DateTimeOffset.UtcNow,
			Slug = string.Empty
		};
	}

	/// <summary>
	///     Updates the category name and description.
	/// </summary>
	/// <param name="name">The updated category display name.</param>
	/// <param name="description">The updated category description.</param>
	public void Update(string name, string description)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentException.ThrowIfNullOrWhiteSpace(description);
		Name = name.Trim();
		Description = description.Trim();
		ModifiedOn = DateTime.UtcNow;
	}
}
