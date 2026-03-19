using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
[RunsOnUIThread]
public class PointersInjectionTests
{
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

		InputInjectorHelper.Current.Tap(elt);

		Assert.IsTrue(clicked);
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

		InputInjectorHelper.Current.Tap(button1);
		Assert.IsTrue(clicked1, "First button should have been clicked");

		InputInjectorHelper.Current.Tap(button2);
		Assert.IsTrue(clicked2, "Second button should have been clicked");
	}

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !HAS_UNO_SKIA && !WINDOWS
	[ExpectedException(typeof(NotSupportedException))]
#endif
	public void When_MoveTo_GeneratesCorrectDeltas()
	{
		var injector = InputInjectorHelper.Current;
		injector.CleanupPointers();

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
		injector.CleanupPointers();

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
}