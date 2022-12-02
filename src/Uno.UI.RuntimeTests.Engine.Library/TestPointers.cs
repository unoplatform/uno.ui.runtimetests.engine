using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Input;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

namespace Uno.UI.RuntimeTests;

public partial class Pointers
{
	private static Pointers? _instance;
	public static Pointers Instance => _instance ??= new();

	/// <summary>
	/// Returns the <see cref="Instance"/> or `null` if it has not yet been initialized.
	/// </summary>
	/// <returns></returns>
	public static Pointers? TryGetInstance() => _instance;

	private Pointers()
	{
		Injector = InputInjector.TryCreate() ?? throw new InvalidOperationException(
			"Cannot create input injector. "
			+ "This usually means that the pointer injection API is not implemented for the current platform. "
			+ "Consider to disable tests that are using pointer injection for this platform.");

		Mouse = new MouseHelper(Injector);
	}

	/// <summary>
	/// Raw access to the input injector for custom raw input injection.
	/// </summary>
	public InputInjector Injector { get; }

	/// <summary>
	/// Factory for mouse injected inputs info.
	/// </summary>
	public MouseHelper Mouse { get; }

	/// <summary>
	/// Gets the default pointer type for the current platform
	/// </summary>
	public static PointerDeviceType DefaultPointerType
#if __IOS__ || __ANDROID__
		=> PointerDeviceType.Touch;
#else
		=> PointerDeviceType.Mouse;
#endif

	/// <summary>
	/// Gets the current pointer type used to inject pointer.
	/// </summary>
	public PointerDeviceType CurrentPointerType { get; private set; } = DefaultPointerType;

	/// <summary>
	/// Sets the <see cref="CurrentPointerType"/>.
	/// </summary>
	/// <param name="type">Type of pointer to use.</param>
	/// <returns>A disposable that will restore the previous type on dispose.</returns>
	public IDisposable SetPointerType(PointerDeviceType type)
	{
		var previous = CurrentPointerType;
		CurrentPointerType = type;

		return new PointerSubscription(this, previous, type);
	}

	/// <summary>
	/// Make sure to release any pressed pointer.
	/// </summary>
	public void CleanupPointers()
	{
		InjectMouseInput(Mouse.ReleaseAny());
		InjectMouseInput(Mouse.MoveTo(0, 0));
	}

	public void InjectTouchInput(IEnumerable<InjectedInputTouchInfo?> input)
		=> Injector.InjectTouchInput(input.Where(i => i is not null).Cast<InjectedInputTouchInfo>());

	public void InjectTouchInput(params InjectedInputTouchInfo?[] input)
		=> Injector.InjectTouchInput(input.Where(i => i is not null).Cast<InjectedInputTouchInfo>());

	public void InjectMouseInput(IEnumerable<InjectedInputMouseInfo?> input)
		=> Injector.InjectMouseInput(input.Where(i => i is not null).Cast<InjectedInputMouseInfo>());

	public void InjectMouseInput(params InjectedInputMouseInfo?[] input)
		=> Injector.InjectMouseInput(input.Where(i => i is not null).Cast<InjectedInputMouseInfo>());


	private record PointerSubscription(Pointers Injector, PointerDeviceType Previous, PointerDeviceType Current) : IDisposable
	{
		/// <inheritdoc />
		public void Dispose()
			=> Injector.CurrentPointerType = Previous;
	}

#pragma warning disable CA1822 // Mark members as static
	public class MouseHelper
	{
		private readonly InputInjector _input;

		private PointerPoint _last = new(default);

		public MouseHelper(InputInjector input)
		{
			_input = input;
		}

		public InjectedInputMouseInfo Press()
			=> new()
			{
				TimeOffsetInMilliseconds = 1,
				MouseOptions = InjectedInputMouseOptions.LeftDown,
			};

		public InjectedInputMouseInfo Release()
			=> new()
			{
				TimeOffsetInMilliseconds = 1,
				MouseOptions = InjectedInputMouseOptions.LeftUp,
			};

		public InjectedInputMouseInfo? ReleaseAny()
		{
			var options = default(InjectedInputMouseOptions);

#if HAS_UNO
			var current = _last;
			if (current.Properties.IsLeftButtonPressed)
			{
				options |= InjectedInputMouseOptions.LeftUp;
			}

			if (current.Properties.IsMiddleButtonPressed)
			{
				options |= InjectedInputMouseOptions.MiddleUp;
			}

			if (current.Properties.IsRightButtonPressed)
			{
				options |= InjectedInputMouseOptions.RightUp;
			}

			if (current.Properties.IsXButton1Pressed)
			{
				options |= InjectedInputMouseOptions.XUp;
			}
#else
			options = InjectedInputMouseOptions.LeftUp
				| InjectedInputMouseOptions.MiddleUp
				| InjectedInputMouseOptions.RightUp
				| InjectedInputMouseOptions.XUp;
#endif

			return options is default(InjectedInputMouseOptions)
				? null
				: new()
				{
					TimeOffsetInMilliseconds = 1,
					MouseOptions = options
				};
		}

		public InjectedInputMouseInfo MoveBy(int deltaX, int deltaY)
			=> new()
			{
				DeltaX = deltaX,
				DeltaY = deltaY,
				TimeOffsetInMilliseconds = 1,
				MouseOptions = InjectedInputMouseOptions.MoveNoCoalesce,
			};

		public IEnumerable<InjectedInputMouseInfo> MoveTo(double x, double y, int? steps = null)
		{
			Point Current()
#if HAS_UNO
				=> _last.Position;
#else
				=> CoreWindow.GetForCurrentThread().PointerPosition;
#endif

			var deltaX = x - Current().X;
			var deltaY = y - Current().Y;

			steps ??= (int)Math.Min(Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)), 512);
			if (steps is 0)
			{
				yield break;
			}

			var stepX = deltaX / steps.Value;
			var stepY = deltaY / steps.Value;

			stepX = stepX is > 0 ? Math.Ceiling(stepX) : Math.Floor(stepX);
			stepY = stepY is > 0 ? Math.Ceiling(stepY) : Math.Floor(stepY);

			for (var step = 0; step <= steps && (stepX is not 0 || stepY is not 0); step++)
			{
				yield return MoveBy((int)stepX, (int)stepY);

				if (Math.Abs(Current().X - x) < stepX)
				{
					stepX = 0;
				}

				if (Math.Abs(Current().Y - y) < stepY)
				{
					stepY = 0;
				}
			}
		}
	}
}