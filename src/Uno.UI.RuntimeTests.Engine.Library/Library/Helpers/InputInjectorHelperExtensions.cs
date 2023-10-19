#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml;
#else
using Windows.UI.Xaml;
#endif

namespace Uno.UI.RuntimeTests;

public static partial class InputInjectorHelperExtensions
{
	public static void Tap(this InputInjectorHelper injector, UIElement elt)
		=> injector.TapCoordinates(GetAbsoluteCenter(elt));

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

}

#endif