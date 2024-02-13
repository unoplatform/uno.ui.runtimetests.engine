#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
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
		if (!await TryWaitFor(predicate, timeout, ct).ConfigureAwait(false))
		{
			throw new TimeoutException($"Operation has been cancelled after {timeout:g}.");
		}
	}

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask<bool> TryWaitFor(Func<bool> predicate, CancellationToken ct = default)
		=> await TryWaitFor(async _ => predicate(), DefaultTimeout, ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask<bool> TryWaitFor(Func<bool> predicate, int timeoutMs, CancellationToken ct = default)
		=> await TryWaitFor(async _ => predicate(), TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async ValueTask<bool> TryWaitFor(Func<bool> predicate, TimeSpan timeout, CancellationToken ct = default)
		=> await TryWaitFor(async _ => predicate(), timeout, ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask<bool> TryWaitFor(Func<CancellationToken, ValueTask<bool>> predicate, CancellationToken ct = default)
		=> TryWaitFor(predicate, DefaultTimeout, ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask<bool> TryWaitFor(Func<CancellationToken, ValueTask<bool>> predicate, int timeoutMs, CancellationToken ct = default)
		=> TryWaitFor(predicate, TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	public static async ValueTask<bool> TryWaitFor(Func<CancellationToken, ValueTask<bool>> predicate, TimeSpan timeout, CancellationToken ct = default)
	{
		using var timeoutSrc = new CancellationTokenSource(timeout);
		try
		{
			ct = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutSrc.Token).Token;

			var interval = Math.Min(1000, (int)(timeout.TotalMilliseconds / 100));
			var steps = timeout.TotalMilliseconds / interval;

			for (var i = 0; i < steps; i++)
			{
				if (ct.IsCancellationRequested)
				{
					return false;
				}

				if (await predicate(ct).ConfigureAwait(false))
				{
					return true;
				}

				if (i < steps - 1)
				{
					await Task.Delay(interval, ct).ConfigureAwait(false);
				}
			}

			return false;
		}
		catch (OperationCanceledException) when (timeoutSrc.IsCancellationRequested)
		{
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask WaitFor(Task task, CancellationToken ct = default)
		=> WaitFor(task, DefaultTimeout, ct);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask WaitFor(Task task, int timeoutMs, CancellationToken ct = default)
		=> WaitFor(task, TimeSpan.FromMilliseconds(timeoutMs), ct);

	public static async ValueTask WaitFor(Task task, TimeSpan timeout, CancellationToken ct = default)
	{
		try
		{
			if (!await TryWaitFor(task, timeout, ct).ConfigureAwait(false))
			{
				throw new TimeoutException($"Operation has been cancelled after {timeout:g}.");
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested && !task.IsCanceled)
		{
			throw new TimeoutException($"Operation has been cancelled after {timeout:g}.");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask<bool> TryWaitFor(Task task, CancellationToken ct = default)
		=> TryWaitFor(task, DefaultTimeout, ct);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask<bool> TryWaitFor(Task task, int timeoutMs, CancellationToken ct = default)
		=> TryWaitFor(task, TimeSpan.FromMilliseconds(timeoutMs), ct);

	public static async ValueTask<bool> TryWaitFor(Task task, TimeSpan timeout, CancellationToken ct = default)
		=> await Task.WhenAny(task, Task.Delay(timeout, ct)).ConfigureAwait(false) == task;
}
#endif