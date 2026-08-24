// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AuthorTestData.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.TestData
// =============================================

namespace Web.TestData;

/// <summary>
///   Builds <see cref="AuthorDto" /> instances for tests, with sensible defaults for every field.
/// </summary>
public static class AuthorTestData
{

	/// <summary>
	///   Creates an <see cref="AuthorDto" />, overriding only the fields a test cares about.
	/// </summary>
	public static AuthorDto Create(
		string userId = "user-1",
		string name = "Author",
		string email = "author@example.com") =>
		new(userId, name, email);

}
