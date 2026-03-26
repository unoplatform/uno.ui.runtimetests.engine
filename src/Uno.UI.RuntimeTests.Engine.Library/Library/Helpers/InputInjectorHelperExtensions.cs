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
#if !HAS_UNO
	/// <summary>
	/// Diagnostic info from the last touch/mouse tap, for assertion messages.
	/// </summary>
	internal static string? LastTapDiagnostics { get; private set; }
#endif

	public static void Tap(this InputInjectorHelper injector, UIElement elt)
	{
		var center = GetAbsoluteCenter(elt);
#if !HAS_UNO
		// Bring the test app window to the foreground before injecting input.
		BringAppToForeground();

		var scale = elt.XamlRoot?.RasterizationScale ?? 1.0;
		var screenPt = GetOnScreenPoint(center.X, center.Y, scale);
		var hwnd = Process.GetCurrentProcess().MainWindowHandle;
		GetWindowRect(hwnd, out var wndRect);

		if (injector.CurrentPointerType == PointerDeviceType.Touch)
		{
			// Use Win32 Touch Injection API directly (user32.dll). The WinRT
			// InputInjector.InjectTouchInput targets the UWP CoreWindow model and
			// silently fails to deliver touch events to WinUI 3 windows (which use
			// Win32 windowing). The Win32 API injects through the WM_POINTER
			// message pipeline that WinUI 3 actually processes.
			var initOk = Win32InitializeTouchInjection(1, TOUCH_FEEDBACK_DEFAULT);
			var initErr = initOk ? 0 : Marshal.GetLastWin32Error();

			// Match Microsoft's official touch injection sample exactly:
			// https://learn.microsoft.com/en-us/windows/win32/input_touchinjection/touch-injection-sample
			var contact = new PointerTouchInfo
			{
				pointerInfo = new PointerInfo
				{
					pointerType = PT_TOUCH,
					pointerId = 0,
					pointerFlags = POINTER_FLAG_DOWN | POINTER_FLAG_INRANGE | POINTER_FLAG_INCONTACT,
					ptPixelLocation = new POINT { x = screenPt.x, y = screenPt.y },
				},
				touchFlags = TOUCH_FLAG_NONE,
				touchMask = TOUCH_MASK_CONTACTAREA | TOUCH_MASK_ORIENTATION | TOUCH_MASK_PRESSURE,
				rcContact = new RECT
				{
					left = screenPt.x - 2, top = screenPt.y - 2,
					right = screenPt.x + 2, bottom = screenPt.y + 2,
				},
				orientation = 90,
				pressure = 32000,
			};

			var downOk = Win32InjectTouchInput(1, ref contact);
			var downErr = downOk ? 0 : Marshal.GetLastWin32Error();

			System.Threading.Thread.Sleep(100);

			contact.pointerInfo.pointerFlags = POINTER_FLAG_UP;
			var upOk = Win32InjectTouchInput(1, ref contact);
			var upErr = upOk ? 0 : Marshal.GetLastWin32Error();

			var ptrSize = Marshal.SizeOf<PointerInfo>();
			var touchSize = Marshal.SizeOf<PointerTouchInfo>();

			LastTapDiagnostics =
				$"Mode=Touch(Win32), DipCenter=({center.X:F1},{center.Y:F1}), " +
				$"ScreenPt=({screenPt.x},{screenPt.y}), Scale={scale}, " +
				$"Init={initOk}(err={initErr}), Down={downOk}(err={downErr}), Up={upOk}(err={upErr}), " +
				$"StructSize=({ptrSize},{touchSize}), " +
				$"WindowRect=({wndRect.left},{wndRect.top},{wndRect.right},{wndRect.bottom}), " +
				$"HWND=0x{hwnd.ToInt64():X}";
			return;
		}

		if (injector.CurrentPointerType == PointerDeviceType.Mouse)
		{
			injector.InjectMouseInput(injector.Mouse.ReleaseAny());

			// Use ABSOLUTE mouse positioning through the InputInjector.
			// SetCursorPos only moves the OS cursor; the InputInjector maintains
			// its own internal position for injected events. Press() and Release()
			// carry DeltaX=DeltaY=0, meaning they land at the InputInjector's
			// position — NOT the OS cursor. With Absolute|VirtualDesk, DeltaX/DeltaY
			// become normalized [0..65535] screen coordinates for the virtual desktop.
			var vLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
			var vTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
			var vWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
			var vHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

			var normalizedX = (int)((screenPt.x - vLeft) * 65535.0 / vWidth);
			var normalizedY = (int)((screenPt.y - vTop) * 65535.0 / vHeight);

			injector.InjectMouseInput(new InjectedInputMouseInfo
			{
				MouseOptions = InjectedInputMouseOptions.Move
					| InjectedInputMouseOptions.Absolute
					| InjectedInputMouseOptions.VirtualDesk,
				DeltaX = normalizedX,
				DeltaY = normalizedY,
			});
			System.Threading.Thread.Sleep(50);

			injector.Mouse.SetTrackedPosition(center.X, center.Y);
			injector.InjectMouseInput(injector.Mouse.Press());
			System.Threading.Thread.Sleep(50);
			injector.InjectMouseInput(injector.Mouse.Release());

			LastTapDiagnostics =
				$"Mode=Mouse, DipCenter=({center.X:F1},{center.Y:F1}), " +
				$"ScreenPt=({screenPt.x},{screenPt.y}), " +
				$"Normalized=({normalizedX},{normalizedY}), " +
				$"VDesk=({vLeft},{vTop},{vWidth},{vHeight}), Scale={scale}, " +
				$"WindowRect=({wndRect.left},{wndRect.top},{wndRect.right},{wndRect.bottom}), " +
				$"HWND=0x{hwnd.ToInt64():X}";
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

				// Match Microsoft's official touch injection sample exactly:
				// https://learn.microsoft.com/en-us/windows/uwp/design/input/input-injection
				// Key flags: Down = PointerDown|InContact|New (NO FirstButton/InRange).
				// Up = PointerUp only (NO coordinates, NO FirstButton).
				// Contact area = 30px radius (larger than a pinpoint touch).
				var downInfo = new InjectedInputTouchInfo
				{
					Contact = new InjectedInputRectangle { Left = 30, Top = 30, Right = 30, Bottom = 30 },
					Pressure = 1.0,
					TouchParameters = InjectedInputTouchParameters.Pressure | InjectedInputTouchParameters.Contact,
					PointerInfo = new()
					{
						PointerId = 42,
						PixelLocation = new() { PositionX = (int)x, PositionY = (int)y },
						PointerOptions = InjectedInputPointerOptions.New
							| InjectedInputPointerOptions.PointerDown
							| InjectedInputPointerOptions.InContact,
						TimeOffsetInMilliseconds = 0
					}
				};
				injector.Injector.InjectTouchInput(new[] { downInfo });

				// Separate OS timestamps for Down and Up events.
				System.Threading.Thread.Sleep(100);

				var upInfo = new InjectedInputTouchInfo
				{
					PointerInfo = new()
					{
						PointerId = 42,
						PointerOptions = InjectedInputPointerOptions.PointerUp
					}
				};
				injector.Injector.InjectTouchInput(new[] { upInfo });

				// Allow the OS to finish processing the touch events
				// before tearing down the injection session.
				System.Threading.Thread.Sleep(50);
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

	// Win32 Touch Injection API (user32.dll) — delivers touch events through
	// the WM_POINTER pipeline, which WinUI 3 processes. The WinRT
	// InputInjector.InjectTouchInput targets CoreWindow and silently fails
	// on WinUI 3's Win32 windowing model.
	[DllImport("user32.dll", EntryPoint = "InitializeTouchInjection", SetLastError = true)]
	private static extern bool Win32InitializeTouchInjection(uint maxCount, uint dwMode);

	[DllImport("user32.dll", EntryPoint = "InjectTouchInput", SetLastError = true)]
	private static extern bool Win32InjectTouchInput(uint count, ref PointerTouchInfo contacts);

	private const uint TOUCH_FEEDBACK_DEFAULT = 0x1;
	private const uint PT_TOUCH = 0x00000002;
	private const uint POINTER_FLAG_INRANGE = 0x00000002;
	private const uint POINTER_FLAG_INCONTACT = 0x00000004;
	private const uint POINTER_FLAG_DOWN = 0x00010000;
	private const uint POINTER_FLAG_UP = 0x00040000;
	private const uint TOUCH_FLAG_NONE = 0x00000000;
	private const uint TOUCH_MASK_CONTACTAREA = 0x00000001;
	private const uint TOUCH_MASK_ORIENTATION = 0x00000002;
	private const uint TOUCH_MASK_PRESSURE = 0x00000004;

	[StructLayout(LayoutKind.Sequential)]
	private struct PointerInfo
	{
		public uint pointerType;
		public uint pointerId;
		public uint frameId;
		public uint pointerFlags;
		public IntPtr sourceDevice;
		public IntPtr hwndTarget;
		public POINT ptPixelLocation;
		public POINT ptHimetricLocation;
		public POINT ptPixelLocationRaw;
		public POINT ptHimetricLocationRaw;
		public uint dwTime;
		public uint historyCount;
		public int inputData;
		public uint dwKeyStates;
		public ulong performanceCount;
		public int buttonChangeType;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct PointerTouchInfo
	{
		public PointerInfo pointerInfo;
		public uint touchFlags;
		public uint touchMask;
		public RECT rcContact;
		public RECT rcContactRaw;
		public uint orientation;
		public uint pressure;
	}

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