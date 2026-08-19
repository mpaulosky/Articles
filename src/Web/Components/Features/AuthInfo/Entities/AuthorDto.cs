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
[Serializable]
public sealed class AuthorDto
{
	/// <summary>
	///   Initializes a new empty author snapshot.
	/// </summary>
	public AuthorDto()
	{
		UserId = string.Empty;
		Name = string.Empty;
		Email = string.Empty;
	}

	/// <summary>
	///   Initializes a new author snapshot with the supplied user identifier and display name.
	/// </summary>
	/// <param name="userId">The unique identifier for the author.</param>
	/// <param name="name">The display name for the author.</param>
	public AuthorDto(string userId, string name) : this(userId, name, string.Empty)
	{
	}

	/// <summary>
	///   Initializes a new author snapshot with the supplied user identifier, display name, and email address.
	/// </summary>
	/// <param name="userId">The unique identifier for the author.</param>
	/// <param name="name">The display name for the author.</param>
	/// <param name="email">The email address for the author.</param>
	public AuthorDto(string userId, string name, string email)
	{
		UserId = userId;
		Name = name;
		Email = email;
	}

	/// <summary>
	///   Gets the unique user identifier from the authentication provider (Auth0 'sub' claim).
	/// </summary>
	[BsonElement("userId")]
	[BsonRepresentation(BsonType.String)]
	public string UserId { get; init; }

	/// <summary>
	///   Gets the display name of the author.
	/// </summary>
	[BsonElement("name")]
	[BsonRepresentation(BsonType.String)]
	public string Name { get; init; }

	/// <summary>
	///   Gets the email address of the author.
	/// </summary>
	[BsonElement("email")]
	[BsonRepresentation(BsonType.String)]
	public string Email { get; init; }

	/// <summary>
	///   Gets an empty AuthorInfo instance.
	/// </summary>
	public static AuthorDto Empty => new() { UserId = string.Empty, Name = string.Empty, Email = string.Empty };
}
