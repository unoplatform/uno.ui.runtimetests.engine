using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Engine.Wasm.Runner;

namespace Uno.UI.RuntimeTests.Engine.Wasm.Runner.Tests;

[TestClass]
public class BrowserDetectionTests
{
	[TestMethod]
	public void GetSystemBrowserPaths_Windows_ContainsEdgeX86Path()
	{
		if (!OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("Windows-only test");
			return;
		}

		var paths = BrowserDetection.GetSystemBrowserPaths().ToList();

		CollectionAssert.Contains(
			paths,
			@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
			"Edge x86 path must be included for Windows machines without Chrome");
	}

	[TestMethod]
	public void GetSystemBrowserPaths_Windows_ContainsEdgeX64Path()
	{
		if (!OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("Windows-only test");
			return;
		}

		var paths = BrowserDetection.GetSystemBrowserPaths().ToList();

		CollectionAssert.Contains(
			paths,
			@"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
			"Edge x64 path must be included for Windows machines without Chrome");
	}

	[TestMethod]
	public void GetSystemBrowserPaths_ReturnsNonEmptyPaths()
	{
		var paths = BrowserDetection.GetSystemBrowserPaths().ToList();

		Assert.IsTrue(paths.Count > 0, "Should return at least one browser path for any OS");
	}

	[TestMethod]
	public void GetSystemBrowserPaths_Windows_ContainsChromeAndEdge()
	{
		if (!OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("Windows-only test");
			return;
		}

		var paths = BrowserDetection.GetSystemBrowserPaths().ToList();

		// Verify both Chrome and Edge paths are present
		Assert.IsTrue(
			paths.Any(p => p.Contains("Chrome", StringComparison.OrdinalIgnoreCase)),
			"Should contain at least one Chrome path");
		Assert.IsTrue(
			paths.Any(p => p.Contains("Edge", StringComparison.OrdinalIgnoreCase)),
			"Should contain at least one Edge path");
	}

	[TestMethod]
	public void FindExecutable_AbsolutePath_NonExistent_ReturnsNull()
	{
		var result = BrowserDetection.FindExecutable(@"C:\NonExistent\Path\browser.exe");

		Assert.IsNull(result);
	}

	[TestMethod]
	public void FindExecutable_RelativeName_NonExistent_ReturnsNull()
	{
		var result = BrowserDetection.FindExecutable("definitely-not-a-real-browser-12345");

		Assert.IsNull(result);
	}

	[TestMethod]
	public void FindChromiumInPlaywrightCache_NonExistentDirectory_ReturnsEmpty()
	{
		var paths = BrowserDetection.FindChromiumInPlaywrightCache(@"C:\NonExistent\Playwright\Cache").ToList();

		Assert.AreEqual(0, paths.Count, "Non-existent cache dir should yield no paths");
	}

	[TestMethod]
	public void GetPlaywrightBrowserPaths_DoesNotThrowDuringEnumeration()
	{
		// Just verify it doesn't throw - actual paths depend on environment
		var paths = BrowserDetection.GetPlaywrightBrowserPaths();
		Assert.IsNotNull(paths, "GetPlaywrightBrowserPaths should never return null");

		// Force enumeration to ensure no exceptions during iteration.
		_ = paths!.ToList();
	}
}
