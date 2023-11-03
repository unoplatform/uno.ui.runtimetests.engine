#if !UNO_RUNTIMETESTS_DISABLE_UI
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if __SKIA__
#define IS_SECONDARY_APP_SUPPORTED
#endif

namespace Uno.UI.RuntimeTests.Internal.Helpers;

/// <summary>
/// Helper class to run tests in a secondary app.
/// </summary>
/// <remarks>
/// This class is intended to be used only by the the test engine itself and should not be used by applications.
/// API contract is not guaranteed and might change in future releases.
/// </remarks>
internal static partial class SecondaryApp
{
	/// <summary>
	/// Gets a boolean indicating if the current platform supports running tests in a secondary app.
	/// </summary>
	public static bool IsSupported => // Note: not as const to avoid "CS0162 unreachable code" warning
#if IS_SECONDARY_APP_SUPPORTED
		true;
#else
		false;
#endif

	/// <summary>
	/// Run the tests defined by the given configuration in another instance of the current application.
	/// </summary>
	/// <param name="config">Test engine configuration.</param>
	/// <param name="ct">Token to cancel the test run.</param>
	/// <param name="isAppVisible">Indicates if the application should be ran head-less or not.</param>
	/// <returns>The test results.</returns>
	internal static async global::System.Threading.Tasks.Task<TestCaseResult[]> RunTest(UnitTestEngineConfig config, global::System.Threading.CancellationToken ct, bool isAppVisible = false)
	{
#if !IS_SECONDARY_APP_SUPPORTED
		throw new global::System.NotSupportedException("Secondary app is not supported on this platform.");
#else
		// First we fetch and start the dev-server (needed to HR tests for instance)
		await using var devServer = await DevServer.Start(ct);

		// Second we start the app (requesting it to connect to the test-dev-server and to run the tests)
		var resultFile = await RunLocalApp("127.0.0.1", devServer.Port, config, isAppVisible, ct);

		// Finally, read the test results
		try
		{
			var results = await global::System.Text.Json.JsonSerializer.DeserializeAsync<TestCaseResult[]>(global::System.IO.File.OpenRead(resultFile), cancellationToken: ct);
			
			return results ?? global::System.Array.Empty<TestCaseResult>();
		}
		catch (global::System.Text.Json.JsonException error)
		{
			throw new global::System.InvalidOperationException(
				$"Failed to deserialize the test results from '{resultFile}', this usually indicates that the secondary app has been closed (or crashed) before the end of the test suit.", 
				error);
		}
	}

	private static int _instance;

	private static async global::System.Threading.Tasks.Task<string> RunLocalApp(string devServerHost, int devServerPort, UnitTestEngineConfig config, bool isAppVisible, global::System.Threading.CancellationToken ct)
	{
		var testOutput = global::System.IO.Path.GetTempFileName();
		var configJson = global::System.Text.Json.JsonSerializer.Serialize(config, new global::System.Text.Json.JsonSerializerOptions { DefaultIgnoreCondition = global::System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault });

		var childStartInfo = new global::System.Diagnostics.ProcessStartInfo(
			global::System.Environment.ProcessPath ?? throw new global::System.InvalidOperationException("Cannot determine the current app executable path"),
			string.Join(" ", global::System.Linq.Enumerable.Select(global::System.Environment.GetCommandLineArgs(), arg => '"' + arg + '"')))
		{
			UseShellExecute = false,
			CreateNoWindow = !isAppVisible,
			WindowStyle = isAppVisible ? global::System.Diagnostics.ProcessWindowStyle.Normal : global::System.Diagnostics.ProcessWindowStyle.Hidden,
			WorkingDirectory = global::System.Environment.CurrentDirectory,
		};

		// Configure the runtime to allow hot-reload
		childStartInfo.EnvironmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug";

		// Requests to the uno app to attempt to connect to the given dev-server instance
		childStartInfo.EnvironmentVariables.Add("UNO_DEV_SERVER_HOST", devServerHost);
		childStartInfo.EnvironmentVariables.Add("UNO_DEV_SERVER_PORT", devServerPort.ToString());

		// Request to the runtime tests engine to auto-start at startup
		childStartInfo.EnvironmentVariables.Add("UNO_RUNTIME_TESTS_RUN_TESTS", configJson);
		childStartInfo.EnvironmentVariables.Add("UNO_RUNTIME_TESTS_OUTPUT_PATH", testOutput);
		childStartInfo.EnvironmentVariables.Add("UNO_RUNTIME_TESTS_OUTPUT_KIND", "UnoRuntimeTests"); // "NUnit"
		childStartInfo.EnvironmentVariables.Add("UNO_RUNTIME_TESTS_IS_SECONDARY_APP", "true"); // "NUnit"

		var childProcess = new global::System.Diagnostics.Process { StartInfo = childStartInfo };

		await childProcess.ExecuteAndLogAsync(typeof(SecondaryApp).CreateScopedLog($"CHILD_TEST_APP_{(global::System.Threading.Interlocked.Increment(ref _instance)):D2}"), ct);

		return testOutput;
#endif
	}
}
#endif