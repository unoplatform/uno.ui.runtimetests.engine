#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Uno.UI.RuntimeTests;

public static partial class TestHelper
{
	public static TimeSpan DefaultTimeout => Debugger.IsAttached ? TimeSpan.FromMinutes(60) : TimeSpan.FromSeconds(1);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask WaitFor(Func<bool> predicate, CancellationToken ct = default)
		=> await WaitFor(async _ => predicate(), DefaultTimeout, ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask WaitFor(Func<bool> predicate, int timeoutMs, CancellationToken ct = default)
		=> await WaitFor(async _ => predicate(), TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask WaitFor(Func<bool> predicate, TimeSpan timeout, CancellationToken ct = default)
		=> await WaitFor(async _ => predicate(), timeout, ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask WaitFor(Func<CancellationToken, ValueTask<bool>> predicate, CancellationToken ct = default)
		=> WaitFor(predicate, DefaultTimeout, ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask WaitFor(Func<CancellationToken, ValueTask<bool>> predicate, int timeoutMs, CancellationToken ct = default)
		=> WaitFor(predicate, TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	public static async ValueTask WaitFor(Func<CancellationToken, ValueTask<bool>> predicate, TimeSpan timeout, CancellationToken ct = default)
	{
		using var timeoutSrc = new CancellationTokenSource(timeout);
		try
		{
			ct = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutSrc.Token).Token;

			var interval = Math.Min(1000, (int)(timeout.TotalMilliseconds / 100));
			var steps = timeout.TotalMilliseconds / interval;

			for (var i = 0; i < steps; i++)
			{
				ct.ThrowIfCancellationRequested();

				if (await predicate(ct))
				{
					return;
				}

				if (i < steps - 1)
				{
					await Task.Delay(interval, ct);
				}
			}

			throw new TimeoutException($"Operation has been cancelled after {timeout:g}.");
		}
		catch (OperationCanceledException) when (timeoutSrc.IsCancellationRequested)
		{
			throw new TimeoutException($"Operation has been cancelled after {timeout:g}.");
		}
	}

	/// <summary>
	/// Wait until a specified <paramref name="actual"/> value provider equals the <paramref name="expected"/> value.
	/// </summary>
	/// <typeparam name="T">The type of the value to validate.</typeparam>
	/// <param name="actual">Actual provider to evaluate repeatedly until it returns the <paramref name="expected"/> value.</param>
	/// <param name="expected">The expected value to wait for.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask WaitFor<T>(Func<T> actual, T expected, CancellationToken ct = default)
		where T : IEquatable<T>
		=> await WaitFor(async _ => actual(), expected, DefaultTimeout, ct);

	/// <summary>
	/// Wait until a specified <paramref name="actual"/> value provider equals the <paramref name="expected"/> value.
	/// </summary>
	/// <typeparam name="T">The type of the value to validate.</typeparam>
	/// <param name="actual">Actual provider to evaluate repeatedly until it returns the <paramref name="expected"/> value.</param>
	/// <param name="expected">The expected value to wait for.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask WaitFor<T>(Func<T> actual, T expected, int timeoutMs, CancellationToken ct = default)
		where T : IEquatable<T>
		=> await WaitFor(async _ => actual(), expected, TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Wait until a specified <paramref name="actual"/> value provider equals the <paramref name="expected"/> value.
	/// </summary>
	/// <typeparam name="T">The type of the value to validate.</typeparam>
	/// <param name="actual">Actual provider to evaluate repeatedly until it returns the <paramref name="expected"/> value.</param>
	/// <param name="expected">The expected value to wait for.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask WaitFor<T>(Func<T> actual, T expected, TimeSpan timeout, CancellationToken ct = default)
		where T : IEquatable<T>
		=> await WaitFor(async _ => actual(), expected, timeout, ct);

	/// <summary>
	/// Wait until a specified <paramref name="actual"/> value provider equals the <paramref name="expected"/> value.
	/// </summary>
	/// <typeparam name="T">The type of the value to validate.</typeparam>
	/// <param name="actual">Actual provider to evaluate repeatedly until it returns the <paramref name="expected"/> value.</param>
	/// <param name="expected">The expected value to wait for.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask WaitFor<T>(Func<CancellationToken, ValueTask<T>> actual, T expected, CancellationToken ct = default)
		where T : IEquatable<T>
		=> WaitFor(actual, expected, DefaultTimeout, ct);

	/// <summary>
	/// Wait until a specified <paramref name="actual"/> value provider equals the <paramref name="expected"/> value.
	/// </summary>
	/// <typeparam name="T">The type of the value to validate.</typeparam>
	/// <param name="actual">Actual provider to evaluate repeatedly until it returns the <paramref name="expected"/> value.</param>
	/// <param name="expected">The expected value to wait for.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask WaitFor<T>(Func<CancellationToken, ValueTask<T>> actual, T expected, int timeoutMs, CancellationToken ct = default)
		where T : IEquatable<T>
		=> WaitFor(actual, expected, TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Wait until a specified <paramref name="actual"/> value provider equals the <paramref name="expected"/> value.
	/// </summary>
	/// <typeparam name="T">The type of the value to validate.</typeparam>
	/// <param name="actual">Actual provider to evaluate repeatedly until it returns the <paramref name="expected"/> value.</param>
	/// <param name="expected">The expected value to wait for.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	public static async ValueTask WaitFor<T>(Func<CancellationToken, ValueTask<T>> actual, T expected, TimeSpan timeout, CancellationToken ct = default)
		where T : IEquatable<T>
	{
		using var timeoutSrc = new CancellationTokenSource(timeout);

		var actualValue = default(T);
		try
		{
			ct = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutSrc.Token).Token;

			var interval = Math.Min(1000, (int)(timeout.TotalMilliseconds / 100));
			var steps = timeout.TotalMilliseconds / interval;

			for (var i = 0; i < steps; i++)
			{
				ct.ThrowIfCancellationRequested();

				if (expected.Equals(actualValue = await actual(ct)))
				{
					return;
				}

				if (i < steps - 1)
				{
					await Task.Delay(interval, ct);
				}
			}

			throw new TimeoutException($"Actual value is '{actualValue}' instead of the expected '{expected}' after {timeout:g}.");
		}
		catch (OperationCanceledException) when (timeoutSrc.IsCancellationRequested)
		{
			throw new TimeoutException($"Actual value is '{actualValue}' instead of the expected '{expected}' after {timeout:g}.");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask WaitFor(Task task, CancellationToken ct = default)
		=> WaitFor(task, ct);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask WaitFor(Task task, int timeoutMs, CancellationToken ct = default)
		=> WaitFor(task, TimeSpan.FromMilliseconds(timeoutMs), ct);

	public static async ValueTask WaitFor(Task task, TimeSpan timeout, CancellationToken ct = default)
	{
		var timeoutTask = Task.Delay(timeout, ct);
		try
		{
			if (await Task.WhenAny(task, timeoutTask) == timeoutTask)
			{
				throw new TimeoutException($"Operation has been cancelled after {timeout:g}.");
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested && !task.IsCanceled)
		{
			throw new TimeoutException($"Operation has been cancelled after {timeout:g}.");
		}
	}
}
#endif