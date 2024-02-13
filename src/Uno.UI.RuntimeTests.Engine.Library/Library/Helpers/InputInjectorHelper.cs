#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

namespace Uno.UI.RuntimeTests;

public partial class InputInjectorHelper
{
	private static InputInjectorHelper? _current;

	/// <summary>
	/// Gets the singleton input injector that can be used to simulate pointers interaction on the application.
	/// </summary>
	public static InputInjectorHelper Current => _current ??= new();

	/// <summary>
	/// Returns the <see cref="Current"/> or `null` if it has not yet been initialized.
	/// </summary>
	public static InputInjectorHelper? TryGetCurrent() => _current;

	private InputInjectorHelper()
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

	/// <summary>
	/// Injects some touch infos to simulate finger interaction on the application
	/// </summary>
	public void InjectTouchInput(IEnumerable<InjectedInputTouchInfo?> input)
		=> Injector.InjectTouchInput(input.Where(i => i is not null).Cast<InjectedInputTouchInfo>());

	/// <summary>
	/// Injects some touch infos to simulate finger interaction on the application
	/// </summary>
	public void InjectTouchInput(params InjectedInputTouchInfo?[] input)
		=> Injector.InjectTouchInput(input.Where(i => i is not null).Cast<InjectedInputTouchInfo>());

	/// <summary>
	/// Injects some mouse infos to simulate mouse interaction on the application
	/// </summary>
	public void InjectMouseInput(IEnumerable<InjectedInputMouseInfo?> input)
		=> Injector.InjectMouseInput(input.Where(i => i is not null).Cast<InjectedInputMouseInfo>());

	/// <summary>
	/// Injects some mouse infos to simulate mouse interaction on the application
	/// </summary>
	public void InjectMouseInput(params InjectedInputMouseInfo?[] input)
		=> Injector.InjectMouseInput(input.Where(i => i is not null).Cast<InjectedInputMouseInfo>());

	private sealed record PointerSubscription(InputInjectorHelper Injector, PointerDeviceType Previous, PointerDeviceType Current) : IDisposable
	{
		/// <inheritdoc />
		public void Dispose()
			=> Injector.CurrentPointerType = Previous;
	}
}

#endif