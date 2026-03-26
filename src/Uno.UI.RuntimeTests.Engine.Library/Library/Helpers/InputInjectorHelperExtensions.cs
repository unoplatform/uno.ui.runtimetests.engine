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
				injector.Injector.InjectTouchInput(new[]
				{
					new InjectedInputTouchInfo
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
					},
					new InjectedInputTouchInfo
					{
						PointerInfo = new()
						{
							PixelLocation = { PositionX = (int)x, PositionY = (int)y },
							PointerOptions = InjectedInputPointerOptions.FirstButton
								| InjectedInputPointerOptions.PointerUp
						}
					}
				});
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

	private const uint GA_ROOT = 2;

	private const uint SWP_NOSIZE = 0x0001;
	private const uint SWP_NOZORDER = 0x0004;
	private const uint SWP_NOACTIVATE = 0x0010;
	private const int SM_XVIRTUALSCREEN = 76;
	private const int SM_YVIRTUALSCREEN = 77;
	private const int SM_CXVIRTUALSCREEN = 78;
	private const int SM_CYVIRTUALSCREEN = 79;

	/// <summary>
	/// Converts window-client-relative DIP coordinates to screen pixel coordinates.
	/// If the computed screen position falls outside the virtual screen bounds
	/// (e.g., the window extends off-screen), the window is repositioned first
	/// to ensure the point is reachable by cursor and touch input.
	/// </summary>
	private static POINT GetOnScreenPoint(double clientDipX, double clientDipY, double scale)
	{
		var hwnd = GetActiveWindow();
		if (hwnd == IntPtr.Zero)
		{
			hwnd = GetForegroundWindow();
		}
		if (hwnd == IntPtr.Zero)
		{
			hwnd = Process.GetCurrentProcess().MainWindowHandle;
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
	/// Moves the top-level ancestor window so the given child HWND's client area
	/// origin maps to screen (100, 100), ensuring elements near the top-left
	/// of the client area are reachable by the OS cursor.
	/// SetWindowPos on child windows only repositions them within their parent,
	/// so we must find and move the root ancestor.
	/// </summary>
	private static void EnsureClientAreaOnScreen(IntPtr hwnd)
	{
		// Use Process.MainWindowHandle to reliably get the top-level window.
		// GetActiveWindow returns a XAML-island child HWND, and GetAncestor(GA_ROOT)
		// may not traverse the WinUI 3 HWND hierarchy correctly.
		var rootHwnd = Process.GetCurrentProcess().MainWindowHandle;
		if (rootHwnd == IntPtr.Zero)
		{
			rootHwnd = GetAncestor(hwnd, GA_ROOT);
		}
		if (rootHwnd == IntPtr.Zero)
		{
			return; // Cannot determine top-level window; skip repositioning.
		}

		var clientOrigin = new POINT();
		ClientToScreen(hwnd, ref clientOrigin);

		GetWindowRect(rootHwnd, out var windowRect);

		// Shift the root window so the child's client area origin
		// lands at screen (100, 100).
		int newLeft = windowRect.left + (100 - clientOrigin.x);
		int newTop = windowRect.top + (100 - clientOrigin.y);

		SetWindowPos(rootHwnd, IntPtr.Zero, newLeft, newTop, 0, 0,
			SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

		// Allow the OS to fully process the window position change
		// before subsequent ClientToScreen calls read updated coordinates.
		System.Threading.Thread.Sleep(500);
	}
#endif
}

#endif