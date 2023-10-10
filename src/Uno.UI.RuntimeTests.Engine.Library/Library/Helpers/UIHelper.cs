#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
#endif

namespace Uno.UI.RuntimeTests;

/// <summary>
/// Set of helpers to interact with the UI during tests.
/// </summary>
internal static class UIHelper
{
	/// <summary>
	/// Gets or sets the content of the current test area.
	/// </summary>
	public static UIElement? Content
	{
		get => UnitTestsUIContentHelper.Content;
		set => UnitTestsUIContentHelper.Content = value;
	}

	/// <summary>
	/// Set the provided element as <see cref="Content"/> and wait for it to be loaded (cf. <see cref="WaitForLoaded"/>).
	/// </summary>
	/// <param name="element">The element to set as content.</param>
	/// <param name="ct">A cancellation token to cancel the async loading operation.</param>
	/// <returns>An async operation that will complete when the given element is loaded.</returns>
	/// <remarks>As all other method of this class, this assume to be invoked on the UI-thread.</remarks>
	public static async ValueTask Load(FrameworkElement element, CancellationToken ct)
	{
		Content = element;
		await WaitForLoaded(element, ct);
	}

	/// <summary>
	/// Waits for the dispatcher to finish processing pending requests
	/// </summary>
	public static async ValueTask WaitForIdle() 
		=> await UnitTestsUIContentHelper.WaitForIdle();

	/// <summary>
	/// Waits for the given element to be loaded by the visual tree.
	/// </summary>
	/// <param name="element">The element to wait for being loaded.</param>
	/// <param name="ct">A cancellation token to cancel the async loading operation.</param>
	/// <returns></returns>
	/// <exception cref="TimeoutException"></exception>
	/// <remarks>As all other method of this class, this assume to be invoked on the UI-thread.</remarks>
	public static async ValueTask WaitForLoaded(FrameworkElement element, CancellationToken ct)
	{
		if (element.IsLoaded)
		{
			return;
		}

		var tcs = new TaskCompletionSource<object?>();
		await using var _ = ct.CanBeCanceled ? ct.Register(() => tcs.TrySetCanceled()) : default;
		try
		{
			element.Loaded += OnElementLoaded;

			if (!element.IsLoaded)
			{
				var timeout = Task.Delay(TestHelper.DefaultTimeout, ct);
				if (await Task.WhenAny(tcs.Task, timeout) == timeout)
				{
					throw new TimeoutException($"Failed to load element within {TestHelper.DefaultTimeout}.");
				}
			}
		}
		finally
		{
			element.Loaded -= OnElementLoaded;
		}

		void OnElementLoaded(object sender, RoutedEventArgs e)
		{
			element.Loaded -= OnElementLoaded;
			tcs.TrySetResult(default);
		}
	}

	/// <summary>
	/// Walks the tree down to find all the children of the given type.
	/// </summary>
	/// <typeparam name="T">Type of the children</typeparam>
	/// <param name="element">The root element of the tree to walk.</param>
	/// <returns>An enumerable sequence of all children of <paramref name="element"/> that are of the requested type.</returns>
	/// <remarks>If the given <paramref name="element"/> is also a <typeparamref name="T"/>, it will be returned in the enumerable sequence.</remarks>
	/// <remarks>As all other method of this class, this assume to be invoked on the UI-thread.</remarks>
	public static IEnumerable<T> FindChildren<T>(DependencyObject element)
	{
		if (element is T t)
		{
			yield return t;
		}

		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
		{
			foreach (var child in FindChildren<T>(VisualTreeHelper.GetChild(element, i)))
			{
				yield return child;
			}
		}
	}

	/// <summary>
	/// Takes a screen-shot of the given element.
	/// </summary>
	/// <param name="element">The element to screen-shot.</param>
	/// <param name="opaque">Indicates if the resulting image should be make opaque (i.e. all pixels has an opacity of 0xFF) or not.</param>
	/// <param name="scaling">Indicates the scaling strategy to apply for the image (when screen is not using a 1.0 scale, usually 4K screens).</param>
	/// <returns></returns>
	public static async ValueTask<TestBitmap> ScreenShot(FrameworkElement element, bool opaque = false, ScreenShotScalingMode scaling = ScreenShotScalingMode.UsePhysicalPixelsWithImplicitScaling)
	{
		var renderer = new RenderTargetBitmap();
		element.UpdateLayout();
		await WaitForIdle();

		TestBitmap bitmap;
		switch (scaling)
		{
			case ScreenShotScalingMode.UsePhysicalPixelsWithImplicitScaling:
				await renderer.RenderAsync(element);
				bitmap = await TestBitmap.From(renderer, element, DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1);
				break;
			case ScreenShotScalingMode.UseLogicalPixels:
				await renderer.RenderAsync(element, (int)element.RenderSize.Width, (int)element.RenderSize.Height);
				bitmap = await TestBitmap.From(renderer, element);
				break;
			case ScreenShotScalingMode.UsePhysicalPixels:
				await renderer.RenderAsync(element);
				bitmap = await TestBitmap.From(renderer, element);
				break;
			default:
				throw new NotSupportedException($"Mode {scaling} is not supported.");
		}

		if (opaque)
		{
			bitmap.MakeOpaque();
		}

		return bitmap;
	}

	public enum ScreenShotScalingMode
	{
		/// <summary>
		/// Screen-shot is made at full resolution, then the returned RawBitmap is configured to implicitly apply screen scaling
		/// to requested pixel coordinates in <see cref="TestBitmap.GetPixel"/> method.
		///
		/// This is the best / common option has it avoids artifacts due image scaling while still allowing to use logical pixels.
		/// </summary>
		UsePhysicalPixelsWithImplicitScaling,

		/// <summary>
		/// Screen-shot is made at full resolution, and access to the returned <see cref="TestBitmap"/> are assumed to be in physical pixels.
		/// </summary>
		UsePhysicalPixels,

		/// <summary>
		/// Screen-shot is forcefully scaled down to logical pixels.
		/// </summary>
		UseLogicalPixels
	}

	/// <summary>
	/// Shows the given screenshot on screen for debug purposes
	/// </summary>
	/// <param name="bitmap">The image to show.</param>
	/// <returns></returns>
	public static async ValueTask Show(TestBitmap bitmap)
	{
		Image img;
		CompositeTransform imgTr;
		TextBlock pos;
		StackPanel legend;
		var popup = new ContentDialog
		{
			MinWidth = bitmap.Width + 2,
			MinHeight = bitmap.Height + 30,
			Content = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(),
					new RowDefinition { Height = GridLength.Auto }
				},
				Children =
				{
					new Border
					{
						BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black),
						BorderThickness = new Thickness(1),
						Background = new SolidColorBrush(Windows.UI.Colors.Gray),
						Width = bitmap.Width * bitmap.ImplicitScaling + 2,
						Height = bitmap.Height * bitmap.ImplicitScaling + 2,
						Child = img = new Image
						{
							Width = bitmap.Width * bitmap.ImplicitScaling,
							Height = bitmap.Height * bitmap.ImplicitScaling,
							Source = await bitmap.GetImageSource(),
							Stretch = Stretch.None,
							ManipulationMode = ManipulationModes.Scale
								| ManipulationModes.ScaleInertia
								| ManipulationModes.TranslateX
								| ManipulationModes.TranslateY
								| ManipulationModes.TranslateInertia,
							RenderTransformOrigin = new Point(.5, .5),
							RenderTransform = imgTr = new CompositeTransform()
						}
					},
					(legend = new StackPanel
					{
						Orientation = Orientation.Horizontal,
						HorizontalAlignment = HorizontalAlignment.Right,
						Children =
						{
							(pos = new TextBlock
							{
								Text = $"{bitmap.Width}x{bitmap.Height}",
								FontSize = 8
							})
						}
					})
				}
			},
			PrimaryButtonText = "OK"
		};
		Grid.SetRow(legend, 1);

		img.PointerMoved += (snd, e) => DumpState(e.GetCurrentPoint(img).Position);
		img.PointerWheelChanged += (snd, e) =>
		{
			if (e.KeyModifiers is VirtualKeyModifiers.Control
				&& e.GetCurrentPoint(img) is { Properties.IsHorizontalMouseWheel: false } point)
			{
				var factor = Math.Sign(point.Properties.MouseWheelDelta) is 1 ? 1.2 : 1 / 1.2;
				imgTr.ScaleX *= factor;
				imgTr.ScaleY *= factor;

				DumpState(point.Position);
			}
		};
		img.ManipulationDelta += (snd, e) =>
		{
			imgTr.TranslateX += e.Delta.Translation.X;
			imgTr.TranslateY += e.Delta.Translation.Y;
			imgTr.ScaleX *= e.Delta.Scale;
			imgTr.ScaleY *= e.Delta.Scale;

			DumpState(e.Position);
		};

		void DumpState(Point phyLoc)
		{
			var scaling = bitmap.ImplicitScaling;
			var virLoc = new Point(phyLoc.X / scaling, phyLoc.Y / scaling);
			var virSize = bitmap.Size;
			var phySize = new Size(virSize.Width * scaling, virSize.Height * scaling);

			if (virLoc.X >= 0 && virLoc.X < virSize.Width
				&& virLoc.Y >= 0 && virLoc.Y < virSize.Height)
			{
				if (scaling is not 1.0)
				{
					pos.Text = $"{imgTr.ScaleX:P0} {bitmap.GetPixel((int)virLoc.X, (int)virLoc.Y)} | vir: {virLoc.X:F0},{virLoc.Y:F0} / {virSize.Width}x{virSize.Height} | phy: {phyLoc.X:F0},{phyLoc.Y:F0} / {phySize.Width}x{phySize.Height}";
				}
				else
				{
					pos.Text = $"{imgTr.ScaleX:P0} {bitmap.GetPixel((int)virLoc.X, (int)virLoc.Y)} | {phyLoc.X:F0},{phyLoc.Y:F0} / {virSize.Width}x{virSize.Height}";
				}
			}
			else
			{
				pos.Text = $"{imgTr.ScaleX:P0} {bitmap.Width}x{bitmap.Height}";
			}
		}

		await popup.ShowAsync(ContentDialogPlacement.Popup);
	}
}

#endif