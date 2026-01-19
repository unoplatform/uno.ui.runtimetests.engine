using System.CommandLine;
using System.Diagnostics;
using System.Net;
using System.Text;
using Spectre.Console;

namespace Uno.UI.RuntimeTests.Engine.Wasm.Runner;

/// <summary>
/// Colored console logging helper using Spectre.Console.
/// </summary>
internal static class Log
{
	public static void Info(string message) =>
		AnsiConsole.MarkupLine($"[blue][[WasmRunner]][/] {Markup.Escape(message)}");

	public static void Success(string message) =>
		AnsiConsole.MarkupLine($"[green][[WasmRunner]][/] {Markup.Escape(message)}");

	public static void Warning(string message) =>
		AnsiConsole.MarkupLine($"[yellow][[WasmRunner]][/] {Markup.Escape(message)}");

	public static void Error(string message) =>
		AnsiConsole.MarkupLine($"[red][[WasmRunner]] Error:[/] {Markup.Escape(message)}");

	public static void Server(string message) =>
		AnsiConsole.MarkupLine($"[magenta][[Server]][/] {Markup.Escape(message)}");

	public static void ServerError(string message) =>
		AnsiConsole.MarkupLine($"[red][[Server]] Error:[/] {Markup.Escape(message)}");

	public static void Browser(string stream, string message) =>
		AnsiConsole.MarkupLine($"[grey][[Browser:{stream}]][/] [dim]{Markup.Escape(message)}[/]");

	public static void Status(string message) =>
		AnsiConsole.MarkupLine($"[cyan][[Status]][/] {Markup.Escape(message)}");

	public static void Detail(string label, string value) =>
		AnsiConsole.MarkupLine($"[blue][[WasmRunner]][/] [grey]{Markup.Escape(label)}:[/] {Markup.Escape(value)}");
}

class Program
{
	static async Task<int> Main(string[] args)
	{
		var rootCommand = new RootCommand("Uno Platform WASM Runtime Tests Runner\n\nPrerequisites:\n  - Install Playwright: npx playwright install chromium\n  - Or: dotnet tool install -g Microsoft.Playwright.CLI && playwright install chromium");

		// Run subcommand (also the default)
		var runCommand = new Command("run", "Run WASM runtime tests");

		var appPathOption = new Option<DirectoryInfo>(
			name: "--app-path",
			description: "Path to the published WASM app directory")
		{
			IsRequired = true
		};

		var outputOption = new Option<FileInfo>(
			name: "--output",
			description: "Path where test results will be written")
		{
			IsRequired = true
		};

		var filterOption = new Option<string?>(
			name: "--filter",
			description: "Test filter expression",
			getDefaultValue: () => null);

		var timeoutOption = new Option<int>(
			name: "--timeout",
			description: "Timeout in seconds for test execution",
			getDefaultValue: () => 300);

		var portOption = new Option<int>(
			name: "--port",
			description: "Port to serve the WASM app on",
			getDefaultValue: () => 0); // 0 = auto-assign

		var headlessOption = new Option<bool>(
			name: "--headless",
			description: "Run browser in headless mode",
			getDefaultValue: () => true);

		var browserPathOption = new Option<FileInfo?>(
			name: "--browser-path",
			description: "Path to the browser executable (auto-detected if not specified)",
			getDefaultValue: () => null);

		var browserArgOption = new Option<string[]>(
			name: "--browser-arg",
			description: "Additional argument to pass to the browser (can be specified multiple times)")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var queryParamOption = new Option<string[]>(
			name: "--query-param",
			description: "Additional query parameter to pass to the app URL in key=value format (can be specified multiple times)")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var browserLogLevelOption = new Option<string>(
			name: "--browser-log-level",
			description: "Browser logging level: 'none' (suppress all), 'minimal' (errors only), 'verbose' (full logging)",
			getDefaultValue: () => "minimal");

		runCommand.AddOption(appPathOption);
		runCommand.AddOption(outputOption);
		runCommand.AddOption(filterOption);
		runCommand.AddOption(timeoutOption);
		runCommand.AddOption(portOption);
		runCommand.AddOption(headlessOption);
		runCommand.AddOption(browserPathOption);
		runCommand.AddOption(browserArgOption);
		runCommand.AddOption(queryParamOption);
		runCommand.AddOption(browserLogLevelOption);

		runCommand.SetHandler(async (context) =>
		{
			var appPath = context.ParseResult.GetValueForOption(appPathOption)!;
			var output = context.ParseResult.GetValueForOption(outputOption)!;
			var filter = context.ParseResult.GetValueForOption(filterOption);
			var timeout = context.ParseResult.GetValueForOption(timeoutOption);
			var port = context.ParseResult.GetValueForOption(portOption);
			var headless = context.ParseResult.GetValueForOption(headlessOption);
			var browserPath = context.ParseResult.GetValueForOption(browserPathOption);
			var browserArgs = context.ParseResult.GetValueForOption(browserArgOption) ?? [];
			var queryParams = context.ParseResult.GetValueForOption(queryParamOption) ?? [];
			var browserLogLevel = context.ParseResult.GetValueForOption(browserLogLevelOption)!;

			var exitCode = await RunTests(appPath, output, filter, timeout, port, headless, browserPath, browserArgs, queryParams, browserLogLevel);
			context.ExitCode = exitCode;
		});

		rootCommand.AddCommand(runCommand);

		// Also add the run options to root command for convenience (backward compat)
		rootCommand.AddOption(appPathOption);
		rootCommand.AddOption(outputOption);
		rootCommand.AddOption(filterOption);
		rootCommand.AddOption(timeoutOption);
		rootCommand.AddOption(portOption);
		rootCommand.AddOption(headlessOption);
		rootCommand.AddOption(browserPathOption);
		rootCommand.AddOption(browserArgOption);
		rootCommand.AddOption(queryParamOption);
		rootCommand.AddOption(browserLogLevelOption);

		rootCommand.SetHandler(async (context) =>
		{
			var appPath = context.ParseResult.GetValueForOption(appPathOption);
			var output = context.ParseResult.GetValueForOption(outputOption);

			// If no options provided, show help
			if (appPath is null || output is null)
			{
				return;
			}

			var filter = context.ParseResult.GetValueForOption(filterOption);
			var timeout = context.ParseResult.GetValueForOption(timeoutOption);
			var port = context.ParseResult.GetValueForOption(portOption);
			var headless = context.ParseResult.GetValueForOption(headlessOption);
			var browserPath = context.ParseResult.GetValueForOption(browserPathOption);
			var browserArgs = context.ParseResult.GetValueForOption(browserArgOption) ?? [];
			var queryParams = context.ParseResult.GetValueForOption(queryParamOption) ?? [];
			var browserLogLevel = context.ParseResult.GetValueForOption(browserLogLevelOption)!;

			var exitCode = await RunTests(appPath, output, filter, timeout, port, headless, browserPath, browserArgs, queryParams, browserLogLevel);
			context.ExitCode = exitCode;
		});

		return await rootCommand.InvokeAsync(args);
	}

	static async Task<int> RunTests(
		DirectoryInfo appPath,
		FileInfo output,
		string? filter,
		int timeoutSeconds,
		int port,
		bool headless,
		FileInfo? browserPathOverride,
		string[] additionalBrowserArgs,
		string[] queryParams,
		string browserLogLevel)
	{
		AnsiConsole.MarkupLine("[bold blue]Uno Platform WASM Runtime Tests Runner[/]");
		AnsiConsole.WriteLine();
		Log.Detail("App path", appPath.FullName);
		Log.Detail("Output", output.FullName);
		Log.Detail("Filter", filter ?? "(none)");
		Log.Detail("Timeout", $"{timeoutSeconds}s");
		Log.Detail("Headless", headless.ToString());
		Log.Detail("Browser log level", browserLogLevel);
		if (browserPathOverride is not null)
		{
			Log.Detail("Browser path", browserPathOverride.FullName);
		}
		if (additionalBrowserArgs.Length > 0)
		{
			Log.Detail("Browser args", string.Join(" ", additionalBrowserArgs));
		}
		if (queryParams.Length > 0)
		{
			Log.Detail("Query params", string.Join(" ", queryParams));
		}
		AnsiConsole.WriteLine();

		if (!appPath.Exists)
		{
			Log.Error($"App path does not exist: {appPath.FullName}");
			return 1;
		}

		// Verify index.html exists
		var indexPath = Path.Combine(appPath.FullName, "index.html");
		if (!File.Exists(indexPath))
		{
			Log.Error($"index.html not found at: {indexPath}");
			AnsiConsole.MarkupLine("[yellow]Contents of app directory:[/]");
			foreach (var file in Directory.GetFiles(appPath.FullName).Take(20))
			{
				AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(Path.GetFileName(file))}[/]");
			}
			foreach (var dir in Directory.GetDirectories(appPath.FullName).Take(10))
			{
				AnsiConsole.MarkupLine($"  [blue]{Markup.Escape(Path.GetFileName(dir))}/[/]");
			}
			return 1;
		}
		Log.Success($"Found index.html ({new FileInfo(indexPath).Length} bytes)");

		// Ensure output directory exists
		output.Directory?.Create();

		// Start HTTP server
		using var server = new TestServer(appPath.FullName, port);

		// Inject environment variables into uno-config.js
		// This is the primary mechanism for passing test configuration to the WASM app
		// Note: URL query parameters are also passed for backward compatibility,
		// but Uno WASM doesn't automatically map them to Environment.GetEnvironmentVariable()
		var testConfig = string.IsNullOrEmpty(filter) ? "true" : filter;
		var serverPort = await server.StartAsync();

		// Now that we know the port, set up the injected environment variables
		var injectedEnvVars = new Dictionary<string, string>
		{
			["UNO_RUNTIME_TESTS_RUN_TESTS"] = testConfig,
			["UNO_RUNTIME_TESTS_OUTPUT_URL"] = $"http://localhost:{serverPort}/results"
		};
		server.SetInjectedEnvironmentVariables(injectedEnvVars);

		Log.Success($"Server started on port {serverPort}");

		// Build the test URL with parameters (for backward compatibility and debugging)
		// Add cache-busting parameter to bypass service worker caching
		var cacheBuster = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		var testUrlBuilder = new StringBuilder();
		testUrlBuilder.Append($"http://localhost:{serverPort}/");
		testUrlBuilder.Append($"?_cb={cacheBuster}"); // Cache buster
		testUrlBuilder.Append($"&UNO_RUNTIME_TESTS_RUN_TESTS={Uri.EscapeDataString(testConfig)}");
		testUrlBuilder.Append($"&UNO_RUNTIME_TESTS_OUTPUT_URL={Uri.EscapeDataString($"http://localhost:{serverPort}/results")}");

		// Add any additional query parameters
		foreach (var param in queryParams)
		{
			var separatorIndex = param.IndexOf('=');
			if (separatorIndex > 0)
			{
				var key = param[..separatorIndex];
				var value = param[(separatorIndex + 1)..];
				testUrlBuilder.Append($"&{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
			}
			else
			{
				Log.Warning($"Invalid query parameter format '{param}' (expected key=value), skipping");
			}
		}

		var testUrl = testUrlBuilder.ToString();
		Log.Detail("Test URL", testUrl);

		// Find and launch browser
		string? browserPath;
		if (browserPathOverride is not null)
		{
			if (!browserPathOverride.Exists)
			{
				Log.Error($"Specified browser path does not exist: {browserPathOverride.FullName}");
				return 1;
			}
			browserPath = browserPathOverride.FullName;
		}
		else
		{
			browserPath = FindChromiumBrowser();
			if (browserPath is null)
			{
				Log.Error("No Chromium-based browser found.");
				AnsiConsole.MarkupLine("[yellow]Please install Chromium/Chrome or run:[/] [green]npx playwright install chromium[/]");
				AnsiConsole.MarkupLine("[grey]Searched locations:[/]");
				AnsiConsole.MarkupLine("[grey]  - PLAYWRIGHT_BROWSERS_PATH environment variable[/]");
				AnsiConsole.MarkupLine("[grey]  - Standard Playwright browser cache locations[/]");
				AnsiConsole.MarkupLine("[grey]  - System-installed browsers (chromium, google-chrome, etc.)[/]");
				return 1;
			}
		}

		Log.Detail("Browser", browserPath);

		// Build browser arguments
		// Create a unique temp directory for each run to ensure fresh browser state (no cached data)
		var tempUserDataDir = Path.Combine(Path.GetTempPath(), $"uno-wasm-runner-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempUserDataDir);

		var browserArgs = new List<string>
		{
			testUrl,
			$"--user-data-dir={tempUserDataDir}", // Fresh profile for each run to avoid cached data
			"--no-first-run",
			"--no-default-browser-check",
			"--disable-background-networking",
			"--disable-sync",
			"--disable-translate",
			"--disable-extensions",
			"--disable-infobars",
			"--disable-popup-blocking",
			"--disable-features=TranslateUI", // Don't disable ServiceWorker as it might cause issues
			"--autoplay-policy=no-user-gesture-required",
			"--no-sandbox", // Required for CI environments (GitHub Actions, Docker, etc.)
			"--disable-dev-shm-usage", // Use /tmp instead of /dev/shm (helps in containerized environments)
			"--disable-application-cache", // Disable application cache
			"--disk-cache-size=0" // No disk cache
		};

		// Add Chrome logging flags based on browser log level
		if (browserLogLevel == "verbose")
		{
			browserArgs.Add("--enable-logging=stderr");
			browserArgs.Add("--v=1");
		}

		if (headless)
		{
			browserArgs.Add("--headless=new");
		}

		// Add any additional browser arguments from CLI
		browserArgs.AddRange(additionalBrowserArgs);

		Log.Info($"Launching browser with {browserArgs.Count} arguments...");

		// Create cancellation token for timeout
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
		var ct = cts.Token;

		Process? browserProcess = null;
		try
		{
			browserProcess = Process.Start(new ProcessStartInfo
			{
				FileName = browserPath,
				Arguments = string.Join(" ", browserArgs),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			});

			if (browserProcess is null)
			{
				Log.Error("Failed to start browser process");
				return 1;
			}

			Log.Success($"Browser started (PID: {browserProcess.Id})");

			// Log browser stdout asynchronously (only for verbose level)
			_ = Task.Run(async () =>
			{
				while (!browserProcess.HasExited && !ct.IsCancellationRequested)
				{
					var line = await browserProcess.StandardOutput.ReadLineAsync(ct);
					if (line is not null && browserLogLevel == "verbose")
					{
						Log.Browser("stdout", line);
					}
				}
			}, ct);

			// Log browser stderr asynchronously (for verbose and minimal levels)
			_ = Task.Run(async () =>
			{
				while (!browserProcess.HasExited && !ct.IsCancellationRequested)
				{
					var line = await browserProcess.StandardError.ReadLineAsync(ct);
					if (line is not null && browserLogLevel != "none")
					{
						Log.Browser("stderr", line);
					}
				}
			}, ct);

			// Wait for results, but also monitor if browser exits early
			Log.Info($"Waiting for test results (timeout: {timeoutSeconds}s)...");
			AnsiConsole.WriteLine();

			// Start a task to monitor browser exit
			var browserExitTask = Task.Run(async () =>
			{
				await browserProcess.WaitForExitAsync(ct);
				Log.Warning($"Browser process exited with code: {browserProcess.ExitCode}");
			}, ct);

			// Start a task to periodically log status
			var statusLogTask = Task.Run(async () =>
			{
				var startTime = DateTime.UtcNow;
				var lastRequestCount = 0;
				while (!ct.IsCancellationRequested)
				{
					await Task.Delay(TimeSpan.FromSeconds(30), ct);
					var elapsed = DateTime.UtcNow - startTime;
					var currentRequests = server.RequestCount;
					var newRequests = currentRequests - lastRequestCount;
					lastRequestCount = currentRequests;
					Log.Status($"{elapsed.TotalSeconds:F0}s elapsed | {currentRequests} requests (+{newRequests}) | browser: {(browserProcess.HasExited ? "exited" : "running")}");
				}
			}, ct);

			var results = await server.WaitForResultsAsync(ct);

			if (results is null)
			{
				AnsiConsole.WriteLine();
				Log.Error($"No results received");
				AnsiConsole.MarkupLine($"[grey]Total HTTP requests received:[/] [yellow]{server.RequestCount}[/]");
				// Check if browser already exited
				if (browserProcess.HasExited)
				{
					Log.Error($"Browser exited with code {browserProcess.ExitCode} before providing results");
				}
				else
				{
					Log.Warning("Browser is still running - possible hang or navigation failure");
				}
				return 1;
			}

			AnsiConsole.WriteLine();

			// Write results to output file
			Log.Info($"Writing results to {output.FullName}...");
			await File.WriteAllTextAsync(output.FullName, results, ct);

			// Parse results to determine exit code
			var hasFailures = results.Contains("result=\"Failed\"") || results.Contains("\"TestResult\":\"Failed\"");
			var exitCodeFromResults = hasFailures ? 1 : 0;

			if (hasFailures)
			{
				AnsiConsole.MarkupLine($"[red]Tests completed with failures (exit code {exitCodeFromResults})[/]");
			}
			else
			{
				AnsiConsole.MarkupLine($"[green]Tests completed successfully![/]");
			}
			return exitCodeFromResults;
		}
		catch (OperationCanceledException)
		{
			AnsiConsole.WriteLine();
			Log.Error($"Test execution timed out after {timeoutSeconds} seconds");
			AnsiConsole.MarkupLine($"[grey]Total HTTP requests received:[/] [yellow]{server.RequestCount}[/]");
			return 2;
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message);
			return 1;
		}
		finally
		{
			// Clean up browser process
			if (browserProcess is not null && !browserProcess.HasExited)
			{
				try
				{
					browserProcess.Kill(entireProcessTree: true);
					await browserProcess.WaitForExitAsync();
				}
				catch
				{
					// Ignore errors during cleanup
				}
			}
			browserProcess?.Dispose();
		}
	}

	/// <summary>
	/// Finds a Chromium-based browser to use for testing.
	/// </summary>
	static string? FindChromiumBrowser()
	{
		// Check for Playwright-installed browsers first
		var playwrightBrowsers = GetPlaywrightBrowserPaths();
		foreach (var path in playwrightBrowsers)
		{
			if (File.Exists(path))
			{
				return path;
			}
		}

		// Fall back to system-installed browsers
		var systemBrowsers = GetSystemBrowserPaths();
		foreach (var browser in systemBrowsers)
		{
			var path = FindExecutable(browser);
			if (path is not null)
			{
				return path;
			}
		}

		return null;
	}

	static IEnumerable<string> GetPlaywrightBrowserPaths()
	{
		// Check PLAYWRIGHT_BROWSERS_PATH first
		var customPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
		if (!string.IsNullOrEmpty(customPath))
		{
			foreach (var chromiumPath in FindChromiumInPlaywrightCache(customPath))
			{
				yield return chromiumPath;
			}
		}

		// Standard Playwright cache locations
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

		if (OperatingSystem.IsWindows())
		{
			var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			foreach (var chromiumPath in FindChromiumInPlaywrightCache(Path.Combine(localAppData, "ms-playwright")))
			{
				yield return chromiumPath;
			}
		}
		else if (OperatingSystem.IsMacOS())
		{
			foreach (var chromiumPath in FindChromiumInPlaywrightCache(Path.Combine(home, "Library", "Caches", "ms-playwright")))
			{
				yield return chromiumPath;
			}
		}
		else // Linux
		{
			foreach (var chromiumPath in FindChromiumInPlaywrightCache(Path.Combine(home, ".cache", "ms-playwright")))
			{
				yield return chromiumPath;
			}
		}
	}

	static IEnumerable<string> FindChromiumInPlaywrightCache(string cacheDir)
	{
		if (!Directory.Exists(cacheDir))
		{
			yield break;
		}

		// Look for chromium-* directories
		var chromiumDirs = Directory.GetDirectories(cacheDir, "chromium-*")
			.OrderByDescending(d => d); // Get newest version first

		foreach (var dir in chromiumDirs)
		{
			if (OperatingSystem.IsWindows())
			{
				yield return Path.Combine(dir, "chrome-win", "chrome.exe");
			}
			else if (OperatingSystem.IsMacOS())
			{
				yield return Path.Combine(dir, "chrome-mac", "Chromium.app", "Contents", "MacOS", "Chromium");
			}
			else // Linux
			{
				// Newer Playwright uses chrome-linux64, older uses chrome-linux
				yield return Path.Combine(dir, "chrome-linux64", "chrome");
				yield return Path.Combine(dir, "chrome-linux", "chrome");
			}
		}
	}

	static IEnumerable<string> GetSystemBrowserPaths()
	{
		if (OperatingSystem.IsWindows())
		{
			yield return "chrome";
			yield return "chromium";
			yield return @"C:\Program Files\Google\Chrome\Application\chrome.exe";
			yield return @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
			yield return @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
		}
		else if (OperatingSystem.IsMacOS())
		{
			yield return "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
			yield return "/Applications/Chromium.app/Contents/MacOS/Chromium";
			yield return "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge";
			yield return "chromium";
			yield return "google-chrome";
		}
		else // Linux
		{
			yield return "chromium-browser";
			yield return "chromium";
			yield return "google-chrome-stable";
			yield return "google-chrome";
			yield return "microsoft-edge-stable";
			yield return "microsoft-edge";
		}
	}

	static string? FindExecutable(string name)
	{
		// If it's an absolute path, check if it exists
		if (Path.IsPathRooted(name))
		{
			return File.Exists(name) ? name : null;
		}

		// Search in PATH
		var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
		var paths = pathEnv.Split(Path.PathSeparator);

		foreach (var path in paths)
		{
			var fullPath = Path.Combine(path, name);
			if (File.Exists(fullPath))
			{
				return fullPath;
			}

			// On Windows, try with .exe extension
			if (OperatingSystem.IsWindows())
			{
				var exePath = fullPath + ".exe";
				if (File.Exists(exePath))
				{
					return exePath;
				}
			}
		}

		return null;
	}
}

/// <summary>
/// Simple HTTP server that serves the WASM app and receives test results.
/// </summary>
internal sealed class TestServer : IDisposable
{
	private readonly string _appPath;
	private readonly int _requestedPort;
	private readonly HttpListener _listener;
	private readonly TaskCompletionSource<string> _resultsTcs = new();
	private CancellationTokenSource? _serverCts;
	private Task? _serverTask;
	private int _requestCount;
	private Dictionary<string, string>? _injectedEnvVars;

	public int RequestCount => _requestCount;

	public TestServer(string appPath, int port)
	{
		_appPath = appPath;
		_requestedPort = port;
		_listener = new HttpListener();
	}

	/// <summary>
	/// Sets environment variables to inject into the uno-config.js when served.
	/// This allows the WASM app to read test configuration via Environment.GetEnvironmentVariable().
	/// </summary>
	public void SetInjectedEnvironmentVariables(Dictionary<string, string> envVars)
	{
		_injectedEnvVars = envVars;
	}

	public async Task<int> StartAsync()
	{
		// Find an available port if not specified
		var port = _requestedPort == 0 ? FindAvailablePort() : _requestedPort;

		_listener.Prefixes.Add($"http://localhost:{port}/");
		_listener.Start();

		_serverCts = new CancellationTokenSource();
		_serverTask = RunServerAsync(_serverCts.Token);

		return port;
	}

	private static int FindAvailablePort()
	{
		using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	private async Task RunServerAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				var context = await _listener.GetContextAsync().WaitAsync(ct);
				_ = HandleRequestAsync(context);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				Log.ServerError($"Error handling request: {ex.Message}");
			}
		}
	}

	private async Task HandleRequestAsync(HttpListenerContext context)
	{
		var request = context.Request;
		var response = context.Response;

		var count = Interlocked.Increment(ref _requestCount);
		Log.Server($"#{count} {request.HttpMethod} {request.Url?.AbsolutePath}");

		try
		{
			if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/results")
			{
				// Receive test results
				using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
				var results = await reader.ReadToEndAsync();

				AnsiConsole.MarkupLine($"[magenta][[Server]][/] [green]Received test results ({results.Length} bytes)[/]");
				_resultsTcs.TrySetResult(results);

				response.StatusCode = 200;
				response.ContentType = "text/plain";
				var responseBytes = "OK"u8.ToArray();
				await response.OutputStream.WriteAsync(responseBytes);
			}
			else if (request.HttpMethod == "GET")
			{
				// Serve static files
				await ServeStaticFileAsync(request, response);
			}
			else
			{
				response.StatusCode = 404;
			}
		}
		catch (Exception ex)
		{
			Log.ServerError(ex.Message);
			response.StatusCode = 500;
		}
		finally
		{
			response.Close();
		}
	}

	private async Task ServeStaticFileAsync(HttpListenerRequest request, HttpListenerResponse response)
	{
		var path = request.Url?.AbsolutePath ?? "/";
		if (path == "/")
		{
			path = "/index.html";
		}

		// Security: prevent directory traversal
		var safePath = Path.GetFullPath(Path.Combine(_appPath, path.TrimStart('/')));
		if (!safePath.StartsWith(_appPath))
		{
			response.StatusCode = 403;
			return;
		}

		if (!File.Exists(safePath))
		{
			response.StatusCode = 404;
			return;
		}

		var extension = Path.GetExtension(safePath).ToLowerInvariant();
		response.ContentType = extension switch
		{
			".html" => "text/html",
			".js" => "application/javascript",
			".wasm" => "application/wasm",
			".css" => "text/css",
			".json" => "application/json",
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			".svg" => "image/svg+xml",
			".ico" => "image/x-icon",
			".dat" => "application/octet-stream",
			".clr" => "application/octet-stream",
			".dll" => "application/octet-stream",
			".pdb" => "application/octet-stream",
			".blat" => "application/octet-stream",
			_ => "application/octet-stream"
		};

		// Add CORS headers for WASM
		response.AddHeader("Cross-Origin-Opener-Policy", "same-origin");
		response.AddHeader("Cross-Origin-Embedder-Policy", "require-corp");

		byte[] content;
		var fileName = Path.GetFileName(safePath);

		// If this is uno-config.js and we have injected environment variables, modify the content
		// Also add aggressive no-cache headers to prevent service worker from serving stale config
		if (fileName == "uno-config.js" && _injectedEnvVars?.Count > 0)
		{
			// Add aggressive no-cache headers to prevent service worker from serving stale config
			response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
			response.AddHeader("Pragma", "no-cache");
			response.AddHeader("Expires", "0");

			var originalContent = await File.ReadAllTextAsync(safePath);
			var modifiedContent = InjectEnvironmentVariables(originalContent);
			content = Encoding.UTF8.GetBytes(modifiedContent);
			Log.Server("Injected environment variables into uno-config.js");
		}
		else
		{
			content = await File.ReadAllBytesAsync(safePath);
		}

		response.ContentLength64 = content.Length;
		await response.OutputStream.WriteAsync(content);
	}

	/// <summary>
	/// Injects environment variables into the uno-config.js content.
	/// </summary>
	private string InjectEnvironmentVariables(string configContent)
	{
		if (_injectedEnvVars is null || _injectedEnvVars.Count == 0)
		{
			return configContent;
		}

		// Build JavaScript code to add environment variables
		var sb = new StringBuilder();
		sb.AppendLine();
		sb.AppendLine("// Injected by WASM Runtime Tests Runner");
		foreach (var kvp in _injectedEnvVars)
		{
			// Escape the value for JavaScript string
			var escapedValue = kvp.Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
			sb.AppendLine($"config.environmentVariables[\"{kvp.Key}\"] = \"{escapedValue}\";");
		}

		// Find the export statement and insert before it
		var exportIndex = configContent.LastIndexOf("export { config };");
		if (exportIndex >= 0)
		{
			return configContent.Insert(exportIndex, sb.ToString());
		}

		// Fallback: append to the end
		return configContent + sb.ToString();
	}

	public async Task<string?> WaitForResultsAsync(CancellationToken ct)
	{
		try
		{
			return await _resultsTcs.Task.WaitAsync(ct);
		}
		catch (OperationCanceledException)
		{
			return null;
		}
	}

	public void Dispose()
	{
		_serverCts?.Cancel();
		_listener.Stop();
		_listener.Close();
	}
}
