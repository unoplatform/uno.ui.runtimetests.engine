#if !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Engine;

/// <summary>
/// A runtime-test runner that is embedded in applications that are referencing the runtime-test engine package.
/// </summary>
/// <remarks>
/// This class is intended to be used only by the the test engine itself and should not be used by applications.
/// API contract is not guaranteed and might change in future releases.
/// </remarks>
internal static partial class RuntimeTestEmbeddedRunner
{
	private enum TestResultKind
	{
		NUnit = 0,
		UnoRuntimeTests,
	}

	[ModuleInitializer]
	public static void AutoStartTests()
	{
		if (Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_RUN_TESTS") is { } testsConfig
			&& Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_OUTPUT_PATH") is { } outputPath)
		{
			var outputKind = Enum.TryParse<TestResultKind>(Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_OUTPUT_KIND"), ignoreCase: true, out var kind)
				? kind
				: TestResultKind.NUnit;
			var isSecondaryApp = Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_IS_SECONDARY_APP")?.ToLowerInvariant() switch
			{
				null => false,
				"false" => false,
				"0" => false,
				_ => true
			};

			_ = RunTestsAndExit(testsConfig, outputPath, outputKind, isSecondaryApp);
		}
	}

	private static async Task RunTestsAndExit(string testsConfigRaw, string outputPath, TestResultKind outputKind, bool isSecondaryApp)
	{
		var ct = new CancellationTokenSource();

		try
		{
			Log("Waiting for app to init before running runtime-tests");

			Console.CancelKeyPress += (_, _) => ct.Cancel(true);

			// Wait for the app to init it-self
			await Task.Delay(500, ct.Token).ConfigureAwait(false);
			for (var i = 0; Window.Current is null && i < 100; i++)
			{
				await Task.Delay(50, ct.Token).ConfigureAwait(false);
			}

			var window = Window.Current;
			if (window is null or { Dispatcher: null })
			{
				throw new InvalidOperationException("Window.Current is null or does not have any valid dispatcher");
			}

			// While the app init, parse the tests config
			var config = default(UnitTestEngineConfig?);
			if (testsConfigRaw.StartsWith('{'))
			{
				try
				{
					config = JsonSerializer.Deserialize<UnitTestEngineConfig>(testsConfigRaw);
				}
				catch { }
			}
			config ??= new UnitTestEngineConfig { Filter = testsConfigRaw };

			// Let continue on the dispatcher thread
			var tcs = new TaskCompletionSource();
			await window
				.Dispatcher
				.RunAsync(
					CoreDispatcherPriority.Normal,
					async () =>
					{
						try
						{
							await RunTests(window, config, outputPath, outputKind, isSecondaryApp, ct.Token);
							tcs.TrySetResult();
						}
						catch (OperationCanceledException) when (ct.IsCancellationRequested)
						{
							tcs.TrySetCanceled();
						}
						catch (Exception error)
						{
							tcs.TrySetException(error);
						}
					})
				.AsTask(ct.Token)
				.ConfigureAwait(false);

			await tcs.Task.ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			LogError("Runtime-tests has been cancelled.");
			Environment.Exit(-1);
		}
		catch (Exception error)
		{
			LogError("Failed to run runtime-test.");
			LogError(error.ToString());
			Environment.Exit(1);
		}
		finally
		{
			Environment.Exit(0);
		}
	}

	private static async Task RunTests(Window window, UnitTestEngineConfig config, string outputPath, TestResultKind outputKind, bool isSecondaryApp, CancellationToken ct)
	{
		// Wait for the app to init it-self
		for (var i = 0; window.Content is null or { ActualSize.X: 0 } or { ActualSize.Y: 0 } && i < 20; i++)
		{
			await Task.Delay(20, ct);
		}

		// Then override the app content by the test control
		Log("Initializing runtime-tests engine.");
		var engine = new UnitTestsControl { IsSecondaryApp = isSecondaryApp };
		Window.Current.Content = engine;

		// Runt tes test !
		Log($"Running runtime-tests ({config})");
		await engine.RunTests(ct, config).ConfigureAwait(false);

		// Finally save the test results
		Log($"Saving runtime-tests results to {outputPath}.");
		switch (outputKind)
		{
			case TestResultKind.UnoRuntimeTests: 
				await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(engine.Results), Encoding.UTF8, ct);
				break;

			default:
			case TestResultKind.NUnit:
				await File.WriteAllTextAsync(outputPath, engine.NUnitTestResultsDocument, Encoding.Unicode, ct);
				break;
		}
	}

	private static void Log(string text)
		=> Console.WriteLine(text);

	private static void LogError(string text)
		=> Console.Error.WriteLine(text);
}
#endif