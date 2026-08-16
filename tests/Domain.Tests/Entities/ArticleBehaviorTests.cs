// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

namespace Domain.Entities;

public class ArticleBehaviorTests
{
	private static readonly string[] AdminRoles = ["admin"];

	[Fact]
	public void CreateSetsExpectedState()
	{
		// Arrange
		var author = new PostAuthor("author-1", "Ada Lovelace", "ada@example.com", AdminRoles);

		// Act
		var article = Article.Create("My First Post", "Hello from the domain", author);

		// Assert
		article.Id.Should().NotBe(ObjectId.Empty);
		article.Title.Should().Be("My First Post");
		article.Content.Should().Be("Hello from the domain");
		article.Author.Should().Be(author);
		article.CreatedAt.Should().BeAfter(DateTime.MinValue);
		article.UpdatedAt.Should().BeNull();
		article.IsPublished.Should().BeFalse();
		article.Version.Should().Be(0);
		article.CategoryId.Should().BeNull();
	}

	[Fact]
	public void CreateThrowsForInvalidTitleOrContent()
	{
		// Arrange
		var author = new PostAuthor("author-1", "Ada Lovelace", "ada@example.com", Array.Empty<string>());

		// Act
		Action actWithBlankTitle = () => Article.Create("   ", "content", author);
		Action actWithBlankContent = () => Article.Create("title", "   ", author);
		Action actWithNullAuthor = () => Article.Create("title", "content", null!);
		Action actWithBlankAuthorName = () => Article.Create("title", "content",
			new PostAuthor("author-1", "  ", "ada@example.com", Array.Empty<string>()));

		// Assert
		actWithBlankTitle.Should().Throw<ArgumentException>();
		actWithBlankContent.Should().Throw<ArgumentException>();
		actWithNullAuthor.Should().Throw<ArgumentNullException>();
		actWithBlankAuthorName.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void PublishAndUnpublishTogglePublishedState()
	{
		// Arrange
		var article = Article.Create("Post", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));

		// Act
		article.Publish();
		var publishedState = article.IsPublished;
		article.Unpublish();

		// Assert
		publishedState.Should().BeTrue();
		article.IsPublished.Should().BeFalse();
	}

	[Fact]
	public void UpdateChangesContentAndVersionAndCanClearCategory()
	{
		// Arrange
		var article = Article.Create("Original", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var categoryId = ObjectId.GenerateNewId();
		article.AssignCategory(categoryId);

		// Act
		article.Update("Updated", "Body updated", categoryId: ObjectId.GenerateNewId(), clearCategory: false);
		var firstUpdateVersion = article.Version;
		article.Update("Updated again", "Body updated again", clearCategory: true);

		// Assert
		article.Title.Should().Be("Updated again");
		article.Content.Should().Be("Body updated again");
		article.CategoryId.Should().BeNull();
		article.Version.Should().Be(firstUpdateVersion + 1);
		article.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void AssignAndRemoveCategoryChangeCategoryAndIncrementVersion()
	{
		// Arrange
		var article = Article.Create("Post", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var firstCategoryId = ObjectId.GenerateNewId();
		var secondCategoryId = ObjectId.GenerateNewId();

		// Act
		article.AssignCategory(firstCategoryId);
		article.AssignCategory(secondCategoryId);
		article.RemoveCategory();

		// Assert
		article.CategoryId.Should().BeNull();
		article.Version.Should().Be(3);
	}
}