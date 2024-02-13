#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
namespace Uno.UI.RuntimeTests;

public static partial class AsyncAssert
{
	#region IsTrue
	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(
		global::System.Func<bool> condition,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(
		global::System.Func<bool> condition,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(
		global::System.Func<bool> condition,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(global::System.Func<bool> condition, string message, global::System.Threading.CancellationToken ct = default)
		=> IsTrueCore(condition, message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(global::System.Func<bool> condition, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> IsTrueCore(condition, message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(global::System.Func<bool> condition, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> IsTrueCore(condition, message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(
		global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(
		global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(
		global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition, string message, global::System.Threading.CancellationToken ct = default)
		=> IsTrueCore(condition, message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> IsTrueCore(condition, message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsTrue(global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> IsTrueCore(condition, message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async global::System.Threading.Tasks.ValueTask IsTrueCore(global::System.Func<bool> condition, string reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(condition, timeout, ct);

		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(condition(), reason);
	}

	private static async global::System.Threading.Tasks.ValueTask IsTrueCore(global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition, string reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => await condition().ConfigureAwait(false), timeout, ct);

		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(await condition().ConfigureAwait(false), reason);
	}
	#endregion

	#region IsFalse
	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(
		global::System.Func<bool> condition,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(
		global::System.Func<bool> condition,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(
		global::System.Func<bool> condition,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(global::System.Func<bool> condition, string message, global::System.Threading.CancellationToken ct = default)
		=> IsFalseCore(condition, message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(global::System.Func<bool> condition, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> IsFalseCore(condition, message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(global::System.Func<bool> condition, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> IsFalseCore(condition, message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(
		global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(
		global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(
		global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition, string message, global::System.Threading.CancellationToken ct = default)
		=> IsFalseCore(condition, message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> IsFalseCore(condition, message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsFalse(global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> IsFalseCore(condition, message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async global::System.Threading.Tasks.ValueTask IsFalseCore(global::System.Func<bool> condition, string reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(() => !condition(), timeout, ct);

		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(condition(), reason);
	}

	private static async global::System.Threading.Tasks.ValueTask IsFalseCore(global::System.Func<global::System.Threading.Tasks.ValueTask<bool>> condition, string reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => !await condition().ConfigureAwait(false), timeout, ct);

		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(await condition().ConfigureAwait(false), reason);
	}
	#endregion

	#region IsNull
	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(
		global::System.Func<T> value,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(
		global::System.Func<T> value,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(
		global::System.Func<T> value,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(global::System.Func<T> value, string message, global::System.Threading.CancellationToken ct = default)
		=> IsNullCore(value, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(global::System.Func<T> value, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> IsNullCore(value, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(global::System.Func<T> value, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> IsNullCore(value, a => message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value, string message, global::System.Threading.CancellationToken ct = default)
		=> IsNullCore<T>(value, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> IsNullCore<T>(value, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNull<T>(global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> IsNullCore<T>(value, a => message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async global::System.Threading.Tasks.ValueTask IsNullCore<T>(global::System.Func<T> value, global::System.Func<T, string> reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(() => object.Equals(null, value()), timeout, ct);

		var a = value();
		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(a, reason(a));
	}

	private static async global::System.Threading.Tasks.ValueTask IsNullCore<T>(global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value, global::System.Func<T, string> reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => object.Equals(null, await value().ConfigureAwait(false)), timeout, ct);

		var a = await value().ConfigureAwait(false);
		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(a, reason(a));
	}
	#endregion

	#region IsNotNull
	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(
		global::System.Func<T> value,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(
		global::System.Func<T> value,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(
		global::System.Func<T> value,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(global::System.Func<T> value, string message, global::System.Threading.CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(global::System.Func<T> value, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(global::System.Func<T> value, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value, string message, global::System.Threading.CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask IsNotNull<T>(global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async global::System.Threading.Tasks.ValueTask IsNotNullCore<T>(global::System.Func<T> value, global::System.Func<T, string> reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(() => !object.Equals(null, value()), timeout, ct);

		var a = value();
		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(a, reason(a));
	}

	private static async global::System.Threading.Tasks.ValueTask IsNotNullCore<T>(global::System.Func<global::System.Threading.Tasks.ValueTask<T>> value, global::System.Func<T, string> reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => !object.Equals(null, await value().ConfigureAwait(false)), timeout, ct);

		var a = await value().ConfigureAwait(false);
		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(a, reason(a));
	}
	#endregion

	#region AreEqual
	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(
		T expected,
		global::System.Func<T> actual,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(
		T expected,
		global::System.Func<T> actual,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(
		T expected,
		global::System.Func<T> actual,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(T expected, global::System.Func<T> actual, string message, global::System.Threading.CancellationToken ct = default)
		=> AreEqualCore(expected, actual, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(T expected, global::System.Func<T> actual, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> AreEqualCore(expected, actual, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(T expected, global::System.Func<T> actual, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> AreEqualCore(expected, actual, a => message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(
		T expected,
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(
		T expected,
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(
		T expected,
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, string message, global::System.Threading.CancellationToken ct = default)
		=> AreEqualCore(expected, actual, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> AreEqualCore(expected, actual, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreEqual<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> AreEqualCore(expected, actual, a => message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async global::System.Threading.Tasks.ValueTask AreEqualCore<T>(T expected, global::System.Func<T> actual, global::System.Func<T, string> reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(() => object.Equals(expected, actual()), timeout, ct);

		var a = actual();
		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(expected, a, reason(a));
	}

	private static async global::System.Threading.Tasks.ValueTask AreEqualCore<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, global::System.Func<T, string> reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => object.Equals(expected, await actual().ConfigureAwait(false)), timeout, ct);

		var a = await actual().ConfigureAwait(false);
		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(expected, a, reason(a));
	}
	#endregion

	#region AreNotEqual
	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(
		T expected,
		global::System.Func<T> actual,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(
		T expected,
		global::System.Func<T> actual,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(
		T expected,
		global::System.Func<T> actual,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(T expected, global::System.Func<T> actual, string message, global::System.Threading.CancellationToken ct = default)
		=> AreNotEqualCore(expected, actual, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(T expected, global::System.Func<T> actual, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> AreNotEqualCore(expected, actual, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(T expected, global::System.Func<T> actual, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> AreNotEqualCore(expected, actual, a => message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(
		T expected,
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(
		T expected,
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual,
		global::System.TimeSpan timeout,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="actualExpression">For debug purposes.</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(
		T expected,
		global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual,
		int timeoutMs,
		global::System.Threading.CancellationToken ct = default,
		[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
		[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({(global::System.IO.Path.GetFileName(file))}@{line})", global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, string message, global::System.Threading.CancellationToken ct = default)
		=> AreNotEqualCore(expected, actual, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
		=> AreNotEqualCore(expected, actual, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static global::System.Threading.Tasks.ValueTask AreNotEqual<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
		=> AreNotEqualCore(expected, actual, a => message, global::System.TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async global::System.Threading.Tasks.ValueTask AreNotEqualCore<T>(T expected, global::System.Func<T> actual, global::System.Func<T, string> reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(() => !object.Equals(expected, actual()), timeout, ct);

		var a = actual();
		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreNotEqual(expected, a, reason(a));
	}

	private static async global::System.Threading.Tasks.ValueTask AreNotEqualCore<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, global::System.Func<T, string> reason, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => !object.Equals(expected, await actual().ConfigureAwait(false)), timeout, ct);

		var a = await actual().ConfigureAwait(false);
		global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreNotEqual(expected, a, reason(a));
	}
	#endregion
}
#endif
