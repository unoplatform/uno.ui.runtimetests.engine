using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Engine.Wasm.Runner;

namespace Uno.UI.RuntimeTests.Engine.Wasm.Runner.Tests;

/// <summary>
/// Validates the <c>--timeout</c> handling of the WASM runner (https://github.com/unoplatform/uno.ui.runtimetests.engine/issues/221).
/// </summary>
[TestClass]
public class TimeoutTests
{
	[TestMethod]
	public void CreateCancellationTokenSource_Zero_IsNotCanceled()
	{
		using var cts = RunnerTimeout.CreateCancellationTokenSource(0, CancellationToken.None);

		Assert.IsFalse(cts.IsCancellationRequested, "A timeout of 0 means no timeout, not an already elapsed one");
	}

	[TestMethod]
	public async Task CreateCancellationTokenSource_Positive_CancelsAfterDelay()
	{
		using var cts = RunnerTimeout.CreateCancellationTokenSource(1, CancellationToken.None);

		Assert.IsFalse(cts.IsCancellationRequested, "The timeout must not be elapsed right after creation");

		var canceled = new TaskCompletionSource();
		using var registration = cts.Token.Register(() => canceled.TrySetResult());
		await canceled.Task.WaitAsync(TimeSpan.FromSeconds(30)); // Throws TimeoutException if the token never cancels
	}

	[TestMethod]
	public void CreateCancellationTokenSource_MaxSeconds_DoesNotThrow()
	{
		using var cts = RunnerTimeout.CreateCancellationTokenSource(RunnerTimeout.MaxSeconds, CancellationToken.None);

		Assert.IsFalse(cts.IsCancellationRequested);
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(300)]
	public void CreateCancellationTokenSource_ExternalCancellation_Cancels(int seconds)
	{
		using var external = new CancellationTokenSource();
		using var cts = RunnerTimeout.CreateCancellationTokenSource(seconds, external.Token);

		external.Cancel();

		Assert.IsTrue(cts.IsCancellationRequested, "Canceling the invocation (e.g. Ctrl+C) must cancel the run, even without timeout");
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(300)]
	[DataRow(RunnerTimeout.MaxSeconds)]
	public void Validate_InRange_ReturnsNull(int seconds)
	{
		Assert.IsNull(RunnerTimeout.Validate(seconds));
	}

	[TestMethod]
	[DataRow(-1)]
	[DataRow(RunnerTimeout.MaxSeconds + 1)]
	public void Validate_OutOfRange_ReturnsError(int seconds)
	{
		StringAssert.Contains(RunnerTimeout.Validate(seconds), "--timeout");
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("run")]
	public void Parse_ValidTimeout_HasNoErrors(string command)
	{
		foreach (var timeout in new[] { "0", "300", RunnerTimeout.MaxSeconds.ToString() })
		{
			var result = Parse(command, timeout);

			Assert.AreEqual(0, result.Errors.Count, $"--timeout {timeout}: {string.Join(", ", result.Errors.Select(e => e.Message))}");
		}
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("run")]
	public void Parse_OutOfRangeTimeout_ReportsError(string command)
	{
		foreach (var timeout in new[] { "-1", (RunnerTimeout.MaxSeconds + 1L).ToString() })
		{
			var result = Parse(command, timeout);

			Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("--timeout must be between")), $"--timeout {timeout} must be rejected");
		}
	}

	[TestMethod]
	public void Parse_NonIntegerTimeout_ReportsParserError()
	{
		var result = Parse("", "abc");

		Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("abc")), "The parser must report the invalid value instead of throwing");
	}

	[TestMethod]
	public async Task WaitForResults_ResultsReceived_ReturnsResults()
	{
		var results = await Program.WaitForResultsOrBrowserExitAsync(Task.FromResult<string?>("results"), new TaskCompletionSource().Task, TimeSpan.FromMinutes(10), CancellationToken.None);

		Assert.AreEqual("results", results);
	}

	[TestMethod]
	public async Task WaitForResults_BrowserExited_ResultsDuringGracePeriod_ReturnsResults()
	{
		var resultsTcs = new TaskCompletionSource<string?>();
		_ = Task.Delay(50).ContinueWith(_ => resultsTcs.TrySetResult("results"), TaskScheduler.Default);

		var results = await Program.WaitForResultsOrBrowserExitAsync(resultsTcs.Task, Task.CompletedTask, TimeSpan.FromMinutes(10), CancellationToken.None)
			.WaitAsync(TimeSpan.FromSeconds(30));

		Assert.AreEqual("results", results, "A results POST in flight when the browser exits must not be lost");
	}

	[TestMethod]
	public async Task WaitForResults_BrowserExited_NoResults_ReturnsNullAfterGracePeriod()
	{
		var results = await Program.WaitForResultsOrBrowserExitAsync(new TaskCompletionSource<string?>().Task, Task.CompletedTask, TimeSpan.FromMilliseconds(50), CancellationToken.None)
			.WaitAsync(TimeSpan.FromSeconds(30)); // Without timeout (--timeout 0), this would never complete if browser exit was ignored

		Assert.IsNull(results);
	}

	[TestMethod]
	public async Task WaitForResults_Canceled_ReturnsNullWithoutGracePeriod()
	{
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		// As in the runner, both tasks complete with the canceled token: the results as null, the browser exit monitor as canceled.
		var results = await Program.WaitForResultsOrBrowserExitAsync(Task.FromResult<string?>(null), Task.FromCanceled(cts.Token), TimeSpan.FromMinutes(10), cts.Token)
			.WaitAsync(TimeSpan.FromSeconds(30));

		Assert.IsNull(results);
	}

	private static ParseResult Parse(string command, string timeout)
	{
		var args = new List<string>();
		if (command.Length > 0)
		{
			args.Add(command);
		}
		args.AddRange(["--app-path", "app", "--output", "results.xml", "--timeout", timeout]);

		return Program.CreateRootCommand().Parse(args.ToArray());
	}
}
