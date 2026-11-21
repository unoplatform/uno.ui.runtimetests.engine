#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER && !UNO_RUNTIMETESTS_DISABLE_UI
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests.Engine;

/// <summary>
/// Streams the test results to an external runner while the tests are running, so that the runner can still report
/// the completed tests when the run does not complete (e.g. the runner times out, or the browser crashes).
/// </summary>
/// <remarks>
/// At most one request is in flight: the results reported meanwhile are sent together by the next one.
/// Each request is a JSON object: <c>{ "assembly": "...", "running": "..." | null, "results": [ { "name", "result", "duration", "message" } ] }</c>,
/// where <c>running</c> is the test case that is currently running, and <c>results</c> are the results reported since the previous request.
/// </remarks>
internal sealed class TestRunProgressReporter
{
	private const int MaxConsecutiveFailures = 3;
	private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

	private readonly object _gate = new();
	private readonly string _url;
	private readonly string? _assemblyName;
	private readonly Func<string, byte[], Task<bool>> _post;
	private readonly List<TestCaseResult> _pending = new();
	private string? _runningTest;
	private bool _hasChanges;
	private bool _isSending;
	private bool _isDisabled;
	private int _consecutiveFailures;

	public TestRunProgressReporter(string url, string? assemblyName)
		: this(url, assemblyName, (u, content) => WasmTestResultReporter.TryPostProgressAsync(u, content))
	{
	}

	internal TestRunProgressReporter(string url, string? assemblyName, Func<string, byte[], Task<bool>> post)
	{
		_url = url;
		_assemblyName = assemblyName;
		_post = post;
	}

	/// <summary>
	/// Completes when no request is in flight and nothing remains to be sent.
	/// </summary>
	internal Task Idle { get; private set; } = Task.CompletedTask;

	/// <summary>
	/// Gets the number of results waiting to be sent.
	/// </summary>
	internal int PendingCount
	{
		get
		{
			lock (_gate)
			{
				return _pending.Count;
			}
		}
	}

	public void OnTestCaseStarted(string testName)
	{
		lock (_gate)
		{
			if (_isDisabled)
			{
				return;
			}

			_runningTest = testName;
			_hasChanges = true;
		}

		EnsureSending();
	}

	public void OnTestCaseCompleted(TestCaseResult result)
	{
		lock (_gate)
		{
			if (_isDisabled)
			{
				return; // Nothing would ever send it
			}

			_pending.Add(result);
			if (_runningTest == result.TestName)
			{
				_runningTest = null;
			}
			_hasChanges = true;
		}

		EnsureSending();
	}

	private void EnsureSending()
	{
		lock (_gate)
		{
			if (_isSending || _isDisabled)
			{
				return;
			}

			_isSending = true;
			Idle = SendAsync();
		}
	}

	private async Task SendAsync()
	{
		// Let the caller (i.e. the test engine) continue: the results it reports meanwhile are sent with this request.
		await Task.Yield();

		while (true)
		{
			TestCaseResult[] batch;
			string? runningTest;
			lock (_gate)
			{
				if (!_hasChanges || _isDisabled)
				{
					_isSending = false;
					return;
				}

				batch = _pending.ToArray();
				_pending.Clear();
				runningTest = _runningTest;
				_hasChanges = false;
			}

			bool success;
			try
			{
				success = await _post(_url, Serialize(_assemblyName, runningTest, batch)).ConfigureAwait(false);
			}
			catch (Exception error)
			{
				LogError($"Failed to report the test progress: {error.Message}");
				success = false;
			}

			if (success)
			{
				_consecutiveFailures = 0;
				continue;
			}

			lock (_gate)
			{
				if (++_consecutiveFailures >= MaxConsecutiveFailures)
				{
					LogError($"Failed to report the test progress {MaxConsecutiveFailures} times in a row, no longer reporting it to {_url}.");
					_isDisabled = true;
					_pending.Clear();
					_isSending = false;
					return;
				}

				// Send these results again with the next request
				_pending.InsertRange(0, batch);
				_hasChanges = true;
			}

			await Task.Delay(RetryDelay).ConfigureAwait(false);
		}
	}

	internal static byte[] Serialize(string? assemblyName, string? runningTest, IReadOnlyList<TestCaseResult> results)
	{
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream))
		{
			writer.WriteStartObject();
			writer.WriteString("assembly", assemblyName);
			writer.WriteString("running", runningTest);
			writer.WriteStartArray("results");
			foreach (var result in results)
			{
				writer.WriteStartObject();
				writer.WriteString("name", result.TestName);
				writer.WriteString("result", result.TestResult.ToString());
				writer.WriteNumber("duration", result.Duration.TotalSeconds);
				writer.WriteString("message", result.Message);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
		}

		return stream.ToArray();
	}

	private static void LogError(string message)
		=> Console.Error.WriteLine($"[TestRunProgressReporter] {message}");
}
#endif
