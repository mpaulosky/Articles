// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleListFilterTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using FluentAssertions;

using MongoDB.Bson;

using Web.Components.Features.Articles.Models;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Models;

namespace Web.Tests.Features.Articles.Models;

public class ArticleListFilterTests
{
	[Fact]
	public void Apply_WithNoCriteria_ReturnsOnlyUnarchivedArticles()
	{
		// Arrange
		var articles = new[]
		{
			CreateArticle("Published", "user-1", isArchived: false),
			CreateArticle("Archived", "user-1", isArchived: true)
		};
		var filter = new ArticleListFilter();

		// Act
		var result = filter.Apply(articles);

		// Assert
		result.Select(a => a.Title).Should().Equal("Published");
	}

	[Fact]
	public void Apply_IncludeArchivedTrue_ReturnsArchivedArticlesToo()
	{
		// Arrange
		var articles = new[]
		{
			CreateArticle("Published", "user-1", isArchived: false),
			CreateArticle("Archived", "user-1", isArchived: true)
		};
		var filter = new ArticleListFilter { IncludeArchived = true };

		// Act
		var result = filter.Apply(articles);

		// Assert
		result.Select(a => a.Title).Should().BeEquivalentTo(["Published", "Archived"]);
	}

	[Fact]
	public void Apply_ShowMyArticlesOnly_FiltersToCurrentUser()
	{
		// Arrange
		var articles = new[]
		{
			CreateArticle("Mine", "user-1"),
			CreateArticle("Someone else's", "user-2")
		};
		var filter = new ArticleListFilter { ShowMyArticlesOnly = true, CurrentUserId = "user-1" };

		// Act
		var result = filter.Apply(articles);

		// Assert
		result.Select(a => a.Title).Should().Equal("Mine");
	}

	[Fact]
	public void Apply_SearchText_MatchesTitleOrAuthorCaseInsensitively()
	{
		// Arrange
		var articles = new[]
		{
			CreateArticle("Docker Basics", "user-1", authorName: "Alice"),
			CreateArticle("Kubernetes 101", "user-1", authorName: "Bob"),
			CreateArticle("Networking", "user-1", authorName: "docker fan")
		};
		var filter = new ArticleListFilter { SearchText = "docker" };

		// Act
		var result = filter.Apply(articles);

		// Assert
		result.Select(a => a.Title).Should().BeEquivalentTo(["Docker Basics", "Networking"]);
	}

	[Fact]
	public void Apply_TitleFilter_MatchesTitleOnly()
	{
		// Arrange
		var articles = new[]
		{
			CreateArticle("Alpha", "user-1", authorName: "Beta"),
			CreateArticle("Gamma", "user-1", authorName: "Alpha")
		};
		var filter = new ArticleListFilter { TitleFilter = "alpha" };

		// Act
		var result = filter.Apply(articles);

		// Assert
		result.Select(a => a.Title).Should().Equal("Alpha");
	}

	[Fact]
	public void Apply_AuthorFilter_MatchesAuthorOnly()
	{
		// Arrange
		var articles = new[]
		{
			CreateArticle("Alpha", "user-1", authorName: "Beta"),
			CreateArticle("Gamma", "user-1", authorName: "Alpha")
		};
		var filter = new ArticleListFilter { AuthorFilter = "alpha" };

		// Act
		var result = filter.Apply(articles);

		// Assert
		result.Select(a => a.Title).Should().Equal("Gamma");
	}

	[Fact]
	public void Apply_CategoryFilter_MatchesCategoryId()
	{
		// Arrange
		var wantedCategory = new CategoryDto { Id = ObjectId.GenerateNewId(), CategoryName = "Wanted" };
		var otherCategory = new CategoryDto { Id = ObjectId.GenerateNewId(), CategoryName = "Other" };
		var articles = new[]
		{
			CreateArticle("Wanted article", "user-1", category: wantedCategory),
			CreateArticle("Other article", "user-1", category: otherCategory)
		};
		var filter = new ArticleListFilter { CategoryFilter = wantedCategory.Id.ToString() };

		// Act
		var result = filter.Apply(articles);

		// Assert
		result.Select(a => a.Title).Should().Equal("Wanted article");
	}

	[Theory]
	[InlineData("Published", "Published Article")]
	[InlineData("Draft", "Draft Article")]
	public void Apply_StatusFilter_MatchesPublishedOrDraft(string statusFilter, string expectedTitle)
	{
		// Arrange
		var articles = new[]
		{
			CreateArticle("Published Article", "user-1", isPublished: true),
			CreateArticle("Draft Article", "user-1", isPublished: false)
		};
		var filter = new ArticleListFilter { StatusFilter = statusFilter };

		// Act
		var result = filter.Apply(articles);

		// Assert
		result.Select(a => a.Title).Should().Equal(expectedTitle);
	}

	[Fact]
	public void Apply_StatusFilterAll_ReturnsBothPublishedAndDraft()
	{
		// Arrange
		var articles = new[]
		{
			CreateArticle("Published Article", "user-1", isPublished: true),
			CreateArticle("Draft Article", "user-1", isPublished: false)
		};
		var filter = new ArticleListFilter { StatusFilter = "All" };

		// Act
		var result = filter.Apply(articles);

		// Assert
		result.Should().HaveCount(2);
	}

	[Fact]
	public void Apply_CombinesAllCriteria()
	{
		// Arrange
		var articles = new[]
		{
			CreateArticle("Docker Basics", "user-1", authorName: "Alice", isPublished: true),
			CreateArticle("Docker Advanced", "user-2", authorName: "Bob", isPublished: true),
			CreateArticle("Docker Draft", "user-1", authorName: "Alice", isPublished: false)
		};
		var filter = new ArticleListFilter
		{
			SearchText = "docker",
			ShowMyArticlesOnly = true,
			CurrentUserId = "user-1",
			StatusFilter = "Published"
		};

		// Act
		var result = filter.Apply(articles);

		// Assert
		result.Select(a => a.Title).Should().Equal("Docker Basics");
	}

	private static ArticleDto CreateArticle(string title, string authorId, bool isPublished = true,
		bool isArchived = false, string authorName = "Test Author", CategoryDto? category = null)
	{
		return new ArticleDto(
			Id: ObjectId.GenerateNewId().ToString(),
			Title: title,
			Slug: "test-slug",
			Content: "Test content",
			Author: new AuthorDto(authorId, authorName),
			Category: category ?? new CategoryDto
			{
				CategoryName = "Test Category", Slug = "test-category", Description = "Test description"
			},
			CreatedAt: DateTime.UtcNow,
			UpdatedAt: DateTime.UtcNow,
			IsPublished: isPublished,
			PublishedOn: isPublished ? DateTime.UtcNow : null,
			IsArchived: isArchived
		);
	}
}
