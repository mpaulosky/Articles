using Bunit;

using FluentAssertions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using PSC.Blazor.Components.MarkdownEditor.EventsArgs;
using PSC.Blazor.Components.MarkdownEditor.Models;

using Web.Components.Shared;
using Web.Services;

namespace Web.UI.Tests.Shared;

public class TextEditorTests : BunitContext
{
	public TextEditorTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void RendersWithAlignmentOptionsEnabled()
	{
		// Arrange
		Services.AddSingleton<IFileStorage>(new FakeFileStorage("stored.png"));

		// Act
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, true));

		// Assert
		cut.Instance.AlignmentOptionsEnabled.Should().BeTrue();
		cut.Markup.Should().Contain("textarea");
	}

	[Fact]
	public void RendersWithAlignmentOptionsDisabled()
	{
		// Arrange
		Services.AddSingleton<IFileStorage>(new FakeFileStorage("stored.png"));

		// Act
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, false));

		// Assert
		cut.Instance.AlignmentOptionsEnabled.Should().BeFalse();
		cut.Markup.Should().Contain("textarea");
	}

	[Fact]
	public void MyContentSetterUpdatesContentAndRaisesContentChanged()
	{
		// Arrange
		Services.AddSingleton<IFileStorage>(new FakeFileStorage("stored.png"));
		string? raisedValue = null;
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, true)
			.Add(p => p.Content, "initial")
			.Add(p => p.ContentChanged, EventCallback.Factory.Create<string>(this, value => raisedValue = value)));

		// Act
		cut.Instance.MyContent = "updated";

		// Assert
		cut.Instance.Content.Should().Be("updated");
		raisedValue.Should().Be("updated");
	}

	[Fact]
	public async Task HandleImageUploadStoresTheFileAndBuildsAnUploadUrl()
	{
		// Arrange
		var fakeStorage = new FakeFileStorage("generated-name.png");
		Services.AddSingleton<IFileStorage>(fakeStorage);
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, true));
		var file = new FileEntry
		{
			Name = "upload.png",
			ContentBase64 = Convert.ToBase64String([1, 2, 3, 4]),
			LastModified = DateTime.UtcNow,
		};

		// Act
		var result = await cut.Instance.HandleImageUpload(null!, file);

		// Assert
		fakeStorage.LastUploadedFileName.Should().Be("upload.png");
		result.UploadUrl.Should().Be("http://localhost/api/files/generated-name.png");
	}

	[Fact]
	public async Task OnCustomButtonClickedDoesNothingWhenNoTextIsSelected()
	{
		// Arrange
		Services.AddSingleton<IFileStorage>(new FakeFileStorage("stored.png"));
		JSInterop.Setup<string>("getSelectedText", _ => true).SetResult(string.Empty);
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, true)
			.Add(p => p.Content, "original"));

		// Act
		await cut.Instance.OnCustomButtonClicked(new MarkdownButtonEventArgs("align", "left"));

		// Assert
		cut.Instance.Content.Should().Be("original");
	}

	[Fact]
	public async Task OnCustomButtonClickedWrapsSelectedTextWithAlignmentDiv()
	{
		// Arrange
		Services.AddSingleton<IFileStorage>(new FakeFileStorage("stored.png"));
		JSInterop.Setup<string>("getSelectedText", _ => true).SetResult("hello");
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, true)
			.Add(p => p.Content, "hello world"));

		// Act
		await cut.Instance.OnCustomButtonClicked(new MarkdownButtonEventArgs("align", "left"));

		// Assert
		cut.Instance.Content.Should().Be("<div style='text-align:left'>hello</div> world");
	}

	private sealed class FakeFileStorage(string fileNameToReturn) : IFileStorage
	{
		public string? LastUploadedFileName { get; private set; }

		public Task<string> AddFile(FileData fileData)
		{
			LastUploadedFileName = fileData.MetaData.Name;
			return Task.FromResult(fileNameToReturn);
		}
	}
}
