// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CollectionNames.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain
// =============================================

using Domain.Abstractions;

namespace Domain.Helpers;

/// <summary>
///     Resolves domain entity names to MongoDB collection names.
/// </summary>
public static class CollectionNames
{
	/// <summary>
	///     Gets the MongoDB collection name for a known domain entity name.
	/// </summary>
	/// <param name="entityName">The domain entity name to resolve.</param>
	/// <returns>A successful result containing the collection name, or a failed result for an unknown entity.</returns>
	public static Result<string> GetCollectionName(string? entityName)
	{
		switch (entityName)
		{
			case "Article": return Result.Ok("Articles");

			case "Category": return Result.Ok("Categories");

			default: return Result<string>.Fail("Invalid entity name provided.");
		}
	}
}