#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.UI;

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
#endif

namespace Uno.UI.RuntimeTests;

/// <summary>
/// Represents a <see cref="RenderTargetBitmap"/> to be tested against.
/// </summary>
public partial class TestBitmap
{
	private readonly RenderTargetBitmap _bitmap;
	private readonly UIElement _renderedElement; // Allow access through partial implementation
	private readonly double _implicitScaling;

	private byte[]? _pixels; 
	private bool _altered;

	private TestBitmap(RenderTargetBitmap bitmap, UIElement renderedElement, double implicitScaling)
	{
		_bitmap = bitmap;
		_renderedElement = renderedElement;
		_implicitScaling = implicitScaling;
	}

	/// <summary>
	/// Prefer using UIHelper.Screenshot() instead.
	/// </summary>
	internal static async Task<TestBitmap> From(RenderTargetBitmap bitmap, UIElement renderedElement, double? implicitScaling = null)
	{
		implicitScaling ??= DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1;
		var raw = new TestBitmap(bitmap, renderedElement, implicitScaling.Value);
		await raw.Populate();

		return raw;
	}

	/// <summary>
	/// Enables the <see cref="GetPixel(int, int)"/> method.
	/// </summary>
	/// <returns></returns>
	private async Task Populate()
	{
		_pixels ??= (await _bitmap.GetPixelsAsync()).ToArray();

		// Image is RGBA-premul, we need to un-multiply it to get the actual color in GetPixel().
		ImageHelper.UnMultiplyAlpha(_pixels);
	}

	/// <summary>
	/// The implicit scaling applied on coordinates provided to <see cref="GetPixel(int, int)"/>.
	/// </summary>
	/// <remarks>
	/// When not 1.0, this factor is applied on coordinates requested in indexer and <see cref="GetPixel"/>.
	/// For example, if scaling is 2.0, then a call to <see cref="GetPixel(int, int)"/> with (10, 10) will return the color of the pixel at (20, 20) in the bitmap.
	/// This allows user to work in "logical pixel" while underlying bitmap is at full physical resolution.
	/// </remarks>
	public double ImplicitScaling => _implicitScaling;

	/// <summary>
	/// The **logical** size of the bitmap.
	/// </summary>
	public Size Size => new(Width, Height);

	/// <summary>
	/// The **logical** width of the bitmap.
	/// </summary>
	public int Width => (int)(_bitmap.PixelWidth / _implicitScaling);

	/// <summary>
	/// The **logical** height of the bitmap.
	/// </summary>
	public int Height => (int)(_bitmap.PixelHeight / _implicitScaling);

	/// <summary>
	/// Gets the color of the pixel at the given **logical** coordinates.
	/// </summary>
	/// <param name="x">**Logical** x position</param>
	/// <param name="y">**Logical** y position</param>
	/// <returns>The color of the pixel.</returns>
	public Color this[int x, int y] => GetPixel(x, y);

	/// <summary>
	/// Gets the color of the pixel at the given **logical** coordinates.
	/// </summary>
	/// <param name="x">**Logical** x position</param>
	/// <param name="y">**Logical** y position</param>
	/// <returns>The color of the pixel.</returns>
	public Color GetPixel(int x, int y)
	{
		if (_pixels is null)
		{
			throw new InvalidOperationException("Populate must be invoked first");
		}

		x = (int)(x * _implicitScaling);
		y = (int)(y * _implicitScaling);

		var offset = (y * _bitmap.PixelWidth + x) * 4;
		var a = _pixels[offset + 3];
		var r = _pixels[offset + 2];
		var g = _pixels[offset + 1];
		var b = _pixels[offset + 0];

		return Color.FromArgb(a, r, g, b);
	}

	/// <summary>
	/// Gets all **physical** pixels of the bitmap.
	/// </summary>
	/// <returns>The underlying pixels bitmap.</returns>
	public byte[] GetRawPixels()
	{
		if (_pixels is null)
		{
			throw new InvalidOperationException("Populate must be invoked first");
		}

		return _pixels;
	}

	/// <summary>
	/// Gets the underlying <see cref="ImageSource"/>.
	/// </summary>
	/// <param name="preferOriginal">Indicates that original bitmap should be returned, ignoring any modification made on the current bitmap.</param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	internal async Task<ImageSource> GetImageSource(bool preferOriginal = false)
	{
		if (_pixels is null)
		{
			throw new InvalidOperationException("Populate must be invoked first");
		}

		if (_altered && !preferOriginal)
		{
			var output = new WriteableBitmap(_bitmap.PixelWidth, _bitmap.PixelHeight);
			await new MemoryStream(_pixels).AsInputStream().ReadAsync(output.PixelBuffer, output.PixelBuffer.Length, InputStreamOptions.None);
			return output;
		}
		else
		{
			return _bitmap;
		}
	}

	/// <summary>
	/// Makes sure that all pixels are opaque.
	/// </summary>
	/// <param name="background">The background color to apply on non-opaque pixels.</param>
	public void MakeOpaque(Color? background = null)
	{
		if (_pixels is null)
		{
			throw new InvalidOperationException("Populate must be invoked first");
		}

		_altered = ImageHelper.MakeOpaque(_pixels, background);
	}

#if __SKIA__ && DEBUG // DEBUG: Make the build to fail on CI to avoid forgetting to remove the call (would pollute server or other devs disks!).
	/// <summary>
	/// Save the screenshot into the specified path **for debug purposes only**.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="preferOriginal"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	internal async Task Save(string path, bool preferOriginal = false)
	{
		if (_pixels is null)
		{
			throw new InvalidOperationException("Populate must be invoked first");
		}

		await using var file = File.OpenWrite(path);

		var img = preferOriginal
			? SkiaSharp.SKImage.FromPixelCopy(new SkiaSharp.SKImageInfo(_bitmap.PixelWidth, _bitmap.PixelHeight, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul), (await _bitmap.GetPixelsAsync()).ToArray())
			: SkiaSharp.SKImage.FromPixelCopy(new SkiaSharp.SKImageInfo(_bitmap.PixelWidth, _bitmap.PixelHeight, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Unpremul), _pixels);

		img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100).SaveTo(file);
	}
#endif
}

internal static class TestBitmapExtensions
{
	public static Color GetPixel(this TestBitmap bitmap, double x, double y)
		=> bitmap.GetPixel((int)x, (int)y);

	public static Color GetPixel(this TestBitmap bitmap, Point point)
		=> bitmap.GetPixel((int)point.X, (int)point.Y);
}
#endif