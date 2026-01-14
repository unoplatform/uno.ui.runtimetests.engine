#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Math;
using Windows.UI;
using Windows.Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RuntimeTests;

/// <summary>
/// Screen shot based assertions, to validate individual colors of an image
/// </summary>
public static partial class ImageAssert
{
	#region HasColorAt
	public static void HasColorAt(TestBitmap screenshot, Windows.Foundation.Point location, string expectedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> HasColorAtImpl(screenshot, (int)location.X, (int)location.Y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode), tolerance, line);

	public static void HasColorAt(TestBitmap screenshot, Windows.Foundation.Point location, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> HasColorAtImpl(screenshot, (int)location.X, (int)location.Y, expectedColor, tolerance, line);

	public static void HasColorAt(TestBitmap screenshot, float x, float y, string expectedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> HasColorAtImpl(screenshot, (int)x, (int)y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode), tolerance, line);

	public static void HasColorAt(TestBitmap screenshot, float x, float y, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> HasColorAtImpl(screenshot, (int)x, (int)y, expectedColor, tolerance, line);

	/// <summary>
	/// Asserts that a given screenshot has a color anywhere at a given rectangle.
	/// </summary>
	public static void HasColorInRectangle(TestBitmap screenshot, Rect rect, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
	{
		var bitmap = screenshot;
		(int x, int y, int diff, Color color) min = (-1, -1, int.MaxValue, default);
		for (var x = rect.Left; x < rect.Right; x++)
		{
			for (var y = rect.Top; y < rect.Bottom; y++)
			{
				var pixel = bitmap.GetPixel(x, y);
				if (AreSameColor(expectedColor, pixel, tolerance, out var diff))
				{
					return;
				}
				else if (diff < min.diff)
				{
					min = ((int)x, (int)y, diff, pixel);
				}
			}
		}

		Assert.Fail($"Expected '{ToArgbCode(expectedColor)}' in rectangle '{rect}', but no pixel has this color. The closest pixel found is '{ToArgbCode(min.color)}' at '{min.x},{min.y}' with a (exclusive) difference of {min.diff}.");
	}

	/// <summary>
	/// Asserts that a given screenshot does not have a specific color anywhere within a given rectangle.
	/// </summary>
	public static void DoesNotHaveColorInRectangle(TestBitmap screenshot, Rect rect, Color excludedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
	{
		var bitmap = screenshot;
		for (var x = rect.Left; x < rect.Right; x++)
		{
			for (var y = rect.Top; y < rect.Bottom; y++)
			{
				var pixel = bitmap.GetPixel(x, y);
				if (AreSameColor(excludedColor, pixel, tolerance, out var diff))
				{
					Assert.Fail($"Color '{ToArgbCode(excludedColor)}' was found at ({x}, {y}) in rectangle '{rect}' (Exclusive difference of {diff}).");
				}
			}
		}
	}

	private static void HasColorAtImpl(TestBitmap screenshot, int x, int y, Color expectedColor, byte tolerance, int line)
	{
		var bitmap = screenshot;

		if (bitmap.Width <= x || bitmap.Height <= y)
		{
			Assert.Fail(WithContext($"Coordinates ({x}, {y}) falls outside of screenshot dimension {bitmap.Size}"));
		}

		var pixel = bitmap.GetPixel(x, y);

		if (!AreSameColor(expectedColor, pixel, tolerance, out var difference))
		{
			Assert.Fail(WithContext(builder: builder => builder
				.AppendLine($"Color at ({x},{y}) is not expected")
				.AppendLine($"expected: {ToArgbCode(expectedColor)} {expectedColor}")
				.AppendLine($"actual  : {ToArgbCode(pixel)} {pixel}")
				.AppendLine($"tolerance: {tolerance}")
				.AppendLine($"difference: {difference}")
			));
		}

		string WithContext(string? message = null, Action<StringBuilder>? builder = null)
		{
			var sb = new StringBuilder()
				.AppendLine($"ImageAssert.HasColorAt @ line {line}")
				.AppendLine("====================");

			if (message is not null)
			{
				sb.AppendLine(message);
			}
			builder?.Invoke(sb);

			return sb.ToString();
		}
	}
	#endregion

	#region DoesNotHaveColorAt
	public static void DoesNotHaveColorAt(TestBitmap screenshot, float x, float y, string excludedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> DoesNotHaveColorAtImpl(screenshot, (int)x, (int)y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), excludedColorCode), tolerance, line);

	public static void DoesNotHaveColorAt(TestBitmap screenshot, float x, float y, Color excludedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> DoesNotHaveColorAtImpl(screenshot, (int)x, (int)y, excludedColor, tolerance, line);

	private static void DoesNotHaveColorAtImpl(TestBitmap screenshot, int x, int y, Color excludedColor, byte tolerance, int line)
	{
		var bitmap = screenshot;
		if (bitmap.Width <= x || bitmap.Height <= y)
		{
			Assert.Fail(WithContext($"Coordinates ({x}, {y}) falls outside of screenshot dimension {bitmap.Size}"));
		}

		var pixel = bitmap.GetPixel(x, y);
		if (AreSameColor(excludedColor, pixel, tolerance, out var difference))
		{
			Assert.Fail(WithContext(builder: builder => builder
				.AppendLine($"Color at ({x},{y}) is not expected")
				.AppendLine($"excluded: {ToArgbCode(excludedColor)}")
				.AppendLine($"actual  : {ToArgbCode(pixel)} {pixel}")
				.AppendLine($"tolerance: {tolerance}")
				.AppendLine($"difference: {difference}")
			));
		}

		string WithContext(string? message = null, Action<StringBuilder>? builder = null)
		{
			var sb = new StringBuilder()
				.AppendLine($"ImageAssert.DoesNotHaveColorAt @ line {line}")
				.AppendLine("====================");

			if (message is not null)
			{
				sb.AppendLine(message);
			}
			builder?.Invoke(sb);

			return sb.ToString();
		}
	}
	#endregion

	#region HasPixels
	public static void HasPixels(TestBitmap actual, params ExpectedPixels[] expectations)
	{
		var bitmap = actual;

		foreach (var expectation in expectations)
		{
			var x = expectation.Location.X;
			var y = expectation.Location.Y;

			Assert.IsTrue(bitmap.Width >= x);
			Assert.IsTrue(bitmap.Height >= y);

			var result = new StringBuilder();
			result.AppendLine(expectation.Name);
			if (!Validate(expectation, bitmap, 1, result))
			{
				Assert.Fail(result.ToString());
			}
		}
	}
	#endregion

	internal static Rect GetColorBounds(TestBitmap testBitmap, Color color, byte tolerance = 0)
	{
		var minX = int.MaxValue;
		var minY = int.MaxValue;
		var maxX = int.MinValue;
		var maxY = int.MinValue;

		for (int x = 0; x < testBitmap.Width; x++)
		{
			for (int y = 0; y < testBitmap.Height; y++)
			{
				if (AreSameColor(color, testBitmap.GetPixel(x, y), tolerance, out _))
				{
					minX = Math.Min(minX, x);
					minY = Math.Min(minY, y);
					maxX = Math.Max(maxX, x);
					maxY = Math.Max(maxY, y);
				}
			}
		}

		return new Rect(new Point(minX, minY), new Size(maxX - minX, maxY - minY));
	}

	public static async Task AreEqual(TestBitmap actual, TestBitmap expected)
	{
		CollectionAssert.AreEqual(actual.GetRawPixels(), expected.GetRawPixels());
	}

	public static async Task AreNotEqual(TestBitmap actual, TestBitmap expected)
	{
		CollectionAssert.AreNotEqual(actual.GetRawPixels(), expected.GetRawPixels());
	}

	/// <summary>
	/// Asserts that two image are similar within the given <see href="https://en.wikipedia.org/wiki/Root-mean-square_deviation">RMSE</see>
	/// The method it based roughly on ImageMagick implementation to ensure consistency.
	/// If the error is greater than or equal to 0.022, the differences are visible to human eyes.
	/// <paramref name="actual">The image to compare with reference</paramref>
	/// <paramref name="expected">Reference image.</paramref>
	/// <paramref name="imperceptibilityThreshold">It is the threshold beyond which the compared images are not considered equal. Default value is 0.022.</paramref>>
	/// </summary>
	public static async Task AreSimilarAsync(TestBitmap actual, TestBitmap expected, double imperceptibilityThreshold = 0.022)
	{
		if (actual.Width != expected.Width || actual.Height != expected.Height)
		{
			Assert.Fail($"Images have different resolutions. {Environment.NewLine}expected:({expected.Width},{expected.Height}){Environment.NewLine}actual  :({actual.Width},{actual.Height})");
		}

		var quantity = actual.Width * actual.Height;
		double squaresError = 0;

		const double scale = 1 / 255d;

		for (var x = 0; x < actual.Width; x++)
		{
			double localError = 0;

			for (var y = 0; y < actual.Height; y++)
			{
				var expectedAlpha = expected[x, y].A * scale;
				var actualAlpha = actual[x, y].A * scale;

				var r = scale * (expectedAlpha * expected[x, y].R - actualAlpha * actual[x, y].R);
				var g = scale * (expectedAlpha * expected[x, y].G - actualAlpha * actual[x, y].G);
				var b = scale * (expectedAlpha * expected[x, y].B - actualAlpha * actual[x, y].B);
				var a = expectedAlpha - actualAlpha;

				var error = r * r + g * g + b * b + a * a;

				localError += error;
			}

			squaresError += localError;
		}

		var meanSquaresError = squaresError / quantity;

		const int channelCount = 4;

		meanSquaresError = meanSquaresError / channelCount;
		var sqrtMeanSquaresError = Sqrt(meanSquaresError);
		if (sqrtMeanSquaresError >= imperceptibilityThreshold)
		{
			Assert.Fail($"the actual image is not the same as the expected one.{Environment.NewLine}actual RSMD: {sqrtMeanSquaresError}{Environment.NewLine}threshold: {imperceptibilityThreshold}");
		}
	}
}
#endif