namespace Uno.UI.RuntimeTests.Engine.Wasm.Runner;

/// <summary>
/// Handling of the <c>--timeout</c> option, in seconds, where 0 means no timeout.
/// </summary>
internal static class RunnerTimeout
{
	/// <summary>
	/// Largest accepted value, in seconds (~49.7 days): <see cref="CancellationTokenSource.CancelAfter(TimeSpan)"/>
	/// rejects delays above <c>uint.MaxValue - 1</c> milliseconds.
	/// </summary>
	internal const int MaxSeconds = (int)((uint.MaxValue - 1) / 1000);

	/// <summary>
	/// Returns an error message when <paramref name="seconds"/> is not a valid <c>--timeout</c> value, otherwise null.
	/// </summary>
	internal static string? Validate(int seconds)
		=> seconds is < 0 or > MaxSeconds
			? $"--timeout must be between 0 and {MaxSeconds} seconds (0 = no timeout), but was {seconds}."
			: null;

	/// <summary>
	/// Creates a source canceled when <paramref name="cancellationToken"/> is canceled or, unless
	/// <paramref name="seconds"/> is 0, when the timeout elapses.
	/// </summary>
	internal static CancellationTokenSource CreateCancellationTokenSource(int seconds, CancellationToken cancellationToken)
	{
		var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		if (seconds > 0)
		{
			cts.CancelAfter(TimeSpan.FromSeconds(seconds));
		}

		return cts;
	}
}
