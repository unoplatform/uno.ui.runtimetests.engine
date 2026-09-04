#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

// This bridge references Microsoft.Testing.Platform types directly, so it can only compile where
// those packages are actually referenced (i.e. when $(UseMSTest)=true, see TestApp.csproj).
#if USE_UNO_MSTEST_ENGINE
#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace Uno.UI.RuntimeTests;

/// <summary>
/// An <see cref="IDataConsumer"/> that subscribes to the <see cref="TestNodeUpdateMessage"/>s
/// published by MSTest's real engine as a run progresses, and translates them into the Uno
/// runtime-tests engine's own <see cref="TestCaseResult"/> shape so that <see cref="UnitTestsControl"/>
/// can show live "which test is executing / which have executed" feedback, and so that the
/// existing NUnit-XML/env-var reporting (<c>RuntimeTestEmbeddedRunner</c>) keeps working unchanged.
/// </summary>
internal sealed class UnitTestsMSTestReporter : IDataConsumer
{
	public static event Action<TestCaseResult>? OnTestCaseResult;
	public static event Action<string>? OnTestCaseInProgress;

	public UnitTestsMSTestReporter()
	{
	}

	public string Uid => nameof(UnitTestsMSTestReporter);

	public string Version => "1.0.0";

	public string DisplayName => "Uno Runtime Tests Reporter";

	public string Description => "Bridges MSTest/Microsoft.Testing.Platform test results into the Uno runtime-tests UI and reporting.";

	public Type[] DataTypesConsumed { get; } = [typeof(TestNodeUpdateMessage)];

	public Task<bool> IsEnabledAsync() => Task.FromResult(true);

	public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
	{
		var onResult = OnTestCaseResult;
		var onInProgress = OnTestCaseInProgress;

		if (value is TestNodeUpdateMessage { TestNode: { } node })
		{
			var state = node.Properties.OfType<TestNodeStateProperty>().FirstOrDefault();
			var duration = node.Properties.SingleOrDefault<TimingProperty>()?.GlobalTiming.Duration ?? TimeSpan.Zero;

			switch (state)
			{
				case InProgressTestNodeStateProperty:
					onInProgress?.Invoke(node.DisplayName);
					break;

				case PassedTestNodeStateProperty:
					var h = OnTestCaseResult;
					onResult?.Invoke(new TestCaseResult { TestName = node.DisplayName, TestResult = TestResult.Passed, Duration = duration });
					break;

				case SkippedTestNodeStateProperty skipped:
					onResult?.Invoke(new TestCaseResult { TestName = node.DisplayName, TestResult = TestResult.Skipped, Duration = duration, Message = skipped.Explanation });
					break;

				case FailedTestNodeStateProperty failed:
					onResult?.Invoke(new TestCaseResult { TestName = node.DisplayName, TestResult = TestResult.Failed, Duration = duration, Message = failed.Explanation ?? failed.Exception?.Message, Error = failed.Exception });
					break;

				case ErrorTestNodeStateProperty error:
					onResult?.Invoke(new TestCaseResult { TestName = node.DisplayName, TestResult = TestResult.Error, Duration = duration, Message = error.Explanation ?? error.Exception?.Message, Error = error.Exception });
					break;

					// Discovered/other states aren't terminal results -- ignored here.
			}
		}

		return Task.CompletedTask;
	}
}
#endif
