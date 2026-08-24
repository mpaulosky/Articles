// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleImageTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using FluentAssertions;

using Web.Components.Features.Articles.Models;

namespace Web.Tests.Features.Articles.Models;

public class ArticleImageTests
{
	[Fact]
	public void CreateSetsExpectedState()
	{
		// Arrange
		var uploadedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		// Act
		var image = ArticleImage.Create(
			"abc123.png", "/uploads/abc123.png", 1024, "image/png", "A diagram", uploadedAt);

		// Assert
		image.FileName.Should().Be("abc123.png");
		image.Url.Should().Be("/uploads/abc123.png");
		image.SizeInBytes.Should().Be(1024);
		image.MimeType.Should().Be("image/png");
		image.AltText.Should().Be("A diagram");
		image.UploadedAt.Should().Be(uploadedAt);
	}

	[Fact]
	public void CreateThrowsForBlankFileNameOrUrl()
	{
		// Act
		Action actWithBlankFileName = () =>
			ArticleImage.Create("   ", "/uploads/abc123.png", 1024, "image/png", "alt", DateTime.UtcNow);
		Action actWithBlankUrl = () =>
			ArticleImage.Create("abc123.png", "   ", 1024, "image/png", "alt", DateTime.UtcNow);

		// Assert
		actWithBlankFileName.Should().Throw<ArgumentException>();
		actWithBlankUrl.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void CreateDefaultsNullMimeTypeAndAltTextToEmpty()
	{
		// Act
		var image = ArticleImage.Create(
			"abc123.png", "/uploads/abc123.png", 1024, null!, null!, DateTime.UtcNow);

		// Assert
		image.MimeType.Should().BeEmpty();
		image.AltText.Should().BeEmpty();
	}
}
