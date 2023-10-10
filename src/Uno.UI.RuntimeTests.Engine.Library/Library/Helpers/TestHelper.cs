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

namespace Uno.UI.RuntimeTests;

public static partial class TestHelper
{
	public static TimeSpan DefaultTimeout => Debugger.IsAttached ? TimeSpan.FromMinutes(60) : TimeSpan.FromSeconds(1);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	public static async ValueTask WaitFor(Func<bool> predicate, CancellationToken ct)
		=> await WaitFor(async _ => predicate(), ct);

	/// <summary>
	/// Wait until a specified <paramref name="predicate"/> is met. 
	/// </summary>
	/// <param name="predicate">Predicate to evaluate repeatedly until it returns true.</param>
	/// <param name="ct">A cancellation token to cancel the wait operation.</param>
	public static async ValueTask WaitFor(Func<CancellationToken, ValueTask<bool>> predicate, CancellationToken ct)
	{
		using var timeout = new CancellationTokenSource(DefaultTimeout);
		try
		{
			ct = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token).Token;

			var interval = Math.Min(1000, (int)(DefaultTimeout.TotalMilliseconds / 100));
			var steps = DefaultTimeout.TotalMilliseconds / interval;

			for (var i = 0; i < steps; i++)
			{
				ct.ThrowIfCancellationRequested();

				if (await predicate(ct))
				{
					return;
				}

				await Task.Delay(interval, ct);
			}

			throw new TimeoutException();
		}
		catch (OperationCanceledException) when (timeout.IsCancellationRequested)
		{
			throw new TimeoutException();
		}
	}
}
#endif