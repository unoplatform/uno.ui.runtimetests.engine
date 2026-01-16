#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER
#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

#pragma warning disable CA2255 // The 'ModuleInitializer' attribute is only intended to be used in application code or advanced source generator scenarios
	[ModuleInitializer]
#pragma warning restore CA2255
	public static void AutoStartTests()
	{
		Trace("Initializing runtime-tests module.");

		if (Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_RUN_TESTS") is { } testsConfig)
		{
			var outputPath = Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_OUTPUT_PATH");
			var outputUrl = Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_OUTPUT_URL");

			// At least one output destination must be configured
			if (string.IsNullOrEmpty(outputPath) && string.IsNullOrEmpty(outputUrl))
			{
				Trace("Application has not been configured with output destination, aborting runtime-test embedded runner.");
				return;
			}

			if (bool.TryParse(testsConfig, out var runTests))
			{
				if (runTests)
				{
					testsConfig = string.Empty; // Do not treat "true" as a filter.
				}
				else
				{
					Trace("Application has been configured to **NOT** start runtime-test, aborting runtime-test embedded runner.");
					return;
				}
			}

			Trace($"Application configured to start runtime-tests (OutputPath={outputPath} | OutputUrl={outputUrl} | Config={testsConfig}).");

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

			_ = RunTestsAndExit(testsConfig, outputPath, outputUrl, outputKind, isSecondaryApp);
		}
		else
		{
			Trace("Application has not been configured to start runtime-test, aborting runtime-test embedded runner.");
		}
	}

	private static async Task RunTestsAndExit(string testsConfigRaw, string? outputPath, string? outputUrl, TestResultKind outputKind, bool isSecondaryApp)
	{
		var ct = new CancellationTokenSource();

		try
		{
			Log("Waiting for app to init before running runtime-tests.");

#pragma warning disable CA1416 // Validate platform compatibility
			Console.CancelKeyPress += (_, _) => ct.Cancel(true);
#pragma warning restore CA1416 // Validate platform compatibility

			// Wait for the app to init it-self
			await Task.Delay(2000, ct.Token).ConfigureAwait(false);
			for (var i = 0; Window.Current is null && i < 100; i++)
			{
				await Task.Delay(50, ct.Token).ConfigureAwait(false);
			}

			var window = Window.Current;
			if (window is null or { Dispatcher: null })
			{
				throw new InvalidOperationException("Window.Current is null or does not have any valid dispatcher");
			}

			Trace("Got window (and dispatcher), continuing runtime-test initialization on it.");

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
							Trace("Got dispatcher access, init the runtime-test engine.");

							await RunTests(window, config, outputPath, outputUrl, outputKind, isSecondaryApp, ct.Token);
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
			ExitApplication(-1);
		}
		catch (Exception error)
		{
			LogError("Failed to run runtime-test.");
			LogError(error.ToString());
			ExitApplication(1);
		}
		finally
		{
			Log("Runtime-test completed, exiting app.");
			ExitApplication(0);
		}
	}

	private static async Task RunTests(Window window, UnitTestEngineConfig config, string? outputPath, string? outputUrl, TestResultKind outputKind, bool isSecondaryApp, CancellationToken ct)
	{
		// Wait for the app to init it-self
		for (var i = 0; window.Content is null or { ActualSize.X: 0 } or { ActualSize.Y: 0 } && i < 20; i++)
		{
			await Task.Delay(20, ct);
		}

		// Then override the app content by the test control
		Trace("Initializing runtime-tests engine.");
		var engine = new UnitTestsControl { IsSecondaryApp = isSecondaryApp };
		Window.Current.Content = engine;
		await UIHelper.WaitForLoaded(engine, ct);

		// Run the test !
		Log($"Running runtime-tests ({config})");
		await engine.RunTests(ct, config).ConfigureAwait(false);

		// Generate results content based on output kind
		string resultsContent;
		string contentType;
		switch (outputKind)
		{
			case TestResultKind.UnoRuntimeTests:
				resultsContent = JsonSerializer.Serialize(engine.Results);
				contentType = "application/json";
				break;

			default:
			case TestResultKind.NUnit:
				resultsContent = engine.NUnitTestResultsDocument;
				contentType = "application/xml";
				break;
		}

		// Save to file if path is configured (non-WASM platforms)
		if (!string.IsNullOrEmpty(outputPath))
		{
			Log($"Saving runtime-tests results to {outputPath}.");
			await File.WriteAllTextAsync(outputPath, resultsContent, Encoding.UTF8, ct);
		}

		// POST to URL if configured (WASM platform or dual output)
		if (!string.IsNullOrEmpty(outputUrl))
		{
			Log($"Posting runtime-tests results to {outputUrl}.");
			var success = await WasmTestResultReporter.PostResultsAsync(outputUrl, resultsContent, contentType, ct);
			if (!success)
			{
				LogError($"Failed to POST results to {outputUrl}");
			}
		}
	}

	private static void ExitApplication(int exitCode)
	{
		// Set the exit code first, then exit gracefully via Application.Current.Exit()
		// to allow Skia/X11 to clean up properly and avoid segfaults on Linux.
		Environment.ExitCode = exitCode;
		Application.Current.Exit();
	}

	[Conditional("DEBUG")]
	private static void Trace(string text)
		=> Console.WriteLine(text);

	private static void Log(string text)
		=> Console.WriteLine(text);

	private static void LogError(string text)
		=> Console.Error.WriteLine(text);
}
#endif