using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Web.Components.Features.Articles.Services;

namespace Web.Tests.Features.Articles.Services;

public sealed class FileStorageTests : IDisposable
{
	private readonly string _webRootPath;

	public FileStorageTests()
	{
		_webRootPath = Path.Combine(Path.GetTempPath(), $"file-storage-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(_webRootPath);
	}

	[Fact]
	public async Task AddFileSavesTheFileAndReturnsAGeneratedFileName()
	{
		// Arrange
		var storage = CreateStorage(_webRootPath);
		var fileData = CreatePngFileData("photo.png");

		// Act
		var savedFileName = await storage.AddFile(fileData);

		// Assert
		savedFileName.Should().EndWith(".png");
		File.Exists(Path.Combine(_webRootPath, "uploads", savedFileName)).Should().BeTrue();
	}

	[Fact]
	public async Task AddFileThrowsWhenWebRootPathIsNotConfigured()
	{
		// Arrange
		var storage = CreateStorage(string.Empty);
		var fileData = CreatePngFileData("photo.png");

		// Act
		Func<Task> act = async () => await storage.AddFile(fileData).ConfigureAwait(false);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("WebRootPath is not configured");
	}

	[Fact]
	public async Task AddFileThrowsWhenFileExceedsMaximumSize()
	{
		// Arrange
		var storage = CreateStorage(_webRootPath);
		var oversizedContent = new MemoryStream(new byte[(10 * 1024 * 1024) + 1]);
		var fileData = new FileData(oversizedContent, new FileMetaData("big.png", "image/png", DateTime.UtcNow));

		// Act
		Func<Task> act = async () => await storage.AddFile(fileData).ConfigureAwait(false);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("File exceeds maximum allowed size of 10 MB");
	}

	[Theory]
	[InlineData("photo.exe")]
	[InlineData("photo")]
	[InlineData("photo.txt")]
	public async Task AddFileThrowsWhenFileExtensionIsNotAnAllowedImageType(string fileName)
	{
		// Arrange
		var storage = CreateStorage(_webRootPath);
		var fileData = CreatePngFileData(fileName);

		// Act
		Func<Task> act = async () => await storage.AddFile(fileData).ConfigureAwait(false);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("File type not allowed. Only images are permitted.");
	}

	[Fact]
	public async Task AddFileRethrowsIOExceptionWhenUploadsDirectoryCannotBeCreated()
	{
		// Arrange
		var storage = CreateStorage(_webRootPath);
		File.WriteAllBytes(Path.Combine(_webRootPath, "uploads"), []);
		var fileData = CreatePngFileData("photo.png");

		// Act
		Func<Task> act = async () => await storage.AddFile(fileData).ConfigureAwait(false);

		// Assert
		await act.Should().ThrowAsync<IOException>();
	}

	[Fact]
	public async Task AddFileRethrowsUnexpectedExceptionsFromTheContentStream()
	{
		// Arrange
		var storage = CreateStorage(_webRootPath);
		var fileData = new FileData(new ThrowingStream(), new FileMetaData("photo.png", "image/png", DateTime.UtcNow));

		// Act
		Func<Task> act = async () => await storage.AddFile(fileData).ConfigureAwait(false);

		// Assert
		await act.Should().ThrowAsync<NotSupportedException>();
	}

	[Fact]
	public async Task AddFileGeneratesAUniqueFileNameForEachUpload()
	{
		// Arrange
		var storage = CreateStorage(_webRootPath);

		// Act
		var firstFileName = await storage.AddFile(CreatePngFileData("photo.png"));
		var secondFileName = await storage.AddFile(CreatePngFileData("photo.png"));

		// Assert
		firstFileName.Should().NotBe(secondFileName);
	}

	[Fact]
	public async Task DeleteFileRemovesAPreviouslySavedFile()
	{
		// Arrange
		var storage = CreateStorage(_webRootPath);
		var savedFileName = await storage.AddFile(CreatePngFileData("photo.png"));

		// Act
		await storage.DeleteFile(savedFileName);

		// Assert
		File.Exists(Path.Combine(_webRootPath, "uploads", savedFileName)).Should().BeFalse();
	}

	[Fact]
	public async Task DeleteFileIsANoOpWhenTheFileDoesNotExist()
	{
		// Arrange
		var storage = CreateStorage(_webRootPath);

		// Act
		Func<Task> act = async () => await storage.DeleteFile("missing.png").ConfigureAwait(false);

		// Assert
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task DeleteFileIgnoresDirectoryTraversalInTheFileName()
	{
		// Arrange: a file that legitimately lives outside the uploads/ directory.
		var storage = CreateStorage(_webRootPath);
		var outsideFile = Path.Combine(_webRootPath, "sentinel.txt");
		await File.WriteAllTextAsync(outsideFile, "do not delete me", TestContext.Current.CancellationToken);

		// Act: a crafted name trying to escape the uploads/ folder.
		await storage.DeleteFile("../sentinel.txt");

		// Assert: Path.GetFileName strips the traversal segment down to "sentinel.txt", so this
		// only ever looks inside uploads/ - which has no such file - leaving the real one alone.
		File.Exists(outsideFile).Should().BeTrue();
	}

	private static FileStorage CreateStorage(string webRootPath)
	{
		var environment = Substitute.For<IWebHostEnvironment>();
		environment.WebRootPath.Returns(webRootPath);
		var logger = Substitute.For<ILogger<FileStorage>>();
		var imageOptimizer = new ImageOptimizer(Substitute.For<ILogger<ImageOptimizer>>());
		return new FileStorage(environment, logger, imageOptimizer);
	}

	private static FileData CreatePngFileData(string fileName)
	{
		var content = new MemoryStream([1, 2, 3, 4]);
		return new FileData(content, new FileMetaData(fileName, "image/png", DateTime.UtcNow));
	}

	public void Dispose()
	{
		if (Directory.Exists(_webRootPath))
		{
			Directory.Delete(_webRootPath, recursive: true);
		}
	}

	private sealed class ThrowingStream : MemoryStream
	{
		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException("Simulated unexpected failure");
		}

		public override void CopyTo(Stream destination, int bufferSize)
		{
			throw new NotSupportedException("Simulated unexpected failure");
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("Simulated unexpected failure");
		}
	}
}
