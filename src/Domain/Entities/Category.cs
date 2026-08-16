// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Category.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain
// =============================================

namespace Domain.Entities;

/// <summary>
///     Represents a category that groups related articles.
/// </summary>
public sealed class Category
{
	/// <summary>
	///     Gets the unique MongoDB identifier for the category.
	/// </summary>
	public ObjectId Id { get; private set; }

	/// <summary>
	///     Gets the category display name.
	/// </summary>
	public string Name { get; private set; } = string.Empty;

	/// <summary>
	///     Gets the category description.
	/// </summary>
	public string Description { get; private set; } = string.Empty;

	private Category()
	{
	}

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

		return new Category { Id = ObjectId.GenerateNewId(), Name = name.Trim(), Description = description.Trim(), };
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
	}
}