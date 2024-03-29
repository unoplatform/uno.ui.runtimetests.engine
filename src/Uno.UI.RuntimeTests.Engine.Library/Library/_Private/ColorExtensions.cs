﻿#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
using System;
using System.Linq;
using Windows.UI;

namespace Uno.UI.RuntimeTests;

/// <remarks>
/// This class is intended to be used only by the the test engine itself and should not be used by applications.
/// API contract is not guaranteed and might change in future releases.
/// </remarks>
internal static class ColorExtensions
{
	/// <summary>
	/// Returns the color that results from blending the color with the given background color.
	/// </summary>
	/// <param name="color">The color to blend.</param>
	/// <param name="background">The background color to use. This is assumed to be opaque (not checked for perf reason when used on pixel buffer).</param>
	/// <returns>The color that results from blending the color with the given background color.</returns>
	internal static Color ToOpaque(this Color color, Color background)
		=> Color.FromArgb(
			255,
			(byte)(((byte.MaxValue - color.A) * background.R + color.A * color.R) / 255),
			(byte)(((byte.MaxValue - color.A) * background.G + color.A * color.G) / 255),
			(byte)(((byte.MaxValue - color.A) * background.B + color.A * color.B) / 255)
		);

#if !HAS_UNO
	internal static Color WithOpacity(this Color color, double opacity)
		=> Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);
#endif
}
#endif