// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ImageOptimizer.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using SkiaSharp;

namespace Web.Components.Features.Articles.Services;

public interface IImageOptimizer
{
	/// <summary>
	///     Resizes and recompresses an uploaded image for web delivery when it exceeds the size
	///     budget, leaving already-small images untouched.
	/// </summary>
	/// <param name="content">The raw image bytes. Read to completion; the caller still owns disposal.</param>
	/// <param name="extension">The file's extension (including the leading dot), e.g. ".png".</param>
	/// <returns>The optimized content and the extension it should be saved with.</returns>
	OptimizedImage Optimize(Stream content, string extension);
}

public sealed record OptimizedImage(MemoryStream Content, string Extension);

public sealed partial class ImageOptimizer(ILogger<ImageOptimizer> logger) : IImageOptimizer
{
	private const int MaxDimension = 1920;
	private const long MaxBytes = 500 * 1024;
	private const int InitialJpegQuality = 85;
	private const int MinJpegQuality = 40;
	private const int JpegQualityStep = 15;
	private const int MaxDimensionReductions = 3;
	private const float DimensionReductionFactor = 0.85f;

	public OptimizedImage Optimize(Stream content, string extension)
	{
		ArgumentNullException.ThrowIfNull(content);

		// Read into a buffer we own before handing anything to SkiaSharp: SKBitmap.Decode(Stream)
		// disposes whatever stream it's given (even when decoding fails), which would leave the
		// caller's stream unusable for a PassThrough fallback.
		using var buffer = new MemoryStream();
		content.CopyTo(buffer);
		var originalBytes = buffer.ToArray();

		// Animated GIFs would lose their animation if we decoded and re-encoded them (SkiaSharp
		// only reads the first frame), so leave them untouched.
		if (string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase))
		{
			return PassThrough(originalBytes, extension);
		}

		using var codec = SKCodec.Create(new MemoryStream(originalBytes));
#pragma warning disable CA1508 // SkiaSharp's nullability annotations claim this is never null; it demonstrably is for unrecognized image data.
		if (codec is null)
		{
			LogUndecodable(extension);
			return PassThrough(originalBytes, extension);
		}
#pragma warning restore CA1508

		using var original = SKBitmap.Decode(codec);
		if (original is null)
		{
			LogUndecodable(extension);
			return PassThrough(originalBytes, extension);
		}

		var withinBudget = original.Width <= MaxDimension
			&& original.Height <= MaxDimension
			&& originalBytes.Length <= MaxBytes;
		if (withinBudget)
		{
			return PassThrough(originalBytes, extension);
		}

		var hasAlpha = original.AlphaType != SKAlphaType.Opaque;
		var format = hasAlpha ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
		var outputExtension = hasAlpha ? ".png" : ".jpg";

		using var scaled = ResizeToFit(original, MaxDimension);
		var result = EncodeWithinBudget(scaled, format);

		LogOptimized(extension, originalBytes.Length, outputExtension, result.Length);
		return new OptimizedImage(result, outputExtension);
	}

	private static SKBitmap ResizeToFit(SKBitmap bitmap, int maxDimension)
	{
		if (bitmap.Width <= maxDimension && bitmap.Height <= maxDimension)
		{
			return bitmap.Copy();
		}

		var scale = (float)maxDimension / Math.Max(bitmap.Width, bitmap.Height);
		var targetWidth = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
		var targetHeight = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

		return bitmap.Resize(new SKImageInfo(targetWidth, targetHeight, bitmap.ColorType, bitmap.AlphaType),
			new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
	}

	/// <summary>
	///     Encodes <paramref name="bitmap" />, stepping quality down (JPEG) and, failing that,
	///     shrinking dimensions further, until the result fits <see cref="MaxBytes" /> or the
	///     bounded attempts run out - whichever comes first. Always returns the smallest attempt.
	/// </summary>
	private static MemoryStream EncodeWithinBudget(SKBitmap bitmap, SKEncodedImageFormat format)
	{
		MemoryStream? best = null;
		var maxSide = Math.Max(bitmap.Width, bitmap.Height);

		for (var dimensionAttempt = 0; dimensionAttempt <= MaxDimensionReductions; dimensionAttempt++)
		{
			// Attempt 0 must copy rather than reference bitmap directly: the `using` below would
			// otherwise dispose the caller's bitmap before later attempts can resize from it.
			using var candidate = dimensionAttempt == 0
				? bitmap.Copy()
				: ResizeToFit(bitmap, (int)(maxSide * Math.Pow(DimensionReductionFactor, dimensionAttempt)));

			foreach (var quality in QualityStepsFor(format))
			{
				var attempt = EncodeToStream(candidate, format, quality);
				if (best is null || attempt.Length < best.Length)
				{
					best?.Dispose();
					best = attempt;
				}
				else
				{
					attempt.Dispose();
				}

				if (best.Length <= MaxBytes)
				{
					return best;
				}
			}
		}

		return best!;
	}

	private static IEnumerable<int> QualityStepsFor(SKEncodedImageFormat format)
	{
		if (format != SKEncodedImageFormat.Jpeg)
		{
			// PNG's "quality" parameter controls compression effort, not visual fidelity - one pass
			// at maximum effort is all that's worth doing; further savings only come from resizing.
			yield return 100;
			yield break;
		}

		for (var quality = InitialJpegQuality; quality >= MinJpegQuality; quality -= JpegQualityStep)
		{
			yield return quality;
		}
	}

	private static MemoryStream EncodeToStream(SKBitmap bitmap, SKEncodedImageFormat format, int quality)
	{
		var stream = new MemoryStream();
		using (var image = SKImage.FromBitmap(bitmap))
		using (var data = image.Encode(format, quality))
		{
			data.SaveTo(stream);
		}

		stream.Position = 0;
		return stream;
	}

	private static OptimizedImage PassThrough(byte[] content, string extension)
	{
		return new OptimizedImage(new MemoryStream(content), extension);
	}

	[LoggerMessage(Level = LogLevel.Information,
		Message = "Optimized image {Extension} ({OriginalBytes} bytes) -> {OutputExtension} ({OptimizedBytes} bytes)")]
	private partial void LogOptimized(string extension, long originalBytes, string outputExtension, long optimizedBytes);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Could not decode uploaded image with extension {Extension}; storing unchanged")]
	private partial void LogUndecodable(string extension);
}
