using Bunit;

using FluentAssertions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using PSC.Blazor.Components.MarkdownEditor;
using PSC.Blazor.Components.MarkdownEditor.EventsArgs;
using PSC.Blazor.Components.MarkdownEditor.Models;

using Web.Components.Features.Articles.Components;
using Web.Components.Features.Articles.Services;

namespace Web.UI.Tests.Features.Articles.Components;

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
		result.UploadUrl.Should().Be("http://localhost/uploads/generated-name.png");
	}

	[Fact]
	public async Task UploadingChanged_StaysTrue_UntilTheInsertedImageReachesContent()
	{
		// Arrange
		Services.AddSingleton<IFileStorage>(new FakeFileStorage("stored.png"));
		var uploadingStates = new List<bool>();
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, true)
			.Add(p => p.Content, string.Empty)
			.Add(p => p.UploadingChanged, EventCallback.Factory.Create<bool>(this, v => uploadingStates.Add(v))));
		var markdownEditor = cut.FindComponent<MarkdownEditor>().Instance;
		var file = new FileEntry { Name = "upload.png" };

		// Act: upload starts (server writes the file, MarkdownEditor's own internal state is
		// still stale) - the caller must not be able to submit yet.
		await markdownEditor.ImageUploadStarted!.Invoke(new FileStartedEventArgs(file));

		// Assert: still marked uploading even after the vendor's "ended" callback fires, because
		// that fires *before* the image markdown is actually inserted into the editor content.
		await markdownEditor.ImageUploadEnded!.Invoke(new FileEndedEventArgs(file, true, null));
		uploadingStates.Should().Equal(true);

		// Act: the insertion finally lands, driving MyContent the same way EasyMDE's "change"
		// event would.
		cut.Instance.MyContent = "![](/uploads/stored.png)";

		// Assert
		uploadingStates.Should().Equal(true, false);
	}

	[Fact]
	public async Task UploadingChanged_ClearsImmediately_WhenUploadFails()
	{
		// Arrange
		Services.AddSingleton<IFileStorage>(new FakeFileStorage("stored.png"));
		var uploadingStates = new List<bool>();
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, true)
			.Add(p => p.UploadingChanged, EventCallback.Factory.Create<bool>(this, v => uploadingStates.Add(v))));
		var markdownEditor = cut.FindComponent<MarkdownEditor>().Instance;
		var file = new FileEntry { Name = "upload.png" };

		// Act
		await markdownEditor.ImageUploadStarted!.Invoke(new FileStartedEventArgs(file));
		await markdownEditor.ImageUploadEnded!.Invoke(new FileEndedEventArgs(file, false, "boom"));

		// Assert: no insertion is coming, so don't leave the caller stuck disabled forever.
		uploadingStates.Should().Equal(true, false);
	}

	[Fact]
	public async Task RemovingAnUploadedImageFromContent_DeletesItFromStorage()
	{
		// Arrange
		var fakeStorage = new FakeFileStorage("stored.png");
		Services.AddSingleton<IFileStorage>(fakeStorage);
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, true)
			.Add(p => p.Content, string.Empty));
		var file = new FileEntry { Name = "upload.png", ContentBase64 = Convert.ToBase64String([1, 2, 3, 4]) };
		await cut.Instance.HandleImageUpload(null!, file);

		// Act: the image lands in content, then the user removes it before saving.
		cut.Instance.MyContent = "![](/uploads/stored.png)";
		await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
		cut.Instance.MyContent = string.Empty;
		await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);

		// Assert
		fakeStorage.DeletedFileNames.Should().Equal("stored.png");
	}

	[Fact]
	public async Task ReplacingAnUploadedImage_DeletesOnlyTheReplacedOne()
	{
		// Arrange
		var fakeStorage = new SequencedFakeFileStorage("first.png", "second.png");
		Services.AddSingleton<IFileStorage>(fakeStorage);
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, true)
			.Add(p => p.Content, string.Empty));
		var file = new FileEntry { Name = "upload.png", ContentBase64 = Convert.ToBase64String([1, 2, 3, 4]) };

		// Act: upload a first image, insert it, then upload and swap in a second image.
		await cut.Instance.HandleImageUpload(null!, file);
		cut.Instance.MyContent = "![](/uploads/first.png)";
		await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
		await cut.Instance.HandleImageUpload(null!, file);
		cut.Instance.MyContent = "![](/uploads/second.png)";
		await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);

		// Assert: the replaced image is gone, the current one is untouched.
		fakeStorage.DeletedFileNames.Should().Equal("first.png");
	}

	[Fact]
	public void RemovingContentThatWasNeverUploadedThisSession_DoesNotDeleteAnything()
	{
		// Arrange: mirrors editing an existing article - its saved images were never uploaded
		// through this component instance, so this editor must never delete them speculatively.
		var fakeStorage = new FakeFileStorage("stored.png");
		Services.AddSingleton<IFileStorage>(fakeStorage);
		var cut = Render<TextEditor>(parameters => parameters
			.Add(p => p.AlignmentOptionsEnabled, true)
			.Add(p => p.Content, "![](/uploads/already-saved.png)"));

		// Act
		cut.Instance.MyContent = string.Empty;

		// Assert
		fakeStorage.DeletedFileNames.Should().BeEmpty();
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

		public List<string> DeletedFileNames { get; } = [];

		public Task<string> AddFile(FileData fileData)
		{
			LastUploadedFileName = fileData.MetaData.Name;
			return Task.FromResult(fileNameToReturn);
		}

		public Task DeleteFile(string fileName)
		{
			DeletedFileNames.Add(fileName);
			return Task.CompletedTask;
		}
	}

	private sealed class SequencedFakeFileStorage(params string[] fileNamesToReturn) : IFileStorage
	{
		private int _callCount;

		public List<string> DeletedFileNames { get; } = [];

		public Task<string> AddFile(FileData fileData)
		{
			return Task.FromResult(fileNamesToReturn[_callCount++]);
		}

		public Task DeleteFile(string fileName)
		{
			DeletedFileNames.Add(fileName);
			return Task.CompletedTask;
		}
	}
}
