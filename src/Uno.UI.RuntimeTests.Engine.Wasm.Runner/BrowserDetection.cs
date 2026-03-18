namespace Uno.UI.RuntimeTests.Engine.Wasm.Runner;

/// <summary>
/// Provides methods for detecting Chromium-based browsers on the system.
/// </summary>
internal static class BrowserDetection
{
	/// <summary>
	/// Finds a Chromium-based browser to use for testing.
	/// </summary>
	internal static string? FindChromiumBrowser()
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

	internal static IEnumerable<string> GetPlaywrightBrowserPaths()
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

	internal static IEnumerable<string> FindChromiumInPlaywrightCache(string cacheDir)
	{
		if (!Directory.Exists(cacheDir))
		{
			yield break;
		}

		// Look for chromium-* directories
		string[] chromiumDirs;
		try
		{
			chromiumDirs = Directory.GetDirectories(cacheDir, "chromium-*");
		}
		catch (IOException)
		{
			yield break;
		}
		catch (UnauthorizedAccessException)
		{
			yield break;
		}
		catch (System.Security.SecurityException)
		{
			// If we cannot enumerate the cache directory (e.g., due to permissions or IO errors),
			// treat it as if no Chromium browsers were found in this location.
			yield break;
		}

		foreach (var dir in chromiumDirs.OrderByDescending(d => d)) // Get newest version first
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

	internal static IEnumerable<string> GetSystemBrowserPaths()
	{
		if (OperatingSystem.IsWindows())
		{
			yield return "chrome";
			yield return "chromium";
			yield return @"C:\Program Files\Google\Chrome\Application\chrome.exe";
			yield return @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
			yield return @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
			yield return @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
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

	internal static string? FindExecutable(string name)
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
