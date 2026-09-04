#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Internal.Helpers;
using Windows.Devices.Input;

// `TestResult` is ambiguous in this namespace: Uno.UI.RuntimeTests.TestResult (our pass/fail/error/
// skipped enum) vs Microsoft.VisualStudio.TestTools.UnitTesting.TestResult (MSTest's result class).
// Same-namespace types shadow `using`-imported ones for an unqualified name, so bare `TestResult`
// below always means our enum; the alias is used everywhere MSTest's class is meant.
using MSTestResult = Microsoft.VisualStudio.TestTools.UnitTesting.TestResult;

namespace Uno.UI.RuntimeTests;

/// <summary>
/// Executes a test method through MSTest's real engine (<see cref="TestMethodAttribute.ExecuteAsync"/>),
/// while preserving the Uno runtime-tests engine's behavior attributes: <see cref="RunsOnUIThreadAttribute"/>,
/// <see cref="InjectedPointerAttribute"/>, <see cref="RequiresFullWindowAttribute"/> and
/// <see cref="RunsInSecondaryAppAttribute"/>.
/// </summary>
#pragma warning disable CA1813 // Avoid unsealed attributes: intentionally extensible, matching TestMethodAttribute's own design.
public class UnoTestMethodAttribute : TestMethodAttribute
{
	private static readonly ConcurrentDictionary<string, Task<TestCaseResult[]>> _secondaryAppRuns = new();

	public UnoTestMethodAttribute([CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
		: base(callerFilePath, callerLineNumber)
	{
	}

	/// <inheritdoc />
	public override async Task<MSTestResult[]> ExecuteAsync(ITestMethod testMethod)
	{
		var method = testMethod.MethodInfo;
		var declaringType = method.DeclaringType;

		if (declaringType?.GetCustomAttribute<RunsInSecondaryAppAttribute>() is { } secondaryApp)
		{
			return await ExecuteInSecondaryAppAsync(testMethod, declaringType, secondaryApp).ConfigureAwait(false);
		}

		var runsOnUIThread =
			method.GetCustomAttribute<RunsOnUIThreadAttribute>() is not null ||
			declaringType?.GetCustomAttribute<RunsOnUIThreadAttribute>() is not null;
		var requiresFullWindow =
			method.GetCustomAttribute<RequiresFullWindowAttribute>() is not null ||
			declaringType?.GetCustomAttribute<RequiresFullWindowAttribute>() is not null;
		var pointerTypes = method
			.GetCustomAttributes<InjectedPointerAttribute>()
			.Select(a => a.Type)
			.Distinct()
			.ToArray();

		if (pointerTypes.Length == 0)
		{
			return [await RunOnceAsync(testMethod, runsOnUIThread, requiresFullWindow, pointer: null).ConfigureAwait(false)];
		}

		var results = new MSTestResult[pointerTypes.Length];
		for (var i = 0; i < pointerTypes.Length; i++)
		{
			var result = await RunOnceAsync(testMethod, runsOnUIThread, requiresFullWindow, pointerTypes[i]).ConfigureAwait(false);
			result.DisplayName = $"{testMethod.TestMethodName} [{pointerTypes[i]}]";
			results[i] = result;
		}

		return results;
	}

	private static async Task<MSTestResult> RunOnceAsync(ITestMethod testMethod, bool runsOnUIThread, bool requiresFullWindow, PointerDeviceType? pointer)
	{
		if (!runsOnUIThread)
		{
			return await InvokeCoreAsync(testMethod, pointer, requiresFullWindow).ConfigureAwait(false);
		}

		if (Window.Current is not { } window)
		{
			throw new InvalidOperationException("A test is marked with [RunsOnUIThread], but Window.Current is null.");
		}

		var dispatcher = UnitTestDispatcherCompat.From(window);
		var tcs = new TaskCompletionSource<MSTestResult>();

		await dispatcher.RunAsync(async () =>
		{
			try
			{
				var result = await InvokeCoreAsync(testMethod, pointer, requiresFullWindow).ConfigureAwait(true);
				tcs.TrySetResult(result);
			}
			catch (Exception error)
			{
				tcs.TrySetException(error);
			}
		}).ConfigureAwait(false);

		return await tcs.Task.ConfigureAwait(false);
	}

	private static async Task<MSTestResult> InvokeCoreAsync(ITestMethod testMethod, PointerDeviceType? pointer, bool requiresFullWindow)
	{
		IDisposable? pointerSubscription = null;
		try
		{
			if (InputInjectorHelper.TryGetCurrent() is not null)
			{
				InputInjectorHelper.Current.CleanupPointers();
			}

			if (pointer is { } pt)
			{
				pointerSubscription = InputInjectorHelper.Current.SetPointerType(pt);
			}

			if (requiresFullWindow)
			{
				UnitTestsUIContentHelper.UseActualWindowRoot = true;
				UnitTestsUIContentHelper.SaveOriginalContent();
			}

			return await testMethod.InvokeAsync(null).ConfigureAwait(false);
		}
		finally
		{
			if (requiresFullWindow)
			{
				UnitTestsUIContentHelper.RestoreOriginalContent();
				UnitTestsUIContentHelper.UseActualWindowRoot = false;
			}

			pointerSubscription?.Dispose();
		}
	}

	/// <summary>
	/// Runs the whole test class in a secondary app instance (relaunching the current executable),
	/// caching the run per class per process so that every test method of the class shares a single
	/// secondary-app invocation. Correlates results back to this method by display name only -- the
	/// secondary app's engine reports a bare display name with no class context, the same known
	/// limitation already documented for data-driven cases in the MTP bridge.
	/// </summary>
	private static async Task<MSTestResult[]> ExecuteInSecondaryAppAsync(ITestMethod testMethod, Type declaringType, RunsInSecondaryAppAttribute secondaryApp)
	{
		var className = declaringType.FullName ?? declaringType.Name;

		if (!SecondaryApp.IsSupported)
		{
			if (secondaryApp.IgnoreIfNotSupported)
			{
				return [new MSTestResult
				{
					DisplayName = testMethod.TestMethodName,
					Outcome = UnitTestOutcome.Ignored,
				}];
			}

			return [new MSTestResult
			{
				DisplayName = testMethod.TestMethodName,
				Outcome = UnitTestOutcome.Error,
				TestFailureException = new NotSupportedException($"Test class '{className}' is marked with [RunsInSecondaryApp], but secondary app is not supported on this platform."),
			}];
		}

		var results = await _secondaryAppRuns
			.GetOrAdd(className, static className => RunSecondaryAppAsync(className))
			.ConfigureAwait(false);

		var matches = results
			.Where(r => (r.TestName ?? "").StartsWith(testMethod.TestMethodName, StringComparison.Ordinal))
			.ToArray();

		if (matches.Length == 0)
		{
			return [new MSTestResult
			{
				DisplayName = testMethod.TestMethodName,
				Outcome = UnitTestOutcome.Error,
				TestFailureException = new InvalidOperationException($"No result was reported by the secondary app for test '{testMethod.TestMethodName}' in class '{className}'."),
			}];
		}

		return matches.Select(ToTestResult).ToArray();
	}

	private static Task<TestCaseResult[]> RunSecondaryAppAsync(string className)
	{
		var config = UnitTestEngineConfig.Default with { Filter = className };
		return SecondaryApp.RunTest(config, CancellationToken.None, isAppVisible: System.Diagnostics.Debugger.IsAttached);
	}

	private static MSTestResult ToTestResult(TestCaseResult result)
	{
		var outcome = result.TestResult switch
		{
			TestResult.Passed => UnitTestOutcome.Passed,
			TestResult.Skipped => UnitTestOutcome.Ignored,
			TestResult.Error => UnitTestOutcome.Error,
			_ => UnitTestOutcome.Failed,
		};

		return new MSTestResult
		{
			DisplayName = result.TestName,
			Outcome = outcome,
			Duration = result.Duration,
			TestFailureException = outcome is UnitTestOutcome.Failed or UnitTestOutcome.Error
				? new Exception(result.Message ?? "Test failed in secondary app.")
				: null,
		};
	}
}
