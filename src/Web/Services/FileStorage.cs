namespace Web.Services
{
	public interface IFileStorage
	{
		Task<string> AddFile(FileData fileData);
	}

	public class FileStorage : IFileStorage
	{
		private readonly IWebHostEnvironment _environment;
		private readonly ILogger<FileStorage> _logger;

		public FileStorage(IWebHostEnvironment environment, ILogger<FileStorage> logger)
		{
			_environment = environment;
			_logger = logger;
		}

		public async Task<string> AddFile(FileData fileData)
		{
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
				var extension = Path.GetExtension(fileData.MetaData.Name).ToLowerInvariant();
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
				if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
				{
					throw new InvalidOperationException("File type not allowed. Only images are permitted.");
				}

				// Create uploads directory if it doesn't exist
				var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
				Directory.CreateDirectory(uploadsPath);

				// Generate a unique filename to prevent collisions
				var uniqueFileName = $"{Guid.NewGuid()}{extension}";
				var filePath = Path.Combine(uploadsPath, uniqueFileName);

				// Save the file
				await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
				{
					await fileData.Content.CopyToAsync(fileStream);
				}

				_logger.LogInformation("File saved successfully: {FileName}", uniqueFileName);
				return uniqueFileName;
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning(ex, "Validation error saving file: {FileName}", fileData.MetaData.Name);
				throw;
			}
			catch (IOException ex)
			{
				_logger.LogError(ex, "IO error saving file: {FileName}", fileData.MetaData.Name);
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error saving file: {FileName}", fileData.MetaData.Name);
				throw;
			}
		}
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
}
