#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
#if !HAS_UNO
using System.Diagnostics;
using System.Runtime.InteropServices;
#endif
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests;

public static partial class InputInjectorHelperExtensions
{
	public static void Tap(this InputInjectorHelper injector, UIElement elt)
	{
		var center = GetAbsoluteCenter(elt);
#if !HAS_UNO
		// Bring the test app window to the foreground before injecting input.
		// When a debugger (e.g., VS Code) is attached, its window may overlap
		// the test app, causing injected clicks to land on the wrong window.
		BringAppToForeground();

		if (injector.CurrentPointerType == PointerDeviceType.Touch)
		{
			// On WinUI 3, InjectedInputTouchInfo.PixelLocation expects screen pixels,
			// but GetAbsoluteCenter returns window-client-relative DIPs.
			// Convert to screen pixels like we do for Mouse.
			var touchScale = elt.XamlRoot?.RasterizationScale ?? 1.0;
			var touchScreenPt = GetOnScreenPoint(center.X, center.Y, touchScale);
			injector.TapCoordinates(touchScreenPt.x, touchScreenPt.y);
			return;
		}

		if (injector.CurrentPointerType == PointerDeviceType.Mouse)
		{
			// On WinUI 3, InputInjector uses relative mouse deltas from the physical OS cursor.
			// Use Win32 SetCursorPos for absolute screen positioning, then inject a zero-delta
			// Move so the WinUI input system registers the cursor at the target before button events.
			var scale = elt.XamlRoot?.RasterizationScale ?? 1.0;
			injector.InjectMouseInput(injector.Mouse.ReleaseAny());
			var screenPt = GetOnScreenPoint(center.X, center.Y, scale);
			SetCursorPos(screenPt.x, screenPt.y);

			// Verify the cursor reached the target. SetCursorPos silently clamps
			// to virtual screen bounds; if the cursor drifted, the click would miss.
			GetCursorPos(out var actual);
			if (Math.Abs(actual.x - screenPt.x) > 2 || Math.Abs(actual.y - screenPt.y) > 2)
			{
				var processHwnd = Process.GetCurrentProcess().MainWindowHandle;
				var diagHwnd = GetActiveWindow();
				GetWindowRect(processHwnd, out var procRect);
				throw new InvalidOperationException(
					$"Cannot tap element: cursor could not reach screen ({screenPt.x},{screenPt.y}). " +
					$"Actual=({actual.x},{actual.y}). " +
					$"Center DIPs=({center.X:F1},{center.Y:F1}), Scale={scale}. " +
					$"ProcessHWND=0x{processHwnd.ToInt64():X}, ActiveHWND=0x{diagHwnd.ToInt64():X}. " +
					$"ProcessRect=({procRect.left},{procRect.top},{procRect.right},{procRect.bottom}).");
			}

			injector.InjectMouseInput(new InjectedInputMouseInfo
			{
				MouseOptions = InjectedInputMouseOptions.Move,
				DeltaX = 0,
				DeltaY = 0
			});
			injector.Mouse.SetTrackedPosition(center.X, center.Y);
			injector.InjectMouseInput(injector.Mouse.Press());
			injector.InjectMouseInput(injector.Mouse.Release());
			return;
		}
#endif
		injector.TapCoordinates(center);
	}

	public static void TapCoordinates(this InputInjectorHelper injector, Point point)
		=> injector.TapCoordinates(point.X, point.Y);

	public static void TapCoordinates(this InputInjectorHelper injector, double x, double y)
	{
		switch (injector.CurrentPointerType)
		{
			case PointerDeviceType.Touch:
				injector.Injector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
				var downInfo = new InjectedInputTouchInfo
				{
					PointerInfo = new()
					{
						PointerId = 42,
						PixelLocation = new() { PositionX = (int)x, PositionY = (int)y },
						PointerOptions = InjectedInputPointerOptions.New
							| InjectedInputPointerOptions.FirstButton
							| InjectedInputPointerOptions.PointerDown
							| InjectedInputPointerOptions.InContact
					}
				};
				var upInfo = new InjectedInputTouchInfo
				{
					PointerInfo = new()
					{
						PointerId = 42,
						PixelLocation = { PositionX = (int)x, PositionY = (int)y },
						PointerOptions = InjectedInputPointerOptions.FirstButton
							| InjectedInputPointerOptions.PointerUp
					}
				};
				injector.Injector.InjectTouchInput(new[] { downInfo, upInfo });
				injector.Injector.UninitializeTouchInjection();
				break;

			case PointerDeviceType.Mouse:
				injector.InjectMouseInput(injector.Mouse.ReleaseAny());
				injector.InjectMouseInput(injector.Mouse.MoveTo(x, y));
				injector.InjectMouseInput(injector.Mouse.Press());
				injector.InjectMouseInput(injector.Mouse.Release());
				break;

			default:
				throw NotSupported();
		}
	}

	public static void Drag(this InputInjectorHelper injector, UIElement elt, Point to)
		=> injector.DragCoordinates(GetAbsoluteCenter(elt), to);

	public static void Drag(this InputInjectorHelper injector, UIElement elt, double toX, double toY)
		=> injector.DragCoordinates(GetAbsoluteCenter(elt), new Point(toX, toY));

	public static void DragCoordinates(this InputInjectorHelper injector, Point from, Point to)
		=> injector.DragCoordinates(from.X, from.Y, to.X, to.Y);

	public static void DragCoordinates(this InputInjectorHelper injector, double fromX, double fromY, double toX, double toY)
	{
		switch (injector.CurrentPointerType)
		{
			case PointerDeviceType.Touch:
				injector.Injector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
				injector.Injector.InjectTouchInput(Inputs());
				injector.Injector.UninitializeTouchInjection();

				IEnumerable<InjectedInputTouchInfo> Inputs()
				{
					yield return new()
					{
						PointerInfo = new()
						{
							PointerId = 42,
							PixelLocation = new() { PositionX = (int)fromX, PositionY = (int)fromY },
							PointerOptions = InjectedInputPointerOptions.New
								| InjectedInputPointerOptions.FirstButton
								| InjectedInputPointerOptions.PointerDown
								| InjectedInputPointerOptions.InContact
								| InjectedInputPointerOptions.InRange
						}
					};

					var steps = 10;
					var stepX = (toX - fromX) / steps;
					var stepY = (toY - fromY) / steps;
					for (var step = 0; step <= steps; step++)
					{
						yield return new()
						{
							PointerInfo = new()
							{
								PixelLocation = new() { PositionX = (int)(fromX + step * stepX), PositionY = (int)(fromY + step * stepY) },
								PointerOptions = InjectedInputPointerOptions.Update
									| InjectedInputPointerOptions.FirstButton
									| InjectedInputPointerOptions.InContact
									| InjectedInputPointerOptions.InRange
							}
						};
					}

					yield return new()
					{
						PointerInfo = new()
						{
							PixelLocation = { PositionX = (int)toX, PositionY = (int)toY },
							PointerOptions = InjectedInputPointerOptions.FirstButton
								| InjectedInputPointerOptions.PointerUp
						}
					};
				}

				break;

			case PointerDeviceType.Mouse:
				injector.InjectMouseInput(injector.Mouse.ReleaseAny());
				injector.InjectMouseInput(injector.Mouse.MoveTo(fromX, fromY));
				injector.InjectMouseInput(injector.Mouse.Press());
				injector.InjectMouseInput(injector.Mouse.MoveTo(toX, toY));
				injector.InjectMouseInput(injector.Mouse.Release());
				break;

			default:
				throw NotSupported();
		}
	}

	private static Point GetAbsoluteCenter(UIElement elt)
		=> elt.TransformToVisual(null).TransformPoint(new Point(elt.ActualSize.X / 2.0, elt.ActualSize.Y / 2.0));

	private static NotSupportedException NotSupported([CallerMemberName] string operation = "")
		=> new($"'{operation}' with type '{InputInjectorHelper.Current.CurrentPointerType}' is not supported yet on this platform. Feel free to contribute!");

#if !HAS_UNO
	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int x;
		public int y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int left, top, right, bottom;
	}

	[DllImport("user32.dll")]
	private static extern bool SetCursorPos(int X, int Y);

	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
		int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int nIndex);

	[DllImport("user32.dll")]
	private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	private const uint GA_ROOT = 2;

	private const uint SWP_NOSIZE = 0x0001;
	private const uint SWP_NOZORDER = 0x0004;
	private const uint SWP_NOACTIVATE = 0x0010;

	private const int SM_XVIRTUALSCREEN = 76;
	private const int SM_YVIRTUALSCREEN = 77;
	private const int SM_CXVIRTUALSCREEN = 78;
	private const int SM_CYVIRTUALSCREEN = 79;

	/// <summary>
	/// Converts XamlRoot-relative DIP coordinates to screen pixel coordinates.
	/// Uses the main window HWND's client area as the reference frame, since
	/// <c>TransformToVisual(null)</c> returns coordinates in the XamlRoot space
	/// which corresponds to the main window's client area on WinUI 3.
	/// </summary>
	/// <remarks>
	/// On WinUI 3, <c>GetActiveWindow()</c> returns a XAML island child HWND whose
	/// client area origin may differ from the main window's client area origin.
	/// Using such a child HWND for <c>ClientToScreen</c> produces incorrect screen
	/// coordinates, causing <c>SetCursorPos</c> to place the cursor at the wrong position.
	/// This method avoids that by always using <c>Process.MainWindowHandle</c>.
	/// </remarks>
	private static POINT GetOnScreenPoint(double clientDipX, double clientDipY, double scale)
	{
		// Use Process.MainWindowHandle to get the top-level window HWND.
		// GetActiveWindow() can return a XAML island child HWND with a different
		// client area origin, causing coordinate mismatches on WinUI 3.
		var hwnd = Process.GetCurrentProcess().MainWindowHandle;
		if (hwnd == IntPtr.Zero)
		{
			hwnd = GetActiveWindow();
		}
		if (hwnd == IntPtr.Zero)
		{
			hwnd = GetForegroundWindow();
		}

		var clientPixel = new POINT
		{
			x = (int)(clientDipX * scale),
			y = (int)(clientDipY * scale)
		};

		if (hwnd == IntPtr.Zero)
		{
			return clientPixel;
		}

		var screenPoint = clientPixel;
		ClientToScreen(hwnd, ref screenPoint);

		// If the screen position is outside the virtual screen, SetCursorPos
		// will clamp the cursor and touch injection will miss the target.
		// This happens when the test runner window extends beyond the visible
		// screen area (common in automated/CI environments or multi-monitor setups).
		if (!IsPointOnVirtualScreen(screenPoint.x, screenPoint.y))
		{
			EnsureClientAreaOnScreen(hwnd);

			// Recompute screen coordinates after the window move.
			screenPoint = clientPixel;
			ClientToScreen(hwnd, ref screenPoint);
		}

		return screenPoint;
	}

	private static bool IsPointOnVirtualScreen(int x, int y)
	{
		int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
		int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
		int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
		int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
		return x >= left && x < left + width && y >= top && y < top + height;
	}

	/// <summary>
	/// Moves the top-level window so the given HWND's client area
	/// origin maps to screen (100, 100), ensuring elements near the top-left
	/// of the client area are reachable by the OS cursor.
	/// </summary>
	private static void EnsureClientAreaOnScreen(IntPtr hwnd)
	{
		// Determine where the HWND's client (0,0) currently maps on screen.
		var clientOrigin = new POINT();
		ClientToScreen(hwnd, ref clientOrigin);

		// Find the top-level window to reposition. If the caller already
		// passed the top-level HWND, GetAncestor returns it unchanged.
		var rootHwnd = Process.GetCurrentProcess().MainWindowHandle;
		if (rootHwnd == IntPtr.Zero)
		{
			rootHwnd = GetAncestor(hwnd, GA_ROOT);
		}
		if (rootHwnd == IntPtr.Zero)
		{
			return; // Cannot determine top-level window; skip repositioning.
		}

		GetWindowRect(rootHwnd, out var windowRect);

		// Shift the root window so the HWND's client area origin
		// lands at screen (100, 100).
		int newLeft = windowRect.left + (100 - clientOrigin.x);
		int newTop = windowRect.top + (100 - clientOrigin.y);

		SetWindowPos(rootHwnd, IntPtr.Zero, newLeft, newTop, 0, 0,
			SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

		// Allow the OS to fully process the window position change
		// before subsequent ClientToScreen calls read updated coordinates.
		System.Threading.Thread.Sleep(500);
	}

	/// <summary>
	/// Requests that the OS bring the test app's main window to the
	/// foreground so that injected input reaches it rather than a
	/// different window that currently owns the foreground lock.
	/// </summary>
	private static void BringAppToForeground()
	{
		var hwnd = Process.GetCurrentProcess().MainWindowHandle;
		if (hwnd != IntPtr.Zero)
		{
			SetForegroundWindow(hwnd);
		}
	}
#endif
}

#endif