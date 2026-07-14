#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

// Unlike the rest of this file's siblings, this bridge references Microsoft.Testing.Platform
// types directly, so it can only compile where those packages are actually referenced
// (desktop head only for now -- see specs/003-mtp-integration/spec.md NFR-003).
#if !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER && HAS_UNO_RUNTIMETESTS_MTP
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using Microsoft.UI.Xaml;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Engine;

public static class RuntimeTestsMtpRunner
{
	public static ITestApplicationBuilder AddUnoRuntimeTests(this ITestApplicationBuilder builder)
	{
		builder.RegisterTestFramework(
			_ => new UnoRuntimeTestsFrameworkCapabilities(),
			(capabilities, serviceProvider) => new UnoRuntimeTestsFramework(capabilities, serviceProvider));
		return builder;
	}
}

/// <summary>
/// Opt-in bridge that hosts the runtime-tests engine inside a Microsoft.Testing.Platform (MTP)
/// <c>TestApplication</c>, so it can be discovered/run via <c>dotnet test</c> or an MTP-aware IDE
/// Test Explorer, in addition to the existing <see cref="RuntimeTestEmbeddedRunner"/> env-var flow.
/// </summary>
/// <remarks>
/// This class is intended to be used only by the test engine itself and should not be used by applications.
/// API contract is not guaranteed and might change in future releases. See specs/003-mtp-integration/spec.md.
/// </remarks>
public static class MicrosoftTestingPlatformRunner
{
	/// <summary>
	/// Builds and runs an MTP <c>TestApplication</c> hosting the runtime-tests engine, then hard-exits
	/// the process with the resulting exit code (mirrors <see cref="RuntimeTestEmbeddedRunner.ExitApplication"/>).
	/// </summary>
	/// <remarks>
	/// Must be started concurrently with -- not instead of -- the app's normal native host loop
	/// (e.g. Uno's <c>host.Run()</c>), since that loop blocks the calling thread. Call this from a
	/// fire-and-forget background task before calling <c>host.Run()</c>, mirroring how
	/// <see cref="RuntimeTestEmbeddedRunner.AutoStartTests"/> already runs concurrently with it today.
	/// </remarks>
	public static async Task RunAsync(string[] args)
	{
		var exitCode = 1;
		try
		{
			var builder = await TestApplication.CreateBuilderAsync(args);
			builder.RegisterTestFramework(
				_ => new UnoRuntimeTestsFrameworkCapabilities(),
				(capabilities, serviceProvider) => new UnoRuntimeTestsFramework(capabilities, serviceProvider));

			using var app = await builder.BuildAsync();
			exitCode = await app.RunAsync();
		}
		catch (Exception error)
		{
			RuntimeTestEmbeddedRunner.LogError("Failed to run MTP-hosted runtime tests.");
			RuntimeTestEmbeddedRunner.LogError(error.ToString());
			exitCode = 1;
		}
		finally
		{
			Application.Current.Exit();
			// RuntimeTestEmbeddedRunner.ExitApplication(exitCode);
		}
	}
}

internal sealed class UnoRuntimeTestsFrameworkCapabilities : ITestFrameworkCapabilities
{
	public IReadOnlyCollection<ITestFrameworkCapability> Capabilities => Array.Empty<ITestFrameworkCapability>();
}

internal sealed class UnoRuntimeTestsFramework : ITestFramework, IDataProducer
{
	// capabilities/serviceProvider are intentionally unused for now -- this bridge doesn't
	// yet need platform services (logging, command-line options, etc). See Open Questions/Risks.
	public UnoRuntimeTestsFramework(ITestFrameworkCapabilities capabilities, IServiceProvider serviceProvider)
	{
	}

	public string Uid => nameof(UnoRuntimeTestsFramework);

	public string Version => "1.0.0";

	public string DisplayName => "Uno.UI.RuntimeTests.Engine";

	public string Description => "Uno Platform in-app runtime-tests engine, bridged to Microsoft.Testing.Platform.";

	public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

	public Task<bool> IsEnabledAsync() => Task.FromResult(true);

	public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
		=> Task.FromResult(new CreateTestSessionResult { IsSuccess = true });

	public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
		=> Task.FromResult(new CloseTestSessionResult { IsSuccess = true });

	public async Task ExecuteRequestAsync(ExecuteRequestContext context)
	{
		try
		{
			var window = await WaitForWindowAsync(context.CancellationToken);

			switch (context.Request)
			{
				case DiscoverTestExecutionRequest discover:
					await DiscoverAsync(context, discover, window);
					break;

				case RunTestExecutionRequest run:
					await RunTestsAsync(context, run, window);
					break;
			}
		}
		finally
		{
			context.Complete();
		}
	}

	/// <summary>
	/// Waits for the app's window/dispatcher to become available, mirroring
	/// <see cref="RuntimeTestEmbeddedRunner"/>'s own startup-readiness sequence.
	/// </summary>
	private static async Task<Window> WaitForWindowAsync(CancellationToken ct)
	{
		await Task.Delay(2000, ct);
		await RuntimeTestEmbeddedRunner.WaitForCheckSafe(() => Application.Current is not null, ct: ct, timeout: TimeSpan.FromSeconds(5));
		await RuntimeTestEmbeddedRunner.WaitForIdle(ct);
		await RuntimeTestEmbeddedRunner.WaitForCheckSafe(() => Window.Current?.Dispatcher is not null, ct: ct, timeout: TimeSpan.FromSeconds(30));

		return Window.Current is { Dispatcher: { } } window
			? window
			: throw new InvalidOperationException("Window.Current is null or does not have a valid dispatcher after waiting.");
	}

	private async Task DiscoverAsync(ExecuteRequestContext context, DiscoverTestExecutionRequest request, Window window)
	{
		var tcs = new TaskCompletionSource();

		await window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
		{
			try
			{
				var engine = new UnitTestsControl();

				foreach (var (fqn, displayName) in EnumerateTestFqns(engine))
				{
					var node = new TestNode
					{
						Uid = new TestNodeUid(fqn),
						DisplayName = displayName,
						Properties = new PropertyBag(DiscoveredTestNodeStateProperty.CachedInstance),
					};

					await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(request.Session.SessionUid, node));
				}

				tcs.TrySetResult();
			}
			catch (Exception error)
			{
				tcs.TrySetException(error);
			}
		}).AsTask(context.CancellationToken).ConfigureAwait(false);

		await tcs.Task.ConfigureAwait(false);
	}

	private async Task RunTestsAsync(ExecuteRequestContext context, RunTestExecutionRequest request, Window window)
	{
		var config = RuntimeTestEmbeddedRunner.ApplyShardingFromEnvironment(BuildConfig(request));
		var tcs = new TaskCompletionSource();

		await window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
		{
			try
			{
				var engine = new UnitTestsControl();
				Window.Current!.Content = engine;

				await engine.RunTests(context.CancellationToken, config);

				foreach (var result in engine.Results)
				{
					var name = result.TestName ?? "(unknown)";
					var node = new TestNode
					{
						Uid = new TestNodeUid(name),
						DisplayName = name,
						Properties = new PropertyBag(ToStateProperty(result)),
					};

					await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(request.Session.SessionUid, node));
				}

				tcs.TrySetResult();
			}
			catch (Exception error)
			{
				tcs.TrySetException(error);
			}
		}).AsTask(context.CancellationToken).ConfigureAwait(false);

		await tcs.Task.ConfigureAwait(false);
	}

	/// <summary>
	/// Translates an MTP UID-list filter (e.g. "run selected tests") into the engine's existing
	/// <see cref="UnitTestFilter"/> OR-expression syntax, reusing its parser as-is (see FR-003).
	/// </summary>
	/// <remarks>
	/// Uids here are method-level FQNs (<c>Namespace.Class.Method</c>, see <see cref="EnumerateTestFqns"/>),
	/// which is exactly what <see cref="UnitTestFilter.IsMatch(System.Reflection.MethodInfo)"/> compares
	/// against, and contains no characters that collide with the filter grammar (unlike raw test display
	/// names, which can contain "(", ")", "&amp;", "|" from data-driven case parameters).
	/// </remarks>
	private static UnitTestEngineConfig BuildConfig(RunTestExecutionRequest request)
	{
		if (request.Filter is TestNodeUidListFilter { TestNodeUids: { Length: > 0 } uids })
		{
			var filter = string.Join(" | ", uids.Select(uid => uid.Value));
			return UnitTestEngineConfig.Default with { Filter = filter };
		}

		return UnitTestEngineConfig.Default;
	}

	/// <summary>
	/// Enumerates one (Fqn, DisplayName) pair per test *method* (matching the filter/sharding engine's
	/// existing method-level granularity -- see 002-sharding), not per data-row case.
	/// </summary>
	/// <remarks>
	/// Known limitation: <see cref="TestCaseResult"/> (used for Run reporting) only exposes a bare
	/// display name with no class context, so data-driven ([DataRow]) tests will report each case as
	/// its own TestNode during Run using that display name, which will not correlate back to the single
	/// method-level node reported here during Discovery. Selecting an individual data-row case via
	/// "run selected tests" is not supported -- selecting the method runs all of its rows, consistent
	/// with the existing sharding engine's documented granularity.
	/// </remarks>
	private static IEnumerable<(string Fqn, string DisplayName)> EnumerateTestFqns(UnitTestsControl engine)
	{
		foreach (var classInfo in engine.InitializeTests())
		{
			if (classInfo.Type is not { FullName: { } className })
			{
				continue;
			}

			foreach (var test in classInfo.Tests)
			{
				yield return ($"{className}.{test.Name}", test.Name);
			}
		}
	}

	private static IProperty ToStateProperty(TestCaseResult result) => result.TestResult switch
	{
		TestResult.Passed => PassedTestNodeStateProperty.CachedInstance,
		TestResult.Skipped => SkippedTestNodeStateProperty.CachedInstance,
		TestResult.Error => new ErrorTestNodeStateProperty(result.Message ?? "Unknown error"),
		_ => new FailedTestNodeStateProperty(result.Message ?? "Test failed"),
	};
}
#endif
