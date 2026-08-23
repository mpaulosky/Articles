// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleActionsCellTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.UI.Tests
// =============================================

using Bunit;

using FluentAssertions;

using Microsoft.AspNetCore.Components;

using MongoDB.Bson;

using System.Security.Claims;

using Web.Components.Features.Articles.Components;
using Web.Components.Features.Articles.Models;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Models;

namespace Web.UI.Tests.Features.Articles.Components;

public class ArticleActionsCellTests : BunitContext
{
	[Fact]
	public void RendersViewLink_PointingToArticleSlug()
	{
		// Arrange
		var article = CreateArticle();

		// Act
		var cut = Render(article, CreateAdminUser());

		// Assert
		cut.Find($"a[href='/articles/{article.Slug}']").TextContent.Trim().Should().Be("View");
	}

	[Fact]
	public void ShowsEditLink_WhenUserCanEditArticle()
	{
		// Arrange
		var article = CreateArticle(authorId: "admin1");

		// Act
		var cut = Render(article, CreateAdminUser());

		// Assert
		cut.Find($"a[href='/articles/{article.Slug}/edit']").TextContent.Trim().Should().Be("Edit");
	}

	[Fact]
	public void ShowsDisabledEditSpan_WhenUserCannotEditArticle()
	{
		// Arrange
		var article = CreateArticle(authorId: "author2");

		// Act
		var cut = Render(article, CreateAuthorUser("author1"));

		// Assert
		cut.FindAll("a").Should().NotContain(a => a.TextContent.Trim() == "Edit");
		var disabledEdit = cut.FindAll("span").Single(s => s.TextContent.Trim() == "Edit");
		disabledEdit.GetAttribute("aria-disabled").Should().Be("true");
	}

	[Fact]
	public void ClickingPublish_InvokesOnPublish_WithArticleId()
	{
		// Arrange
		var article = CreateArticle(authorId: "admin1", isPublished: false);
		string? publishedId = null;
		var cut = Render(article, CreateAdminUser(), onPublish: id => publishedId = id);

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Publish").Click();

		// Assert
		publishedId.Should().Be(article.Id);
	}

	[Fact]
	public void ClickingUnpublish_InvokesOnUnpublish_WithArticleId()
	{
		// Arrange
		var article = CreateArticle(authorId: "admin1", isPublished: true);
		string? unpublishedId = null;
		var cut = Render(article, CreateAdminUser(), onUnpublish: id => unpublishedId = id);

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Unpublish").Click();

		// Assert
		unpublishedId.Should().Be(article.Id);
	}

	[Fact]
	public void PublishButton_IsDisabled_WhenUserCannotEditArticle()
	{
		// Arrange
		var article = CreateArticle(authorId: "author2", isPublished: false);

		// Act
		var cut = Render(article, CreateAuthorUser("author1"));

		// Assert
		var publishButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Publish");
		publishButton.HasAttribute("disabled").Should().BeTrue();
	}

	[Fact]
	public void ClickingArchiveRequested_InvokesOnArchiveRequested_WithArticleId()
	{
		// Arrange
		var article = CreateArticle(authorId: "admin1", isArchived: false);
		string? requestedId = null;
		var cut = Render(article, CreateAdminUser(), onArchiveRequested: id => requestedId = id);

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Archive").Click();

		// Assert
		requestedId.Should().Be(article.Id);
	}

	[Fact]
	public void ClickingUnarchive_InvokesOnUnarchive_WithArticleId()
	{
		// Arrange
		var article = CreateArticle(authorId: "admin1", isArchived: true);
		string? unarchivedId = null;
		var cut = Render(article, CreateAdminUser(), onUnarchive: id => unarchivedId = id);

		// Act
		cut.FindAll("button").First(b => b.TextContent.Trim() == "Unarchive").Click();

		// Assert
		unarchivedId.Should().Be(article.Id);
	}

	[Fact]
	public void ArchiveButton_IsDisabled_ForNonAdminUser()
	{
		// Arrange
		var article = CreateArticle(authorId: "author1", isArchived: false);

		// Act
		var cut = Render(article, CreateAuthorUser("author1"));

		// Assert
		var archiveButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Archive");
		archiveButton.HasAttribute("disabled").Should().BeTrue();
	}

	private IRenderedComponent<ArticleActionsCell> Render(
		ArticleDto article,
		ClaimsPrincipal user,
		Action<string>? onPublish = null,
		Action<string>? onUnpublish = null,
		Action<string>? onArchiveRequested = null,
		Action<string>? onUnarchive = null)
	{
		return Render<ArticleActionsCell>(parameters => parameters
			.Add(p => p.Article, article)
			.Add(p => p.CurrentUser, user)
			.Add(p => p.OnPublish, EventCallback.Factory.Create(this, onPublish ?? (_ => { })))
			.Add(p => p.OnUnpublish, EventCallback.Factory.Create(this, onUnpublish ?? (_ => { })))
			.Add(p => p.OnArchiveRequested, EventCallback.Factory.Create(this, onArchiveRequested ?? (_ => { })))
			.Add(p => p.OnUnarchive, EventCallback.Factory.Create(this, onUnarchive ?? (_ => { }))));
	}

	private static ClaimsPrincipal CreateAdminUser()
	{
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "admin1"), new Claim(ClaimTypes.Name, "Admin User"),
			new Claim(ClaimTypes.Role, "Admin")
		};
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
	}

	private static ClaimsPrincipal CreateAuthorUser(string userId)
	{
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Name, "Author User"),
			new Claim(ClaimTypes.Role, "Author")
		};
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
	}

	private static ArticleDto CreateArticle(
		string authorId = "admin1",
		bool isPublished = true,
		bool isArchived = false)
	{
		return new ArticleDto(
			Id: ObjectId.GenerateNewId().ToString(),
			Title: "Test Article",
			Slug: "test-article",
			Content: "Test content",
			Author: new AuthorDto(authorId, "Test Author"),
			Category: new CategoryDto
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
