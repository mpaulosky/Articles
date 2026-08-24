// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     FileStorage.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

namespace Web.Components.Features.Articles.Services;

public interface IFileStorage
{
	Task<string> AddFile(FileData fileData);

	/// <summary>
	///     Deletes a previously uploaded file by its stored name. Idempotent: deleting a file
	///     that doesn't exist (already removed, or never existed) is not an error.
	/// </summary>
	Task DeleteFile(string fileName);
}

public partial class FileStorage : IFileStorage
{
	private readonly IWebHostEnvironment _environment;
	private readonly ILogger<FileStorage> _logger;
	private readonly IImageOptimizer _imageOptimizer;

	public FileStorage(IWebHostEnvironment environment, ILogger<FileStorage> logger, IImageOptimizer imageOptimizer)
	{
		_environment = environment;
		_logger = logger;
		_imageOptimizer = imageOptimizer;
	}

	public async Task<string> AddFile(FileData fileData)
	{
		ArgumentNullException.ThrowIfNull(fileData);

		try
		{
			// Validate WebRootPath is configured
			if (string.IsNullOrEmpty(_environment.WebRootPath))
			{
				throw new InvalidOperationException("WebRootPath is not configured");
			}

			// Validate file size (max 10 MB)
			const long maxFileSize = 10 * 1024 * 1024;
			if (fileData.Content.Length > maxFileSize)
			{
				throw new InvalidOperationException("File exceeds maximum allowed size of 10 MB");
			}

			// Validate file extension
#pragma warning disable CA1308 // Lowercase is the conventional casing for stored file extensions; this is not a security comparison.
			var extension = Path.GetExtension(fileData.MetaData.Name).ToLowerInvariant();
#pragma warning restore CA1308
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
			if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
			{
				throw new InvalidOperationException("File type not allowed. Only images are permitted.");
			}

			// Create uploads directory if it doesn't exist
			var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
			Directory.CreateDirectory(uploadsPath);

			// Resize/recompress oversized images for web delivery; the optimizer may change the
			// extension (e.g. an opaque .png becomes .jpg), so the saved file uses its output.
			var optimized = _imageOptimizer.Optimize(fileData.Content, extension);
			using var optimizedContent = optimized.Content;

			// Generate a unique filename to prevent collisions
			var uniqueFileName = $"{Guid.NewGuid()}{optimized.Extension}";
			var filePath = Path.Combine(uploadsPath, uniqueFileName);

			// Save the file
			var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
			await using (fileStream.ConfigureAwait(false))
			{
				await optimizedContent.CopyToAsync(fileStream).ConfigureAwait(false);
			}

			LogFileSaved(uniqueFileName);
			return uniqueFileName;
		}
		catch (InvalidOperationException ex)
		{
			LogValidationError(ex, fileData.MetaData.Name);
			throw;
		}
		catch (IOException ex)
		{
			LogIOError(ex, fileData.MetaData.Name);
			throw;
		}
		catch (Exception ex)
		{
			LogUnexpectedError(ex, fileData.MetaData.Name);
			throw;
		}
	}

	public Task DeleteFile(string fileName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

		if (string.IsNullOrEmpty(_environment.WebRootPath))
		{
			return Task.CompletedTask;
		}

		// Strip any directory components so a crafted fileName can't escape the uploads folder.
		var safeFileName = Path.GetFileName(fileName);
		var filePath = Path.Combine(_environment.WebRootPath, "uploads", safeFileName);

		try
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
				LogFileDeleted(safeFileName);
			}
		}
		catch (IOException ex)
		{
			LogDeleteError(ex, safeFileName);
		}
		catch (UnauthorizedAccessException ex)
		{
			LogDeleteError(ex, safeFileName);
		}

		return Task.CompletedTask;
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "File saved successfully: {FileName}")]
	private partial void LogFileSaved(string fileName);

	[LoggerMessage(Level = LogLevel.Information, Message = "File deleted: {FileName}")]
	private partial void LogFileDeleted(string fileName);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Validation error saving file: {FileName}")]
	private partial void LogValidationError(Exception ex, string fileName);

	[LoggerMessage(Level = LogLevel.Error, Message = "IO error saving file: {FileName}")]
	private partial void LogIOError(Exception ex, string fileName);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Could not delete file: {FileName}")]
	private partial void LogDeleteError(Exception ex, string fileName);

	[LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error saving file: {FileName}")]
	private partial void LogUnexpectedError(Exception ex, string fileName);
}

public class FileData
{
	public Stream Content { get; }
	public FileMetaData MetaData { get; }

	public FileData(Stream content, FileMetaData metaData)
	{
		Content = content;
		MetaData = metaData;
	}
}

public class FileMetaData
{
	public string Name { get; }
	public string ContentType { get; }
	public DateTime LastModified { get; }

	public FileMetaData(string name, string contentType, DateTime lastModified)
	{
		Name = name;
		ContentType = contentType;
		LastModified = lastModified;
	}
}
