using FluentAssertions;

using Web.Components.Features.Articles.Services;

namespace Web.Tests.Features.Articles.Services;

public sealed class UploadedImageReferencesTests
{
	[Fact]
	public void ExtractFileNames_ReturnsEmptySet_ForNullOrEmptyContent()
	{
		UploadedImageReferences.ExtractFileNames(null).Should().BeEmpty();
		UploadedImageReferences.ExtractFileNames(string.Empty).Should().BeEmpty();
	}

	[Fact]
	public void ExtractFileNames_FindsEveryUploadReference()
	{
		var content = "Before ![](https://example.com/uploads/a1b2.jpg) middle " +
			"![second](/uploads/c3d4.png \"title\") after";

		var result = UploadedImageReferences.ExtractFileNames(content);

		result.Should().BeEquivalentTo(["a1b2.jpg", "c3d4.png"]);
	}

	[Fact]
	public void ExtractFileNames_IgnoresContentWithoutUploadReferences()
	{
		UploadedImageReferences.ExtractFileNames("Plain text with a [link](https://example.com) only.").Should().BeEmpty();
	}

	[Fact]
	public void FindRemoved_ReturnsFileNamesOnlyInOldContent()
	{
		var oldContent = "![](https://example.com/uploads/kept.jpg) ![](https://example.com/uploads/removed.jpg)";
		var newContent = "![](https://example.com/uploads/kept.jpg)";

		UploadedImageReferences.FindRemoved(oldContent, newContent).Should().Equal("removed.jpg");
	}

	[Fact]
	public void FindRemoved_ReturnsEmpty_WhenNothingWasRemoved()
	{
		var content = "![](https://example.com/uploads/kept.jpg)";

		UploadedImageReferences.FindRemoved(content, content).Should().BeEmpty();
	}

	[Fact]
	public void FindRemoved_TreatsNullNewContentAsEverythingRemoved()
	{
		var oldContent = "![](https://example.com/uploads/a.jpg) ![](https://example.com/uploads/b.jpg)";

		UploadedImageReferences.FindRemoved(oldContent, null).Should().BeEquivalentTo(["a.jpg", "b.jpg"]);
	}
}
