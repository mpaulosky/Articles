// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AuthorInfo.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using System.ComponentModel.DataAnnotations;

using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Models;

/// <summary>
///   Record representing author information captured from the authenticated user.
/// </summary>
public record AuthorInfo
{
	public AuthorInfo()
	{
		UserId = string.Empty;
		Name = string.Empty;
	}

	public AuthorInfo(string userId, string name)
	{
		UserId = userId;
		Name = name;
	}

	/// <summary>
	///   Gets the unique user identifier from the authentication provider (Auth0 'sub' claim).
	/// </summary>
	[BsonElement("userId")]
	[BsonRepresentation(BsonType.String)]
	[MaxLength(200)]
	public string UserId { get; init; }

	/// <summary>
	///   Gets the display name of the author.
	/// </summary>
	[BsonElement("name")]
	[BsonRepresentation(BsonType.String)]
	[MaxLength(200)]
	public string Name { get; init; }

	/// <summary>
	///   Gets an empty AuthorInfo instance.
	/// </summary>
	public static AuthorInfo Empty => new() { UserId = string.Empty, Name = string.Empty };
}
