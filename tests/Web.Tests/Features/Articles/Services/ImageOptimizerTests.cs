using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using SkiaSharp;

using Web.Components.Features.Articles.Services;

namespace Web.Tests.Features.Articles.Services;

public sealed class ImageOptimizerTests
{
	private readonly ImageOptimizer _optimizer = new(Substitute.For<ILogger<ImageOptimizer>>());

	[Fact]
	public void Optimize_LeavesSmallImageUnchanged()
	{
		// Arrange
		var small = EncodePng(width: 20, height: 20, withAlpha: false);

		// Act
		var result = _optimizer.Optimize(new MemoryStream(small), ".png");

		// Assert
		result.Extension.Should().Be(".png");
		result.Content.ToArray().Should().Equal(small);
	}

	[Fact]
	public void Optimize_ResizesAndConvertsLargeOpaqueImageToJpeg()
	{
		// Arrange
		var large = EncodePng(width: 3000, height: 2000, withAlpha: false);

		// Act
		var result = _optimizer.Optimize(new MemoryStream(large), ".png");

		// Assert
		result.Extension.Should().Be(".jpg");
		using var decoded = SKBitmap.Decode(result.Content.ToArray());
		decoded.Width.Should().BeLessThanOrEqualTo(1920);
		decoded.Height.Should().BeLessThanOrEqualTo(1920);
		result.Content.Length.Should().BeLessThan(large.Length);
	}

	[Fact]
	public void Optimize_ResizesButKeepsPngWhenImageHasTransparency()
	{
		// Arrange
		var large = EncodePng(width: 3000, height: 2000, withAlpha: true);

		// Act
		var result = _optimizer.Optimize(new MemoryStream(large), ".png");

		// Assert
		result.Extension.Should().Be(".png");
		using var decoded = SKBitmap.Decode(result.Content.ToArray());
		decoded.Width.Should().BeLessThanOrEqualTo(1920);
		decoded.Height.Should().BeLessThanOrEqualTo(1920);
	}

	[Fact]
	public void Optimize_KeepsResultUnderTheSizeBudget()
	{
		// Arrange: high-entropy noise compresses poorly, exercising the quality/size-reduction loop.
		var noisy = EncodeNoisyPng(width: 1000, height: 1000);

		// Act
		var result = _optimizer.Optimize(new MemoryStream(noisy), ".png");

		// Assert
		result.Content.Length.Should().BeLessThanOrEqualTo(512_000);
	}

	[Fact]
	public void Optimize_LeavesAnimatedGifExtensionUntouched()
	{
		// Arrange
		byte[] gifBytes = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];

		// Act
		var result = _optimizer.Optimize(new MemoryStream(gifBytes), ".gif");

		// Assert
		result.Extension.Should().Be(".gif");
		result.Content.ToArray().Should().Equal(gifBytes);
	}

	[Fact]
	public void Optimize_PassesThroughUndecodableData()
	{
		// Arrange
		byte[] garbage = [1, 2, 3, 4];

		// Act
		var result = _optimizer.Optimize(new MemoryStream(garbage), ".png");

		// Assert
		result.Extension.Should().Be(".png");
		result.Content.ToArray().Should().Equal(garbage);
	}

	private static byte[] EncodePng(int width, int height, bool withAlpha)
	{
		var colorType = withAlpha ? SKColorType.Rgba8888 : SKColorType.Rgb888x;
		var alphaType = withAlpha ? SKAlphaType.Premul : SKAlphaType.Opaque;
		using var bitmap = new SKBitmap(new SKImageInfo(width, height, colorType, alphaType));
		using (var canvas = new SKCanvas(bitmap))
		{
			canvas.Clear(withAlpha ? new SKColor(255, 0, 0, 128) : new SKColor(255, 0, 0));
		}

		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		return data.ToArray();
	}

#pragma warning disable CA5394 // Test fixture data only - no security implication.
	private static byte[] EncodeNoisyPng(int width, int height)
	{
		using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgb888x, SKAlphaType.Opaque));
		var random = new Random(42);
		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				bitmap.SetPixel(x, y, new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
			}
		}
#pragma warning restore CA5394

		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		return data.ToArray();
	}
}
