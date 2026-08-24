using FluentAssertions;

using Web.Components.Features.Articles.Models;
using Web.Components.Features.Articles.Services;

namespace Web.Tests.Features.Articles.Services;

public sealed class ArticleImageParserTests
{
	[Fact]
	public void Parse_ReturnsEmptyList_ForNullOrEmptyContent()
	{
		ArticleImageParser.Parse(null, []).Should().BeEmpty();
		ArticleImageParser.Parse(string.Empty, []).Should().BeEmpty();
	}

	[Fact]
	public void Parse_FindsEveryUploadedImageWithItsAltText()
	{
		var content = "Before ![](https://example.com/uploads/a1b2.jpg) middle " +
			"![second image](/uploads/c3d4.png \"title\") after";

		var result = ArticleImageParser.Parse(content, []);

		result.Should().SatisfyRespectively(
			first =>
			{
				first.FileName.Should().Be("a1b2.jpg");
				first.Url.Should().Be("https://example.com/uploads/a1b2.jpg");
				first.AltText.Should().BeEmpty();
			},
			second =>
			{
				second.FileName.Should().Be("c3d4.png");
				second.Url.Should().Be("/uploads/c3d4.png");
				second.AltText.Should().Be("second image");
			});
	}

	[Fact]
	public void Parse_IgnoresContentWithoutUploadedImageReferences()
	{
		ArticleImageParser.Parse("Plain text with a [link](https://example.com) only.", []).Should().BeEmpty();
	}

	[Fact]
	public void Parse_DeduplicatesRepeatedReferencesToTheSameUrl()
	{
		var content = "![](https://example.com/uploads/a.jpg) again ![alt text](https://example.com/uploads/a.jpg)";

		ArticleImageParser.Parse(content, []).Should().ContainSingle();
	}

	[Fact]
	public void Parse_ReusesMetadataFromAMatchingPreviousImage()
	{
		var uploadedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var previous = ArticleImage.Create("a.jpg", "https://example.com/uploads/a.jpg", 1234, "image/jpeg", "old alt",
			uploadedAt);

		var result = ArticleImageParser.Parse("![new alt](https://example.com/uploads/a.jpg)", [previous]);

		result.Should().ContainSingle().Which.Should().BeEquivalentTo(new
		{
			FileName = "a.jpg",
			SizeInBytes = 1234L,
			MimeType = "image/jpeg",
			AltText = "new alt",
			UploadedAt = uploadedAt
		});
	}

	[Fact]
	public void Parse_DefaultsMetadataForANewlyReferencedImage()
	{
		var result = ArticleImageParser.Parse("![](https://example.com/uploads/new.jpg)", []);

		result.Should().ContainSingle().Which.Should().BeEquivalentTo(new
		{
			FileName = "new.jpg",
			SizeInBytes = 0L,
			MimeType = string.Empty
		});
	}

	[Fact]
	public void FindRemoved_ReturnsFileNamesOnlyInPreviousImages()
	{
		var kept = ArticleImage.Create("kept.jpg", "/uploads/kept.jpg", 0, string.Empty, string.Empty, DateTime.UtcNow);
		var removed = ArticleImage.Create("removed.jpg", "/uploads/removed.jpg", 0, string.Empty, string.Empty,
			DateTime.UtcNow);

		ArticleImageParser.FindRemoved([kept, removed], [kept]).Should().Equal("removed.jpg");
	}

	[Fact]
	public void FindRemoved_ReturnsEmpty_WhenNothingWasRemoved()
	{
		var image = ArticleImage.Create("kept.jpg", "/uploads/kept.jpg", 0, string.Empty, string.Empty, DateTime.UtcNow);

		ArticleImageParser.FindRemoved([image], [image]).Should().BeEmpty();
	}

	[Fact]
	public void FindRemoved_TreatsEmptyUpdatedImagesAsEverythingRemoved()
	{
		var a = ArticleImage.Create("a.jpg", "/uploads/a.jpg", 0, string.Empty, string.Empty, DateTime.UtcNow);
		var b = ArticleImage.Create("b.jpg", "/uploads/b.jpg", 0, string.Empty, string.Empty, DateTime.UtcNow);

		ArticleImageParser.FindRemoved([a, b], []).Should().BeEquivalentTo(["a.jpg", "b.jpg"]);
	}
}
