using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
[RunsOnUIThread]
public class PointersInjectionTests
{
#if HAS_UNO_SKIA || WINDOWS
	[TestInitialize]
	public void Setup()
	{
		try { InputInjectorHelper.Current.CleanupPointers(); }
		catch { /* Ignore cleanup errors from a previous test's dirty state */ }
	}

	[TestCleanup]
	public void Cleanup()
	{
		try { InputInjectorHelper.Current.CleanupPointers(); }
		catch { /* COMException can occur if the injector is in an unexpected state */ }
	}
#endif

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
	[InjectedPointer(PointerDeviceType.Touch)]
#if !HAS_UNO_SKIA && !WINDOWS
	[ExpectedException(typeof(NotSupportedException))]
#endif
	public async Task When_TapCoordinates()
	{
		var elt = new Button { Content = "Tap me" };
		var clicked = false;
		elt.Click += (snd, e) => clicked = true;

		UnitTestsUIContentHelper.Content = elt;

		await UnitTestsUIContentHelper.WaitForLoaded(elt);

		var center = elt.TransformToVisual(null)
			.TransformPoint(new Point(elt.ActualSize.X / 2.0, elt.ActualSize.Y / 2.0));

		InputInjectorHelper.Current.Tap(elt);

		// On WinUI 3, InputInjector queues events into the OS input queue.
		// WaitForIdle gives the dispatcher enough cycles to process them.
		await UnitTestsUIContentHelper.WaitForIdle();

		Assert.IsTrue(clicked,
			$"Button should have been clicked. Center=({center.X:F1},{center.Y:F1}), " +
			$"Scale={elt.XamlRoot?.RasterizationScale ?? -1}");
	}

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !HAS_UNO_SKIA && !WINDOWS
	[ExpectedException(typeof(NotSupportedException))]
#endif
	public async Task When_TapCoordinates_Sequential()
	{
		var panel = new StackPanel { Spacing = 8 };
		var button1 = new Button { Content = "Button 1", Width = 120, Height = 40 };
		var button2 = new Button { Content = "Button 2", Width = 120, Height = 40 };
		panel.Children.Add(button1);
		panel.Children.Add(button2);
		var clicked1 = false;
		var clicked2 = false;
		button1.Click += (_, _) => clicked1 = true;
		button2.Click += (_, _) => clicked2 = true;

		UnitTestsUIContentHelper.Content = panel;

		await UnitTestsUIContentHelper.WaitForLoaded(button1);
		await UnitTestsUIContentHelper.WaitForLoaded(button2);

		var center1 = button1.TransformToVisual(null)
			.TransformPoint(new Point(button1.ActualSize.X / 2.0, button1.ActualSize.Y / 2.0));
		var center2 = button2.TransformToVisual(null)
			.TransformPoint(new Point(button2.ActualSize.X / 2.0, button2.ActualSize.Y / 2.0));
		var scale = button1.XamlRoot?.RasterizationScale ?? -1;

		InputInjectorHelper.Current.Tap(button1);
		await UnitTestsUIContentHelper.WaitForIdle();
		Assert.IsTrue(clicked1,
			$"First button should have been clicked. " +
			$"Btn1=({center1.X:F1},{center1.Y:F1}), Btn2=({center2.X:F1},{center2.Y:F1}), Scale={scale}");

		InputInjectorHelper.Current.Tap(button2);
		await UnitTestsUIContentHelper.WaitForIdle();
		Assert.IsTrue(clicked2,
			$"Second button should have been clicked. " +
			$"Btn1=({center1.X:F1},{center1.Y:F1}), Btn2=({center2.X:F1},{center2.Y:F1}), Scale={scale}");
	}

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !HAS_UNO_SKIA && !WINDOWS
	[ExpectedException(typeof(NotSupportedException))]
#endif
	public void When_MoveTo_GeneratesCorrectDeltas()
	{
		var injector = InputInjectorHelper.Current;

		// Capture moves while injecting so position tracking updates between steps
		var capturedMoves = new List<Windows.UI.Input.Preview.Injection.InjectedInputMouseInfo>();
		var moves = injector.Mouse.MoveTo(100, 50).Select(m => { capturedMoves.Add(m); return m; });
		injector.InjectMouseInput(moves);

		Assert.IsTrue(capturedMoves.Count > 0, "MoveTo should generate at least one move");

#if !HAS_UNO
		// On WinUI 3, _trackedPosition gives deterministic delta sums
		var totalDeltaX = capturedMoves.Sum(m => m.DeltaX);
		var totalDeltaY = capturedMoves.Sum(m => m.DeltaY);

		Assert.IsTrue(Math.Abs(totalDeltaX - 100) <= 2, $"Total deltaX should be ~100, was {totalDeltaX}");
		Assert.IsTrue(Math.Abs(totalDeltaY - 50) <= 2, $"Total deltaY should be ~50, was {totalDeltaY}");
#endif
	}

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !HAS_UNO_SKIA && !WINDOWS
	[ExpectedException(typeof(NotSupportedException))]
#endif
	public void When_MoveTo_Sequential_PositionTracksCorrectly()
	{
		var injector = InputInjectorHelper.Current;

		// Move to (100, 100) first
		injector.InjectMouseInput(injector.Mouse.MoveTo(100, 100));

		// Now move to (150, 120) - capture moves to verify deltas are relative
		var capturedMoves = new List<Windows.UI.Input.Preview.Injection.InjectedInputMouseInfo>();
		var moves = injector.Mouse.MoveTo(150, 120).Select(m => { capturedMoves.Add(m); return m; });
		injector.InjectMouseInput(moves);

		Assert.IsTrue(capturedMoves.Count > 0, "Sequential MoveTo should generate moves");

#if !HAS_UNO
		// On WinUI 3, _trackedPosition gives deterministic delta sums
		var totalDeltaX = capturedMoves.Sum(m => m.DeltaX);
		var totalDeltaY = capturedMoves.Sum(m => m.DeltaY);

		Assert.IsTrue(Math.Abs(totalDeltaX - 50) <= 2, $"Sequential deltaX should be ~50, was {totalDeltaX}");
		Assert.IsTrue(Math.Abs(totalDeltaY - 20) <= 2, $"Sequential deltaY should be ~20, was {totalDeltaY}");
#endif
	}

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !HAS_UNO_SKIA && !WINDOWS
	[ExpectedException(typeof(NotSupportedException))]
#endif
	public void When_ReleaseAny_DoesNotThrow()
	{
		var injector = InputInjectorHelper.Current;

		// ReleaseAny should not throw ArgumentException on any platform.
		// On Windows, sending XUp without MouseData caused ArgumentException.
		injector.InjectMouseInput(injector.Mouse.ReleaseAny());
	}

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !HAS_UNO_SKIA && !WINDOWS
	[ExpectedException(typeof(NotSupportedException))]
#endif
	public void When_CleanupPointers_DoesNotThrow()
	{
		var injector = InputInjectorHelper.Current;

		// First simulate some activity: move and press/release
		injector.InjectMouseInput(injector.Mouse.MoveTo(200, 200));
		injector.InjectMouseInput(injector.Mouse.Press());
		injector.InjectMouseInput(injector.Mouse.Release());

		// CleanupPointers should not throw and should not move the OS cursor to (0,0)
		injector.CleanupPointers();

		// After cleanup, MoveTo should still generate correct deltas from reset position
		var capturedMoves = new List<Windows.UI.Input.Preview.Injection.InjectedInputMouseInfo>();
		var moves = injector.Mouse.MoveTo(50, 30).Select(m => { capturedMoves.Add(m); return m; });
		injector.InjectMouseInput(moves);

		Assert.IsTrue(capturedMoves.Count > 0, "Should generate moves after cleanup");

#if !HAS_UNO
		var totalDeltaX = capturedMoves.Sum(m => m.DeltaX);
		var totalDeltaY = capturedMoves.Sum(m => m.DeltaY);

		// After CleanupPointers, tracked position resets to (0,0), so deltas should be ~(50, 30)
		Assert.IsTrue(Math.Abs(totalDeltaX - 50) <= 2, $"Post-cleanup deltaX should be ~50, was {totalDeltaX}");
		Assert.IsTrue(Math.Abs(totalDeltaY - 30) <= 2, $"Post-cleanup deltaY should be ~30, was {totalDeltaY}");
#endif
	}
}