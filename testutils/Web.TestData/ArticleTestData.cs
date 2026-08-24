// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleTestData.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.TestData
// =============================================

namespace Web.TestData;

/// <summary>
///   Builds article mediator commands for tests, with sensible defaults for every field.
/// </summary>
public static class ArticleTestData
{

	/// <summary>
	///   Creates a <see cref="CreateArticleCommand" />, overriding only the fields a test cares about.
	/// </summary>
	internal static CreateArticleCommand CreateCommand(
		string title = "My first article",
		string slug = "my-first-article",
		string content = "This is the article body.",
		AuthorDto? author = null,
		CategoryDto? category = null) =>
		new(title, slug, content, author ?? AuthorTestData.Create(), category);

	/// <summary>
	///   Creates an <see cref="UpdateArticleCommand" />, overriding only the fields a test cares about.
	/// </summary>
	internal static UpdateArticleCommand UpdateCommand(
		string id,
		string title = "My first article",
		string slug = "my-first-article",
		string content = "This is the article body.",
		CategoryDto? category = null,
		bool clearCategory = false) =>
		new(id, title, slug, content, category, clearCategory);

}
