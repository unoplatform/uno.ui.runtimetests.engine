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

		if (GetConfigValue("UNO_RUNTIME_TESTS_RUN_TESTS") is { } testsConfig)
		{
			var outputPath = GetConfigValue("UNO_RUNTIME_TESTS_OUTPUT_PATH");
			var outputUrl = GetConfigValue("UNO_RUNTIME_TESTS_OUTPUT_URL");

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

			var outputKind = Enum.TryParse<TestResultKind>(GetConfigValue("UNO_RUNTIME_TESTS_OUTPUT_KIND"), ignoreCase: true, out var kind)
				? kind
				: TestResultKind.NUnit;
			var isSecondaryApp = GetConfigValue("UNO_RUNTIME_TESTS_IS_SECONDARY_APP")?.ToLowerInvariant() switch
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

#if !__WASM__
			// Console.CancelKeyPress is not supported in WebAssembly
#pragma warning disable CA1416 // Validate platform compatibility
			Console.CancelKeyPress += (_, _) => ct.Cancel(true);
#pragma warning restore CA1416 // Validate platform compatibility
#endif

			// Wait for the app to init it-self
			// On Skia/WebGPU, initialization can take longer due to GPU surface setup
			await Task.Delay(2000, ct.Token).ConfigureAwait(false);

			// Wait for Window.Current to be available with a dispatcher
			Window? window = null;
			for (var i = 0; i < 300; i++)
			{
				window = Window.Current;
				if (window is { Dispatcher: not null })
				{
					break;
				}
				await Task.Delay(100, ct.Token).ConfigureAwait(false);
				if (i > 0 && i % 50 == 0)
				{
					Log($"Still waiting for Window.Current... ({i * 100 / 1000}s)");
				}
			}

			if (window is null or { Dispatcher: null })
			{
				throw new InvalidOperationException("Window.Current is null or does not have any valid dispatcher");
			}

			// Try to wait for Window.Current to have content (app's OnLaunched to complete)
			// Re-check Window.Current each iteration as the app may create a new window
			// Note: On Skia WASM with WebGPU, the app's content might not be set immediately
			// In that case, we'll proceed anyway and set our own content
			for (var i = 0; i < 50; i++) // Wait up to 5 seconds for app content
			{
				window = Window.Current;
				if (window?.Content is not null)
				{
					Log($"Window.Current has content after {i * 100}ms");
					break;
				}
				await Task.Delay(100, ct.Token).ConfigureAwait(false);
				if (i > 0 && i % 10 == 0)
				{
					Log($"Still waiting for Window.Current.Content... ({i * 100 / 1000.0}s)");
				}
			}

			// Final check with the latest Window.Current reference
			window = Window.Current;
			if (window is null or { Dispatcher: null })
			{
				throw new InvalidOperationException("Window.Current is null or does not have any valid dispatcher after waiting for content");
			}

			// If content is still null after waiting, log a warning but continue
			// We'll set our own content which should work
			if (window.Content is null)
			{
				Log("Window.Current.Content is still null after waiting - proceeding anyway (may be expected on Skia/WebGPU)");
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
		// Re-get the current window to ensure we have the active one
		// (the app may have created a new window after we captured the reference)
		window = Window.Current ?? window;
		Log($"RunTests: Using window={window}, Content={window.Content?.GetType().Name}");

		// Then override the app content by the test control
		Log("RunTests: Creating UnitTestsControl...");
		try
		{
			var engine = new UnitTestsControl { IsSecondaryApp = isSecondaryApp };
			Log("RunTests: UnitTestsControl created, setting as window content...");
			// Use Window.Current to ensure we're setting content on the active window
			var activeWindow = Window.Current ?? window;
			activeWindow.Content = engine;
			Log("RunTests: Waiting for UnitTestsControl to initialize...");

			// On Skia WASM with WebGPU, the Loaded event may not fire because the rendering
			// surface initialization happens asynchronously. Try waiting for Loaded first,
			// but fall back to a simple delay if it doesn't fire.
			try
			{
				// Short timeout - if Loaded fires, great; if not, we'll proceed anyway
				await WaitForLoadedWithTimeout(engine, TimeSpan.FromSeconds(5), ct);
				Log("RunTests: UnitTestsControl loaded successfully");
			}
			catch (TimeoutException)
			{
				// On Skia WASM, the Loaded event may never fire due to WebGPU initialization timing
				// Give the rendering surface time to initialize, then proceed
				Log("RunTests: UnitTestsControl.Loaded event did not fire (expected on Skia/WebGPU) - waiting for renderer...");
				await Task.Delay(3000, ct);  // Wait for WebGPU initialization
				Log("RunTests: Proceeding with test execution after renderer delay");
			}

			// Run the test !
			Log($"RunTests: Starting test execution ({config})");
			await engine.RunTests(ct, config).ConfigureAwait(false);
			Log("RunTests: Test execution completed");

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

			// Extract and POST AOT profile if configured (WASM only)
			await ExtractAndPostAotProfile(ct);
		}
		catch (Exception ex)
		{
			LogError($"RunTests: Exception during test execution: {ex}");
			throw;
		}
	}

	/// <summary>
	/// Extracts AOT profile data and POSTs it to the configured URL.
	/// This is only applicable on WASM platforms when the app was built with AOT profiling enabled.
	/// Supports both .NET WASM (WasmProfilers=aot) and Uno.Wasm.Bootstrap (WasmShellGenerateAOTProfile=true).
	/// </summary>
	private static async Task ExtractAndPostAotProfile(CancellationToken ct)
	{
		var aotProfileUrl = GetConfigValue("UNO_RUNTIME_TESTS_AOT_PROFILE_OUTPUT_URL");
		if (string.IsNullOrEmpty(aotProfileUrl))
		{
			return;
		}

#if __WASM__
		Log("Extracting AOT profile data...");

		try
		{
			// Try to call Uno.AotProfilerSupport.StopProfile() if available
			// This triggers the .NET runtime AOT profiler to dump its data
			try
			{
				var aotProfilerType = Type.GetType("Uno.AotProfilerSupport, Uno.Wasm.AotProfiler");
				if (aotProfilerType != null)
				{
					var stopProfileMethod = aotProfilerType.GetMethod("StopProfile", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
					if (stopProfileMethod != null)
					{
						Log("Calling Uno.AotProfilerSupport.StopProfile()...");
						stopProfileMethod.Invoke(null, null);
					}
				}
			}
			catch (Exception ex)
			{
				Trace($"Uno.AotProfilerSupport.StopProfile not available: {ex.Message}");
			}

			// Give a small delay for the profile data to be populated
			await Task.Delay(200, ct);

			// Extract AOT profile data via JavaScript interop
			// Check multiple possible locations for the profile data
			var profileBase64 = Uno.Foundation.WebAssemblyRuntime.InvokeJS(@"
				(function() {
					try {
						// Helper function to convert data to base64
						function toBase64(data) {
							if (!data || data.length === 0) return '';
							let binary = '';
							const bytes = new Uint8Array(data);
							const chunkSize = 8192;
							for (let i = 0; i < bytes.byteLength; i += chunkSize) {
								const chunk = bytes.subarray(i, Math.min(i + chunkSize, bytes.byteLength));
								binary += String.fromCharCode.apply(null, chunk);
							}
							return btoa(binary);
						}

						// Try .NET runtime INTERNAL.aotProfileData (populated after StopProfile)
						if (typeof getDotnetRuntime === 'function') {
							try {
								const runtime = getDotnetRuntime(0);
								if (runtime && runtime.INTERNAL && runtime.INTERNAL.aotProfileData) {
									const data = runtime.INTERNAL.aotProfileData;
									const result = toBase64(data);
									if (result) {
										console.log('Found AOT profile data in getDotnetRuntime().INTERNAL.aotProfileData (' + data.length + ' bytes)');
										return result;
									}
								}
							} catch (e) {
								console.debug('getDotnetRuntime failed:', e);
							}
						}

						// Try Uno.Wasm.Bootstrap Bootstrapper context
						if (globalThis.Uno && globalThis.Uno.WebAssembly && globalThis.Uno.WebAssembly.Bootstrapper) {
							const bootstrapper = globalThis.Uno.WebAssembly.Bootstrapper;
							// Try direct getAotProfileData method
							if (typeof bootstrapper.getAotProfileData === 'function') {
								try {
									const data = bootstrapper.getAotProfileData();
									const result = toBase64(data);
									if (result) {
										console.log('Found AOT profile data in Bootstrapper.getAotProfileData() (' + data.length + ' bytes)');
										return result;
									}
								} catch (e) {
									console.debug('getAotProfileData failed:', e);
								}
							}
							// Try internal context
							if (bootstrapper._context && bootstrapper._context.INTERNAL && bootstrapper._context.INTERNAL.aotProfileData) {
								const data = bootstrapper._context.INTERNAL.aotProfileData;
								const result = toBase64(data);
								if (result) {
									console.log('Found AOT profile data in Bootstrapper._context.INTERNAL.aotProfileData (' + data.length + ' bytes)');
									return result;
								}
							}
						}

						// Try Module.INTERNAL.aotProfileData
						if (globalThis.Module && globalThis.Module.INTERNAL && globalThis.Module.INTERNAL.aotProfileData) {
							const data = globalThis.Module.INTERNAL.aotProfileData;
							const result = toBase64(data);
							if (result) {
								console.log('Found AOT profile data in Module.INTERNAL.aotProfileData (' + data.length + ' bytes)');
								return result;
							}
						}

						// Try globalThis.aotProfileData (some runtimes expose it here)
						if (globalThis.aotProfileData) {
							const result = toBase64(globalThis.aotProfileData);
							if (result) {
								console.log('Found AOT profile data in globalThis.aotProfileData');
								return result;
							}
						}

						console.log('No AOT profile data found in any known location');
						return '';
					} catch (e) {
						console.error('Failed to extract AOT profile:', e);
						return '';
					}
				})()
			");

			if (string.IsNullOrEmpty(profileBase64))
			{
				Log("No AOT profile data available. Ensure the app was built with WasmProfilers=aot or WasmShellGenerateAOTProfile=true");
				// POST empty data to signal no profile was available
				await WasmTestResultReporter.PostBinaryAsync(aotProfileUrl, Array.Empty<byte>(), ct);
				return;
			}

			// Decode base64 to binary
			var profileData = Convert.FromBase64String(profileBase64);
			Log($"Extracted AOT profile data: {profileData.Length} bytes");

			// POST the binary data
			var success = await WasmTestResultReporter.PostBinaryAsync(aotProfileUrl, profileData, ct);
			if (success)
			{
				Log("Successfully posted AOT profile data");
			}
			else
			{
				LogError("Failed to POST AOT profile data");
			}
		}
		catch (Exception ex)
		{
			LogError($"Failed to extract AOT profile: {ex.Message}");
			// POST empty data to signal extraction failed
			try
			{
				await WasmTestResultReporter.PostBinaryAsync(aotProfileUrl, Array.Empty<byte>(), ct);
			}
			catch
			{
				// Ignore errors when posting empty profile
			}
		}
#else
		Log("AOT profile extraction is only supported on WASM platforms");
		// POST empty data to signal not supported
		await WasmTestResultReporter.PostBinaryAsync(aotProfileUrl, Array.Empty<byte>(), ct);
#endif
	}

	private static void ExitApplication(int exitCode)
	{
		// Set the exit code first, then exit gracefully via Application.Current.Exit()
		// to allow Skia/X11 to clean up properly and avoid segfaults on Linux.
		Environment.ExitCode = exitCode;
		Application.Current.Exit();
	}

	/// <summary>
	/// Gets a configuration value from environment variables, or from URL query parameters on WASM.
	/// </summary>
	/// <remarks>
	/// On WebAssembly, URL query parameters are checked FIRST because they always contain
	/// the current server address. Environment variables from uno-config.js may be stale
	/// due to browser/service worker caching. For the output URL specifically, we always
	/// prefer the URL query parameter to ensure results are sent to the correct server.
	/// </remarks>
	private static string? GetConfigValue(string name)
	{
#if __WASM__
		// On WASM, check URL query parameters FIRST for output URL to avoid stale cached values.
		// The uno-config.js environment variables may be cached by the browser or service worker
		// with an old server port, causing results to be sent to the wrong destination.
		if (name == "UNO_RUNTIME_TESTS_OUTPUT_URL" || name == "UNO_RUNTIME_TESTS_OUTPUT_PATH")
		{
			try
			{
				var js = $"(new URLSearchParams(window.location.search)).get('{name}') || ''";
				var urlValue = Uno.Foundation.WebAssemblyRuntime.InvokeJS(js);
				if (!string.IsNullOrEmpty(urlValue))
				{
					Trace($"Got config value from URL query param (preferred for output): {name}");
					return urlValue;
				}
			}
			catch (Exception ex)
			{
				Trace($"Failed to get URL query param '{name}': {ex.Message}");
			}
		}
#endif

		// Try environment variables (works on all platforms)
		// On WASM, the test runner injects these into uno-config.js
		var value = Environment.GetEnvironmentVariable(name);
		if (!string.IsNullOrEmpty(value))
		{
			return value;
		}

#if __WASM__
		// On WASM, also check URL query parameters as fallback for other config values
		try
		{
			// Use inline JavaScript to get query parameter
			var js = $"(new URLSearchParams(window.location.search)).get('{name}') || ''";
			value = Uno.Foundation.WebAssemblyRuntime.InvokeJS(js);
			if (!string.IsNullOrEmpty(value))
			{
				Trace($"Got config value from URL query param: {name}");
				return value;
			}
		}
		catch (Exception ex)
		{
			Trace($"Failed to get URL query param '{name}': {ex.Message}");
		}
#endif

		return null;
	}

	/// <summary>
	/// Waits for a FrameworkElement to be loaded with a configurable timeout.
	/// </summary>
	/// <remarks>
	/// This is a custom implementation with a longer timeout than UIHelper.WaitForLoaded,
	/// needed for Skia/WebGPU initialization which can take longer than the default 1 second.
	/// </remarks>
	private static async Task WaitForLoadedWithTimeout(FrameworkElement element, TimeSpan timeout, CancellationToken ct)
	{
		if (element.IsLoaded)
		{
			return;
		}

		var tcs = new TaskCompletionSource<bool>();
		using var _ = ct.CanBeCanceled ? ct.Register(() => tcs.TrySetCanceled()) : default;

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			element.Loaded -= OnLoaded;
			tcs.TrySetResult(true);
		}

		try
		{
			element.Loaded += OnLoaded;

			if (!element.IsLoaded)
			{
				var timeoutTask = Task.Delay(timeout, ct);
				var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

				if (completedTask == timeoutTask)
				{
					throw new TimeoutException($"Failed to load element within {timeout}. IsLoaded={element.IsLoaded}");
				}
			}
		}
		finally
		{
			element.Loaded -= OnLoaded;
		}
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