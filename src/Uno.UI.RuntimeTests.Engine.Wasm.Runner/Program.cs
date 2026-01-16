using System.CommandLine;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Uno.UI.RuntimeTests.Engine.Wasm.Runner;

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

		runCommand.AddOption(appPathOption);
		runCommand.AddOption(outputOption);
		runCommand.AddOption(filterOption);
		runCommand.AddOption(timeoutOption);
		runCommand.AddOption(portOption);
		runCommand.AddOption(headlessOption);

		runCommand.SetHandler(async (context) =>
		{
			var appPath = context.ParseResult.GetValueForOption(appPathOption)!;
			var output = context.ParseResult.GetValueForOption(outputOption)!;
			var filter = context.ParseResult.GetValueForOption(filterOption);
			var timeout = context.ParseResult.GetValueForOption(timeoutOption);
			var port = context.ParseResult.GetValueForOption(portOption);
			var headless = context.ParseResult.GetValueForOption(headlessOption);

			var exitCode = await RunTests(appPath, output, filter, timeout, port, headless);
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

			var exitCode = await RunTests(appPath, output, filter, timeout, port, headless);
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
		bool headless)
	{
		Console.WriteLine($"[WasmRunner] Starting WASM runtime tests runner");
		Console.WriteLine($"[WasmRunner] App path: {appPath.FullName}");
		Console.WriteLine($"[WasmRunner] Output: {output.FullName}");
		Console.WriteLine($"[WasmRunner] Filter: {filter ?? "(none)"}");
		Console.WriteLine($"[WasmRunner] Timeout: {timeoutSeconds}s");
		Console.WriteLine($"[WasmRunner] Headless: {headless}");

		if (!appPath.Exists)
		{
			Console.Error.WriteLine($"[WasmRunner] Error: App path does not exist: {appPath.FullName}");
			return 1;
		}

		// Ensure output directory exists
		output.Directory?.Create();

		// Start HTTP server
		using var server = new TestServer(appPath.FullName, port);
		var serverPort = await server.StartAsync();
		Console.WriteLine($"[WasmRunner] Server started on port {serverPort}");

		// Build the test URL with parameters
		var testConfig = string.IsNullOrEmpty(filter) ? "true" : filter;
		var testUrl = $"http://localhost:{serverPort}/" +
			$"?UNO_RUNTIME_TESTS_RUN_TESTS={Uri.EscapeDataString(testConfig)}" +
			$"&UNO_RUNTIME_TESTS_OUTPUT_URL={Uri.EscapeDataString($"http://localhost:{serverPort}/results")}";

		Console.WriteLine($"[WasmRunner] Test URL: {testUrl}");

		// Find and launch browser
		var browserPath = FindChromiumBrowser();
		if (browserPath is null)
		{
			Console.Error.WriteLine($"[WasmRunner] Error: No Chromium-based browser found.");
			Console.Error.WriteLine($"[WasmRunner] Please install Chromium/Chrome or run: npx playwright install chromium");
			Console.Error.WriteLine($"[WasmRunner] Searched locations:");
			Console.Error.WriteLine($"[WasmRunner]   - PLAYWRIGHT_BROWSERS_PATH environment variable");
			Console.Error.WriteLine($"[WasmRunner]   - Standard Playwright browser cache locations");
			Console.Error.WriteLine($"[WasmRunner]   - System-installed browsers (chromium, google-chrome, etc.)");
			return 1;
		}

		Console.WriteLine($"[WasmRunner] Using browser: {browserPath}");
		Console.WriteLine($"[WasmRunner] Launching browser...");

		// Build browser arguments
		var browserArgs = new List<string>
		{
			testUrl,
			"--no-first-run",
			"--no-default-browser-check",
			"--disable-background-networking",
			"--disable-sync",
			"--disable-translate",
			"--disable-extensions",
			"--disable-infobars",
			"--disable-popup-blocking",
			"--disable-features=TranslateUI",
			"--autoplay-policy=no-user-gesture-required",
			"--no-sandbox", // Required for CI environments (GitHub Actions, Docker, etc.)
			"--disable-dev-shm-usage" // Use /tmp instead of /dev/shm (helps in containerized environments)
		};

		if (headless)
		{
			browserArgs.Add("--headless=new");
		}

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
				Console.Error.WriteLine($"[WasmRunner] Error: Failed to start browser process");
				return 1;
			}

			// Log browser output asynchronously
			_ = Task.Run(async () =>
			{
				while (!browserProcess.HasExited && !ct.IsCancellationRequested)
				{
					var line = await browserProcess.StandardError.ReadLineAsync(ct);
					if (line is not null)
					{
						Console.WriteLine($"[Browser] {line}");
					}
				}
			}, ct);

			// Wait for results
			Console.WriteLine($"[WasmRunner] Waiting for test results (timeout: {timeoutSeconds}s)...");
			var results = await server.WaitForResultsAsync(ct);

			if (results is null)
			{
				Console.Error.WriteLine($"[WasmRunner] Error: No results received");
				return 1;
			}

			// Write results to output file
			Console.WriteLine($"[WasmRunner] Writing results to {output.FullName}...");
			await File.WriteAllTextAsync(output.FullName, results, ct);

			// Parse results to determine exit code
			var hasFailures = results.Contains("result=\"Failed\"") || results.Contains("\"TestResult\":\"Failed\"");
			var exitCodeFromResults = hasFailures ? 1 : 0;

			Console.WriteLine($"[WasmRunner] Tests completed with exit code {exitCodeFromResults}");
			return exitCodeFromResults;
		}
		catch (OperationCanceledException)
		{
			Console.Error.WriteLine($"[WasmRunner] Error: Test execution timed out after {timeoutSeconds} seconds");
			return 2;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[WasmRunner] Error: {ex.Message}");
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

	public TestServer(string appPath, int port)
	{
		_appPath = appPath;
		_requestedPort = port;
		_listener = new HttpListener();
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
				Console.Error.WriteLine($"[Server] Error handling request: {ex.Message}");
			}
		}
	}

	private async Task HandleRequestAsync(HttpListenerContext context)
	{
		var request = context.Request;
		var response = context.Response;

		try
		{
			if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/results")
			{
				// Receive test results
				using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
				var results = await reader.ReadToEndAsync();

				Console.WriteLine($"[Server] Received test results ({results.Length} bytes)");
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
			Console.Error.WriteLine($"[Server] Error: {ex.Message}");
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

		// Add CORS and caching headers for WASM
		response.AddHeader("Cross-Origin-Opener-Policy", "same-origin");
		response.AddHeader("Cross-Origin-Embedder-Policy", "require-corp");

		var content = await File.ReadAllBytesAsync(safePath);
		response.ContentLength64 = content.Length;
		await response.OutputStream.WriteAsync(content);
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
