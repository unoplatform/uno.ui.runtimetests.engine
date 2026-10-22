using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.HotTesting;

public static class UnoAssertAsyncExtensions
{
	extension(Assert)
	{
		public static global::System.Threading.Tasks.ValueTask IsTrueAsync(
			global::System.Func<bool> condition,
			global::System.Threading.CancellationToken ct = default,
			[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
			[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
			[global::System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string conditionExpression = "")
			=> AsyncAssert.IsTrue(condition, ct, line, file, conditionExpression);        

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(
			T expected,
			global::System.Func<T> actual,
			global::System.Threading.CancellationToken ct = default,
			[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
			[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
			[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
			=> AsyncAssert.AreEqual(expected, actual, ct, line, file, actualExpression);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(
			T expected,
			global::System.Func<T> actual,
			global::System.TimeSpan timeout,
			global::System.Threading.CancellationToken ct = default,
			[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
			[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
			[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
			=> AsyncAssert.AreEqual(expected, actual, timeout, ct, line, file, actualExpression);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(
			T expected,
			global::System.Func<T> actual,
			int timeoutMs,
			global::System.Threading.CancellationToken ct = default,
			[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
			[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
			[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
			=> AsyncAssert.AreEqual(expected, actual, timeoutMs, ct, line, file, actualExpression);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(T expected, global::System.Func<T> actual, string message, global::System.Threading.CancellationToken ct = default)
			=> AsyncAssert.AreEqual(expected, actual, message, ct);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(T expected, global::System.Func<T> actual, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
			=> AsyncAssert.AreEqual(expected, actual, message, timeout, ct);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(T expected, global::System.Func<T> actual, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
			=> AsyncAssert.AreEqual(expected, actual, message, timeoutMs, ct);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(
			T expected,
			global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual,
			global::System.Threading.CancellationToken ct = default,
			[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
			[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
			[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
			=> AsyncAssert.AreEqual(expected, actual, ct, line, file, actualExpression);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(
			T expected,
			global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual,
			global::System.TimeSpan timeout,
			global::System.Threading.CancellationToken ct = default,
			[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
			[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
			[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
			=> AsyncAssert.AreEqual(expected, actual, timeout, ct, line, file, actualExpression);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(
			T expected,
			global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual,
			int timeoutMs,
			global::System.Threading.CancellationToken ct = default,
			[global::System.Runtime.CompilerServices.CallerLineNumber] int line = -1,
			[global::System.Runtime.CompilerServices.CallerFilePath] string file = "",
			[global::System.Runtime.CompilerServices.CallerArgumentExpression("actual")] string actualExpression = "")
			=> AsyncAssert.AreEqual(expected, actual, timeoutMs, ct, line, file, actualExpression);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, string message, global::System.Threading.CancellationToken ct = default)
			=> AsyncAssert.AreEqual(expected, actual, message, ct);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, string message, global::System.TimeSpan timeout, global::System.Threading.CancellationToken ct = default)
			=> AsyncAssert.AreEqual(expected, actual, message, timeout, ct);

		public static global::System.Threading.Tasks.ValueTask AreEqualAsync<T>(T expected, global::System.Func<global::System.Threading.Tasks.ValueTask<T>> actual, string message, int timeoutMs, global::System.Threading.CancellationToken ct = default)
			=> AsyncAssert.AreEqual(expected, actual, message, timeoutMs, ct);
	}
}
