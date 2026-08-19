// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AuthorDto.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Web.Components.Features.AuthInfo.Entities;

/// <summary>
///   Record representing author information captured from the authenticated user.
/// </summary>
/// <param name="UserId">The unique identifier from the authentication provider (Auth0 'sub' claim).</param>
/// <param name="Name">The display name of the author.</param>
/// <param name="Email">The email address of the author.</param>
[Serializable]
public sealed record AuthorDto(
	[property: BsonElement("userId")]
	[property: BsonRepresentation(BsonType.String)]
	string UserId,
	
	[property: BsonElement("name")]
	[property: BsonRepresentation(BsonType.String)]
	string Name,
	
	[property: BsonElement("email")]
	[property: BsonRepresentation(BsonType.String)]
	string Email = "")
{
	/// <summary>
	///   Initializes a new author snapshot with the supplied user identifier and display name.
	/// </summary>
	/// <param name="userId">The unique identifier for the author.</param>
	/// <param name="name">The display name for the author.</param>
	public AuthorDto(string userId, string name) : this(userId, name, string.Empty)
	{
	}
	
	/// <summary>
	///   Gets an empty AuthorDto instance.
	/// </summary>
	public static AuthorDto Empty => new(string.Empty, string.Empty, string.Empty);
}
