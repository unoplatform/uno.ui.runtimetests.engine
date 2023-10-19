#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	public static ValueTask IsTrue(
		Func<bool> condition,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static ValueTask IsTrue(
		Func<bool> condition,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({Path.GetFileName(file)}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static ValueTask IsTrue(
		Func<bool> condition,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsTrue(Func<bool> condition, string message, CancellationToken ct = default)
		=> IsTrueCore(condition, message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsTrue(Func<bool> condition, string message, TimeSpan timeout, CancellationToken ct = default)
		=> IsTrueCore(condition, message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsTrue(Func<bool> condition, string message, int timeoutMs, CancellationToken ct = default)
		=> IsTrueCore( condition, message, TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static ValueTask IsTrue(
		Func<ValueTask<bool>> condition,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static ValueTask IsTrue(
		Func<ValueTask<bool>> condition,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({Path.GetFileName(file)}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static ValueTask IsTrue(
		Func<ValueTask<bool>> condition,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsTrueCore(condition, $"{conditionExpression} to be true but found false ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsTrue(Func<ValueTask<bool>> condition, string message, CancellationToken ct = default)
		=> IsTrueCore(condition, message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsTrue(Func<ValueTask<bool>> condition, string message, TimeSpan timeout, CancellationToken ct = default)
		=> IsTrueCore(condition, message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition">The condition the test expects to be true.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsTrue(Func<ValueTask<bool>> condition, string message, int timeoutMs, CancellationToken ct = default)
		=> IsTrueCore(condition, message, TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async ValueTask IsTrueCore(Func<bool> condition, string reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(condition, timeout, ct);

		Assert.IsTrue(condition(), reason);
	}

	private static async ValueTask IsTrueCore(Func<ValueTask<bool>> condition, string reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => await condition().ConfigureAwait(false), timeout, ct);

		Assert.IsTrue(await condition().ConfigureAwait(false), reason);
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
	public static ValueTask IsFalse(
		Func<bool> condition,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static ValueTask IsFalse(
		Func<bool> condition,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({Path.GetFileName(file)}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static ValueTask IsFalse(
		Func<bool> condition,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsFalse(Func<bool> condition, string message, CancellationToken ct = default)
		=> IsFalseCore(condition, message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsFalse(Func<bool> condition, string message, TimeSpan timeout, CancellationToken ct = default)
		=> IsFalseCore(condition, message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsFalse(Func<bool> condition, string message, int timeoutMs, CancellationToken ct = default)
		=> IsFalseCore(condition, message, TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static ValueTask IsFalse(
		Func<ValueTask<bool>> condition,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static ValueTask IsFalse(
		Func<ValueTask<bool>> condition,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({Path.GetFileName(file)}@{line})", timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="conditionExpression">For debug purposes.</param>
	public static ValueTask IsFalse(
		Func<ValueTask<bool>> condition,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("condition")] string conditionExpression = "")
		=> IsFalseCore(condition, $"{conditionExpression} to be false but found true ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsFalse(Func<ValueTask<bool>> condition, string message, CancellationToken ct = default)
		=> IsFalseCore(condition, message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsFalse(Func<ValueTask<bool>> condition, string message, TimeSpan timeout, CancellationToken ct = default)
		=> IsFalseCore(condition, message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition">The condition the test expects to be false.</param>
	/// <param name="message">The message to include in the exception when condition is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsFalse(Func<ValueTask<bool>> condition, string message, int timeoutMs, CancellationToken ct = default)
		=> IsFalseCore(condition, message, TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async ValueTask IsFalseCore(Func<bool> condition, string reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(() => !condition(), timeout, ct);

		Assert.IsFalse(condition(), reason);
	}

	private static async ValueTask IsFalseCore(Func<ValueTask<bool>> condition, string reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => !await condition().ConfigureAwait(false), timeout, ct);

		Assert.IsFalse(await condition().ConfigureAwait(false), reason);
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
	public static ValueTask IsNull<T>(
		Func<T> value,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

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
	public static ValueTask IsNull<T>(
		Func<T> value,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", timeout, ct);

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
	public static ValueTask IsNull<T>(
		Func<T> value,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNull<T>(Func<T> value, string message, CancellationToken ct = default)
		=> IsNullCore(value, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNull<T>(Func<T> value, string message, TimeSpan timeout, CancellationToken ct = default)
		=> IsNullCore(value, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNull<T>(Func<T> value, string message, int timeoutMs, CancellationToken ct = default)
		=> IsNullCore(value, a => message, TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static ValueTask IsNull<T>(
		Func<ValueTask<T>> value,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

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
	public static ValueTask IsNull<T>(
		Func<ValueTask<T>> value,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", timeout, ct);

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
	public static ValueTask IsNull<T>(
		Func<ValueTask<T>> value,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNull<T>(Func<ValueTask<T>> value, string message, CancellationToken ct = default)
		=> IsNullCore<T>(value, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNull<T>(Func<ValueTask<T>> value, string message, TimeSpan timeout, CancellationToken ct = default)
		=> IsNullCore<T>(value, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is null and throws an exception if it is not.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNull<T>(Func<ValueTask<T>> value, string message, int timeoutMs, CancellationToken ct = default)
		=> IsNullCore<T>(value, a => message, TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async ValueTask IsNullCore<T>(Func<T> value, Func<T, string> reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(() => object.Equals(null, value()), timeout, ct);

		var a = value();
		Assert.IsNull(a, reason(a));
	}

	private static async ValueTask IsNullCore<T>(Func<ValueTask<T>> value, Func<T, string> reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => object.Equals(null, await value().ConfigureAwait(false)), timeout, ct);

		var a = await value().ConfigureAwait(false);
		Assert.IsNotNull(a, reason(a));
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
	public static ValueTask IsNotNull<T>(
		Func<T> value,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

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
	public static ValueTask IsNotNull<T>(
		Func<T> value,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", timeout, ct);

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
	public static ValueTask IsNotNull<T>(
		Func<T> value,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNotNull<T>(Func<T> value, string message, CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNotNull<T>(Func<T> value, string message, TimeSpan timeout, CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNotNull<T>(Func<T> value, string message, int timeoutMs, CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	/// <param name="line">For debug purposes.</param>
	/// <param name="file">For debug purposes.</param>
	/// <param name="valueExpression">For debug purposes.</param>
	public static ValueTask IsNotNull<T>(
		Func<ValueTask<T>> value,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

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
	public static ValueTask IsNotNull<T>(
		Func<ValueTask<T>> value,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", timeout, ct);

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
	public static ValueTask IsNotNull<T>(
		Func<ValueTask<T>> value,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("value")] string valueExpression = "")
		=> IsNotNullCore<T>(value, a => $"{valueExpression} to be null but found {a} ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNotNull<T>(Func<ValueTask<T>> value, string message, CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, TestHelper.DefaultTimeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeout">The max duration to wait for.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNotNull<T>(Func<ValueTask<T>> value, string message, TimeSpan timeout, CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, timeout, ct);

	/// <summary>
	/// Asynchronously tests whether the specified object is non-null and throws an exception if it is null.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="value">The object the test expects to be null.</param>
	/// <param name="message">The message to include in the exception when value is not equal to expected. The message is shown in test results.</param>
	/// <param name="timeoutMs">The max duration to wait for in milliseconds.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask IsNotNull<T>(Func<ValueTask<T>> value, string message, int timeoutMs, CancellationToken ct = default)
		=> IsNotNullCore<T>(value, a => message, TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async ValueTask IsNotNullCore<T>(Func<T> value, Func<T, string> reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(() => !object.Equals(null, value()), timeout, ct);

		var a = value();
		Assert.IsNotNull(a, reason(a));
	}

	private static async ValueTask IsNotNullCore<T>(Func<ValueTask<T>> value, Func<T, string> reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => !object.Equals(null, await value().ConfigureAwait(false)), timeout, ct);

		var a = await value().ConfigureAwait(false);
		Assert.IsNotNull(a, reason(a));
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
	public static ValueTask AreEqual<T>(
		T expected,
		Func<T> actual,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

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
	public static ValueTask AreEqual<T>(
		T expected,
		Func<T> actual,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", timeout, ct);

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
	public static ValueTask AreEqual<T>(
		T expected,
		Func<T> actual,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask AreEqual<T>(T expected, Func<T> actual, string message, CancellationToken ct = default)
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
	public static ValueTask AreEqual<T>(T expected, Func<T> actual, string message, TimeSpan timeout, CancellationToken ct = default)
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
	public static ValueTask AreEqual<T>(T expected, Func<T> actual, string message, int timeoutMs, CancellationToken ct = default)
		=> AreEqualCore(expected, actual, a => message, TimeSpan.FromMilliseconds(timeoutMs), ct);

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
	public static ValueTask AreEqual<T>(
		T expected,
		Func<ValueTask<T>> actual,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

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
	public static ValueTask AreEqual<T>(
		T expected,
		Func<ValueTask<T>> actual,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", timeout, ct);

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
	public static ValueTask AreEqual<T>(
		T expected,
		Func<ValueTask<T>> actual,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are equal and throws an exception if the two values are not equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask AreEqual<T>(T expected, Func<ValueTask<T>> actual, string message, CancellationToken ct = default)
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
	public static ValueTask AreEqual<T>(T expected, Func<ValueTask<T>> actual, string message, TimeSpan timeout, CancellationToken ct = default)
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
	public static ValueTask AreEqual<T>(T expected, Func<ValueTask<T>> actual, string message, int timeoutMs, CancellationToken ct = default)
		=> AreEqualCore(expected, actual, a => message, TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async ValueTask AreEqualCore<T>(T expected, Func<T> actual, Func<T, string> reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(() => object.Equals(expected, actual()), timeout, ct);

		var a = actual();
		Assert.AreEqual(expected, a, reason(a));
	}

	private static async ValueTask AreEqualCore<T>(T expected, Func<ValueTask<T>> actual, Func<T, string> reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => object.Equals(expected, await actual().ConfigureAwait(false)), timeout, ct);

		var a = await actual().ConfigureAwait(false);
		Assert.AreEqual(expected, a, reason(a));
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
	public static ValueTask AreNotEqual<T>(
		T expected,
		Func<T> actual,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

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
	public static ValueTask AreNotEqual<T>(
		T expected,
		Func<T> actual,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", timeout, ct);

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
	public static ValueTask AreNotEqual<T>(
		T expected,
		Func<T> actual,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask AreNotEqual<T>(T expected, Func<T> actual, string message, CancellationToken ct = default)
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
	public static ValueTask AreNotEqual<T>(T expected, Func<T> actual, string message, TimeSpan timeout, CancellationToken ct = default)
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
	public static ValueTask AreNotEqual<T>(T expected, Func<T> actual, string message, int timeoutMs, CancellationToken ct = default)
		=> AreNotEqualCore(expected, actual, a => message, TimeSpan.FromMilliseconds(timeoutMs), ct);

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
	public static ValueTask AreNotEqual<T>(
		T expected,
		Func<ValueTask<T>> actual,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", TestHelper.DefaultTimeout, ct);

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
	public static ValueTask AreNotEqual<T>(
		T expected,
		Func<ValueTask<T>> actual,
		TimeSpan timeout,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", timeout, ct);

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
	public static ValueTask AreNotEqual<T>(
		T expected,
		Func<ValueTask<T>> actual,
		int timeoutMs,
		CancellationToken ct = default,
		[CallerLineNumber] int line = -1,
		[CallerFilePath] string file = "",
		[CallerArgumentExpression("actual")] string actualExpression = "")
		=> AreNotEqualCore(expected, actual, a => $"{actualExpression} to equals {expected} but found {a} ({Path.GetFileName(file)}@{line})", TimeSpan.FromMilliseconds(timeoutMs), ct);

	/// <summary>
	/// Asynchronously tests whether the specified values are unequal and throws an exception if the two values are equal. Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42.
	/// </summary>
	/// <typeparam name="T">The type of values to compare.</typeparam>
	/// <param name="expected">The first value to compare. This is the value the tests expects.</param>
	/// <param name="actual">The second value to compare. This is the value produced by the code under test.</param>
	/// <param name="message">The message to include in the exception when actual is not equal to expected. The message is shown in test results.</param>
	/// <param name="ct">Cancellation token to cancel teh asynchronous operation</param>
	public static ValueTask AreNotEqual<T>(T expected, Func<ValueTask<T>> actual, string message, CancellationToken ct = default)
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
	public static ValueTask AreNotEqual<T>(T expected, Func<ValueTask<T>> actual, string message, TimeSpan timeout, CancellationToken ct = default)
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
	public static ValueTask AreNotEqual<T>(T expected, Func<ValueTask<T>> actual, string message, int timeoutMs, CancellationToken ct = default)
		=> AreNotEqualCore(expected, actual, a => message, TimeSpan.FromMilliseconds(timeoutMs), ct);

	private static async ValueTask AreNotEqualCore<T>(T expected, Func<T> actual, Func<T, string> reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(() => !object.Equals(expected, actual()), timeout, ct);

		var a = actual();
		Assert.AreNotEqual(expected, a, reason(a));
	}

	private static async ValueTask AreNotEqualCore<T>(T expected, Func<ValueTask<T>> actual, Func<T, string> reason, TimeSpan timeout, CancellationToken ct)
	{
		await TestHelper.TryWaitFor(async _ => !object.Equals(expected, await actual().ConfigureAwait(false)), timeout, ct);

		var a = await actual().ConfigureAwait(false);
		Assert.AreNotEqual(expected, a, reason(a));
	}
	#endregion
}
#endif
