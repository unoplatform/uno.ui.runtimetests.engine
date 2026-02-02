#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER
#nullable enable

using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests.Engine;

/// <summary>
/// Helper class to report test results via HTTP POST for WebAssembly apps.
/// </summary>
/// <remarks>
/// Since WASM apps run in a browser sandbox and cannot write files to disk,
/// this class provides an alternative mechanism to POST results to a local HTTP endpoint.
/// </remarks>
internal static class WasmTestResultReporter
{
	private static readonly Lazy<HttpClient> _httpClient = new(() => new HttpClient { Timeout = TimeSpan.FromSeconds(30) });

	/// <summary>
	/// Gets the configured output URL from environment variables.
	/// </summary>
	public static string? OutputUrl => Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_OUTPUT_URL");

	/// <summary>
	/// Gets whether HTTP result reporting is enabled.
	/// </summary>
	public static bool IsEnabled => !string.IsNullOrEmpty(OutputUrl);

	/// <summary>
	/// Posts test results to the configured URL.
	/// </summary>
	/// <param name="content">The test results content (NUnit XML or JSON).</param>
	/// <param name="contentType">The content type (application/xml or application/json).</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>True if successful, false otherwise.</returns>
	public static async Task<bool> PostResultsAsync(
		string content,
		string contentType,
		CancellationToken ct = default)
	{
		var url = OutputUrl;
		if (string.IsNullOrEmpty(url))
		{
			return false;
		}

		return await PostResultsAsync(url, content, contentType, ct);
	}

	/// <summary>
	/// Posts test results to the specified URL with retry logic.
	/// </summary>
	/// <param name="url">The URL to POST results to.</param>
	/// <param name="content">The test results content.</param>
	/// <param name="contentType">The content type.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <param name="maxRetries">Maximum number of retry attempts.</param>
	/// <param name="retryDelayMs">Base delay between retries in milliseconds.</param>
	/// <returns>True if successful, false otherwise.</returns>
	public static async Task<bool> PostResultsAsync(
		string url,
		string content,
		string contentType,
		CancellationToken ct = default,
		int maxRetries = 3,
		int retryDelayMs = 1000)
	{
		Exception? lastException = null;

		for (var attempt = 0; attempt < maxRetries; attempt++)
		{
			if (attempt > 0)
			{
				var delay = retryDelayMs * attempt;
				Log($"Retrying POST to {url} (attempt {attempt + 1}/{maxRetries}) after {delay}ms delay...");
				await Task.Delay(delay, ct);
			}

			try
			{
				using var httpContent = new StringContent(content, Encoding.UTF8, contentType);
				var response = await _httpClient.Value.PostAsync(url, httpContent, ct);

				if (response.IsSuccessStatusCode)
				{
					Log($"Successfully posted results to {url}");
					return true;
				}

				LogError($"POST to {url} failed with status {(int)response.StatusCode} {response.ReasonPhrase}");
			}
			catch (HttpRequestException ex)
			{
				lastException = ex;
				LogError($"HTTP request failed: {ex.Message}");
			}
			catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
			{
				lastException = ex;
				LogError($"Request timed out: {ex.Message}");
			}
		}

		LogError($"Failed to POST results after {maxRetries} attempts. Last error: {lastException?.Message}");
		return false;
	}

	/// <summary>
	/// Posts binary data to the specified URL with retry logic.
	/// </summary>
	/// <param name="url">The URL to POST data to.</param>
	/// <param name="data">The binary data to send.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <param name="maxRetries">Maximum number of retry attempts.</param>
	/// <param name="retryDelayMs">Base delay between retries in milliseconds.</param>
	/// <returns>True if successful, false otherwise.</returns>
	public static async Task<bool> PostBinaryAsync(
		string url,
		byte[] data,
		CancellationToken ct = default,
		int maxRetries = 3,
		int retryDelayMs = 1000)
	{
		Exception? lastException = null;

		for (var attempt = 0; attempt < maxRetries; attempt++)
		{
			if (attempt > 0)
			{
				var delay = retryDelayMs * attempt;
				Log($"Retrying POST to {url} (attempt {attempt + 1}/{maxRetries}) after {delay}ms delay...");
				await Task.Delay(delay, ct);
			}

			try
			{
				using var httpContent = new ByteArrayContent(data);
				httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
				var response = await _httpClient.Value.PostAsync(url, httpContent, ct);

				if (response.IsSuccessStatusCode)
				{
					Log($"Successfully posted binary data to {url} ({data.Length} bytes)");
					return true;
				}

				LogError($"POST to {url} failed with status {(int)response.StatusCode} {response.ReasonPhrase}");
			}
			catch (HttpRequestException ex)
			{
				lastException = ex;
				LogError($"HTTP request failed: {ex.Message}");
			}
			catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
			{
				lastException = ex;
				LogError($"Request timed out: {ex.Message}");
			}
		}

		LogError($"Failed to POST binary data after {maxRetries} attempts. Last error: {lastException?.Message}");
		return false;
	}

	private static void Log(string message) => Console.WriteLine($"[WasmTestResultReporter] {message}");
	private static void LogError(string message) => Console.Error.WriteLine($"[WasmTestResultReporter] {message}");
}

#endif
