// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     PostAuthor.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain
// =============================================

namespace Domain.ValueObjects;

/// <summary>
///     Immutable snapshot of the post author's identity at the time the post was created.
///     Stored as an embedded document in MongoDB.
/// </summary>
public sealed record PostAuthor(
	string Id,
	string Name,
	string Email,
	IReadOnlyList<string> Roles)
{
	/// <summary>Empty/anonymous author placeholder for testing and migration purposes.</summary>
	public static readonly PostAuthor Empty = new(
		string.Empty,
		string.Empty,
		string.Empty,
		Array.Empty<string>());
}