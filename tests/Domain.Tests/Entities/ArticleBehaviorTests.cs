// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

using Domain.Models;

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
		article.Category.Id.Should().Be(ObjectId.Empty);
		article.Category.CategoryName.Should().BeEmpty();
		article.Category.Slug.Should().BeEmpty();
		article.Category.IsArchived.Should().BeFalse();
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
	public void CreateInitializesCategoryAsEmpty()
	{
		// Arrange
		var author = new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>());

		// Act
		var article = Article.Create("Post", "Body", author);

		// Assert
		article.Category.Id.Should().Be(ObjectId.Empty);
		article.Category.CategoryName.Should().BeEmpty();
		article.Category.Slug.Should().BeEmpty();
		article.Category.IsArchived.Should().BeFalse();
	}

	[Fact]
	public void AssignCategorySetsCategoryAndUpdatesTimestamp()
	{
		// Arrange
		var article = Article.Create("Post", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology"
		};

		// Act
		var originalUpdatedAt = article.UpdatedAt;
		article.AssignCategory(category);

		// Assert
		article.Category.Should().Be(category);
		article.Category.Id.Should().Be(category.Id);
		article.Category.CategoryName.Should().Be("Technology");
		article.UpdatedAt.Should().NotBeNull();
		article.UpdatedAt.Should().BeAfter(originalUpdatedAt ?? DateTime.MinValue);
	}

	[Fact]
	public void AssignCategoryReplacesExistingCategory()
	{
		// Arrange
		var article = Article.Create("Post", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var firstCategory = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology"
		};
		var secondCategory = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Science",
			Slug = "science"
		};

		// Act
		article.AssignCategory(firstCategory);
		var firstAssignmentCategory = article.Category;
		article.AssignCategory(secondCategory);

		// Assert
		firstAssignmentCategory.Should().Be(firstCategory);
		article.Category.Should().Be(secondCategory);
		article.Category.Id.Should().Be(secondCategory.Id);
		article.Category.CategoryName.Should().Be("Science");
	}

	[Fact]
	public void RemoveCategoryResetsToCategoryEmpty()
	{
		// Arrange
		var article = Article.Create("Post", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology"
		};
		article.AssignCategory(category);

		// Act
		article.RemoveCategory();

		// Assert
		article.Category.Id.Should().Be(ObjectId.Empty);
		article.Category.CategoryName.Should().BeEmpty();
		article.Category.Slug.Should().BeEmpty();
		article.Category.IsArchived.Should().BeFalse();
		article.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void RemoveCategoryUpdatesTimestamp()
	{
		// Arrange
		var article = Article.Create("Post", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology"
		};
		article.AssignCategory(category);
		var afterAssignmentTime = article.UpdatedAt;

		// Act
		System.Threading.Thread.Sleep(10); // Ensure time passes
		article.RemoveCategory();

		// Assert
		article.UpdatedAt.Should().BeAfter(afterAssignmentTime ?? DateTime.MinValue);
	}

	[Fact]
	public void UpdateWithCategoryChangesContentAndCategory()
	{
		// Arrange
		var article = Article.Create("Original", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology"
		};

		// Act
		article.Update("Updated", "Body updated", category: category, clearCategory: false);

		// Assert
		article.Title.Should().Be("Updated");
		article.Content.Should().Be("Body updated");
		article.Category.Should().Be(category);
		article.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void UpdateWithClearCategoryRemovesCategory()
	{
		// Arrange
		var article = Article.Create("Original", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology"
		};
		article.AssignCategory(category);

		// Act
		article.Update("Updated", "Body updated", category: null, clearCategory: true);

		// Assert
		article.Title.Should().Be("Updated");
		article.Content.Should().Be("Body updated");
		article.Category.Id.Should().Be(ObjectId.Empty);
		article.Category.CategoryName.Should().BeEmpty();
		article.Category.Slug.Should().BeEmpty();
		article.Category.IsArchived.Should().BeFalse();
		article.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void UpdatePreservesCategoryWhenNotSpecified()
	{
		// Arrange
		var article = Article.Create("Original", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology"
		};
		article.AssignCategory(category);

		// Act
		article.Update("Updated", "Body updated", category: null, clearCategory: false);

		// Assert
		article.Title.Should().Be("Updated");
		article.Content.Should().Be("Body updated");
		article.Category.Should().Be(category);
	}

	[Fact]
	public void UpdateReplacesExistingCategoryWhenProvided()
	{
		// Arrange
		var article = Article.Create("Original", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var firstCategory = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology"
		};
		var secondCategory = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Science",
			Slug = "science"
		};
		article.AssignCategory(firstCategory);

		// Act
		article.Update("Updated", "Body updated", category: secondCategory, clearCategory: false);

		// Assert
		article.Title.Should().Be("Updated");
		article.Content.Should().Be("Body updated");
		article.Category.Should().Be(secondCategory);
		article.Category.CategoryName.Should().Be("Science");
	}

	[Fact]
	public void UpdateThrowsForInvalidTitleOrContent()
	{
		// Arrange
		var article = Article.Create("Original", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));

		// Act
		Action actWithBlankTitle = () => article.Update("   ", "Body", category: null);
		Action actWithBlankContent = () => article.Update("Title", "   ", category: null);

		// Assert
		actWithBlankTitle.Should().Throw<ArgumentException>();
		actWithBlankContent.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void PublishIdempotentWhenAlreadyPublished()
	{
		// Arrange
		var article = Article.Create("Post", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		article.Publish();
		var firstPublishTime = article.UpdatedAt;

		// Act
		System.Threading.Thread.Sleep(10);
		article.Publish();

		// Assert
		article.IsPublished.Should().BeTrue();
		article.UpdatedAt.Should().Be(firstPublishTime);
	}

	[Fact]
	public void UnpublishIdempotentWhenAlreadyUnpublished()
	{
		// Arrange
		var article = Article.Create("Post", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var creationTime = article.UpdatedAt;

		// Act
		article.Unpublish();

		// Assert
		article.IsPublished.Should().BeFalse();
		article.UpdatedAt.Should().Be(creationTime);
	}

	[Fact]
	public void MultipleOperationsUpdateTimestampEachTime()
	{
		// Arrange
		var article = Article.Create("Post", "Body",
			new PostAuthor("author-1", "Ada", "ada@example.com", Array.Empty<string>()));
		var category = new CategoryDto
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Technology",
			Slug = "technology"
		};

		// Act
		article.AssignCategory(category);
		var afterAssignmentTime = article.UpdatedAt;
		System.Threading.Thread.Sleep(10);
		article.Publish();
		var afterPublishTime = article.UpdatedAt;
		System.Threading.Thread.Sleep(10);
		article.RemoveCategory();
		var afterRemovalTime = article.UpdatedAt;

		// Assert
		afterAssignmentTime.Should().NotBeNull();
		afterPublishTime.Should().BeAfter(afterAssignmentTime ?? DateTime.MinValue);
		afterRemovalTime.Should().BeAfter(afterPublishTime ?? DateTime.MinValue);
	}
}
