#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#pragma warning disable CA1848 // Log perf
#pragma warning disable CA1823 // Field not used

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Engine;

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml;
#else
using Windows.UI.Xaml;
#endif

namespace Uno.UI.RuntimeTests;

public static partial class HotReloadHelper
{
	public partial record FileEdit(string FilePath, string OldText, string NewText)
	{
		public FileEdit Revert() => this with { OldText = NewText, NewText = OldText };
	}

	private record RevertFileEdit(FileEdit Edition, bool WaitForMetadataUpdate) : IAsyncDisposable
	{
		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			var revert = Edition.Revert();

			_log.LogInformation($"Reverting changes made on {revert.FilePath} (from: \"{StartEnd(revert.OldText)}\" to \"{StartEnd(revert.NewText)}\").");

			// Note: We also wait for metadata update here to ensure that the file is reverted before the test continues / we run another test.
			if (await SendMessageCore(revert, WaitForMetadataUpdate, CancellationToken.None) is ReConnect reconnect)
			{
				_log.LogWarning($"Failed to revert file edition, let {_impl} reconnect.");

				await reconnect(CancellationToken.None);
				if (await SendMessageCore(revert, WaitForMetadataUpdate, CancellationToken.None) is not null)
				{
					_log.LogError($"Failed to revert file edition on file {revert.FilePath}.");
				}
			}
		}
	}

	public delegate Task ReConnect(CancellationToken ct);

	public interface IFileUpdater
	{
		ValueTask EnsureReady(CancellationToken ct);

		ValueTask<ReConnect?> Apply(FileEdit edition, CancellationToken ct);
	}

	private static readonly ILogger _log = typeof(HotReloadHelper).Log();
	private static IFileUpdater _impl = new NotSupported();

	// The delay for the client to connect to the dev-server
	private static TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(3);

	// The delay for the server to load the workspace and let the client know it's ready
	private static TimeSpan WorkspaceTimeout = TimeSpan.FromSeconds(30);

	// The delay for the server to send metadata update once a file has been modified
	private static TimeSpan MetadataUpdateTimeout = TimeSpan.FromSeconds(5);

	static HotReloadHelper()
	{
		if (!IsSupported)
		{
			TryUseDevServerFileUpdater();
		}

		if (!IsSupported)
		{
			TryUseLocalFileUpdater();
		}
	}

	static partial void TryUseDevServerFileUpdater();
	static partial void TryUseLocalFileUpdater();

	/// <summary>
	/// Configures the <see cref="IFileUpdater"/> to use.
	/// </summary>
	/// <param name="updater"></param>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void Use(IFileUpdater updater)
		=> _impl = updater;

	/// <summary>
	/// Gets a boolean which indicates if the HotReloadHelper is supported on the current platform.
	/// </summary>
	/// <remarks>If this returns false, all methods on this class are expected to throw exceptions.</remarks>
	public static bool IsSupported => _impl is not NotSupported;

	/// <summary>
	/// Request to replace all occurrences of <paramref name="originalText"/> by the <paramref name="replacementText"/> in the given source code file.
	/// </summary>
	/// <param name="filPathRelativeToProject">Path of file in which the replacement should take place, relative to the project which reference the runtime-test engine (usually the application head).</param>
	/// <param name="originalText">The original text to replace.</param>
	/// <param name="replacementText">The replacement text.</param>
	/// <param name="ct">An cancellation token to abort the asynchronous operation.</param>
	/// <returns>An IAsyncDisposable object that will revert the change when disposed.</returns>
	public static async ValueTask<IAsyncDisposable> UpdateSourceFile(string filPathRelativeToProject, string originalText, string replacementText, CancellationToken ct = default)
		=> await UpdateSourceFile(filPathRelativeToProject, originalText, replacementText, true, ct);

	/// <summary>
	/// Request to replace all occurrences of <paramref name="originalText"/> by the <paramref name="replacementText"/> in the given source code file.
	/// </summary>
	/// <param name="filPathRelativeToProject">Path of file in which the replacement should take place, relative to the project which reference the runtime-test engine (usually the application head).</param>
	/// <param name="originalText">The original text to replace.</param>
	/// <param name="replacementText">The replacement text.</param>
	/// <param name="waitForMetadataUpdate">Request to wait for a metadata update to consider the file update to be applied.</param>
	/// <param name="ct">An cancellation token to abort the asynchronous operation.</param>
	/// <returns>An IAsyncDisposable object that will revert the change when disposed.</returns>
	public static async ValueTask<IAsyncDisposable> UpdateSourceFile(string filPathRelativeToProject, string originalText, string replacementText, bool waitForMetadataUpdate, CancellationToken ct = default)
	{
		var projectFile = typeof(HotReloadHelper).Assembly.GetCustomAttribute<RuntimeTestsSourceProjectAttribute>()?.ProjectFullPath;
		if (projectFile is null or { Length: 0 })
		{
			throw new InvalidOperationException("The project file path could not be found.");
		}

		var projectDir = Path.GetDirectoryName(projectFile) ?? "";
#if __SKIA__
		if (!File.Exists(projectFile)) // Sanity!
		{
			throw new InvalidOperationException("Unable to find project file.");
		}

		if (!Directory.Exists(projectDir))
		{
			throw new InvalidOperationException("Unable to find project directory.");
		}
#endif

		return await UpdateSourceFile(new FileEdit(Path.Combine(projectDir, filPathRelativeToProject), originalText, replacementText), waitForMetadataUpdate, ct);
	}

	/// <summary>
	/// Request to replace all occurrences of <paramref name="originalText"/> by the <paramref name="replacementText"/> in the source file of the given <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">Type of the <see cref="FrameworkElement"/> for which the source code should be edited.</typeparam>
	/// <param name="originalText">The original text to replace.</param>
	/// <param name="replacementText">The replacement text.</param>
	/// <param name="ct">An cancellation token to abort the asynchronous operation.</param>
	/// <returns>An IAsyncDisposable object that will revert the change when disposed.</returns>
	public static async ValueTask<IAsyncDisposable> UpdateSourceFile<T>(string originalText, string replacementText, CancellationToken ct = default)
		where T : FrameworkElement, new()
	{
		var edition = new T().CreateFileEdit(
			originalText: originalText,
			replacementText: replacementText);

		return await UpdateSourceFile(edition, true, ct);
	}

	/// <summary>
	/// Request to apply the given edition on the source code file.
	/// </summary>
	/// <param name="message">The file update request.</param>
	/// <param name="waitForMetadataUpdate">Request to wait for a metadata update to consider the file update to be applied.</param>
	/// <param name="ct">An cancellation token to abort the asynchronous operation.</param>
	/// <returns>An IAsyncDisposable object that will revert the change when disposed.</returns>
	public static async Task<IAsyncDisposable> UpdateSourceFile(FileEdit message, bool waitForMetadataUpdate, CancellationToken ct = default)
	{
		_log.LogTrace($"Waiting for connection in order to update file {message.FilePath} (from: \"{StartEnd(message.OldText)}\" to \"{StartEnd(message.NewText)}\") ...");
		await _impl.EnsureReady(ct);

		var revertMessage = new RevertFileEdit(message, waitForMetadataUpdate);
		try
		{
			await SendMessageCore(message, waitForMetadataUpdate, ct);
		}
		catch
		{
			await revertMessage.DisposeAsync();
			
			throw;
		}

		return revertMessage;
	}

	/// <summary>
	/// Creates a file update request to replace text in the source file of the given <paramref name="element"/>.
	/// </summary>
	/// <param name="element">An instance of the <see cref="FrameworkElement"/> for which the source code should be edited.</param>
	/// <param name="originalText">The original text to replace.</param>
	/// <param name="replacementText">The replacement text.</param>
	/// <returns>A file update request that can be sent to the dev-server.</returns>
	public static FileEdit CreateFileEdit(
		this FrameworkElement element,
		string originalText,
		string replacementText)
		=> new(element.GetDebugParseContext().FileName, originalText, replacementText);

	private static async Task<ReConnect?> SendMessageCore(FileEdit message, bool waitForMetadataUpdate, CancellationToken ct)
	{
		if (waitForMetadataUpdate)
		{
			var cts = new TaskCompletionSource<object?>();
			using var ctReg = ct.Register(() => cts.TrySetCanceled());
			void UpdateReceived(object? sender, object? args) => cts.TrySetResult(default);

			try
			{
				MetadataUpdateHandler.MetadataUpdated += UpdateReceived;
				//MetadataUpdaterHelper.MetadataUpdated += UpdateReceived;

				var reconnect = await _impl.Apply(message, ct);
				if (reconnect is not null)
				{
					_log.LogTrace("Failed to request file edition, ignore metadata update (i.e. the code changes to be applied in the current app) waiting ...");

					return reconnect;
				}

				_log.LogTrace("File edition requested, waiting for metadata update (i.e. the code changes to be applied in the current app) ...");

				var timeout = Task.Delay(MetadataUpdateTimeout, ct);
				if (await Task.WhenAny(timeout, cts.Task) == timeout)
				{
					throw new TimeoutException(
						"Timeout while waiting for metadata update. "
						+ "This usually indicates that the dev-server failed to process the requested update, you will find more info in dev-server logs "
						+ "(output in main app logs with [DEV_SERVER] prefix for runtime-tests run in secondary app instance).");
				}
			}
			finally
			{
				//MetadataUpdaterHelper.MetadataUpdated -= UpdateReceived;
				MetadataUpdateHandler.MetadataUpdated -= UpdateReceived;
			}

			await Task.Delay(100, ct); // Let the metadata to be updated by all handlers.

			_log.LogTrace("Received **a** metadata update, continuing test.");
			return null;
		}
		else
		{
			var result = await _impl.Apply(message, ct);

			_log.LogTrace("File edition requested, continuing test without waiting for metadata update.");

			return result;
		}
	}

	#region Helpers
	private static (string FileName, int FileLine, int LinePosition) GetDebugParseContext(this FrameworkElement element)
	{
		var dpcProp = typeof(FrameworkElement).GetProperty("DebugParseContext", BindingFlags.Instance | BindingFlags.NonPublic);
		if (dpcProp == null)
		{
			throw new InvalidOperationException("Could not find DebugParseContext property on FrameworkElement. You should consider to define the property '' in your project in order to enable it's generation even in release.");
		}

		var dpcForElement = dpcProp.GetValue(element);

		if (dpcForElement is null)
		{
			return (string.Empty, -1, -1);
		}

		var fl = dpcForElement.GetType().GetProperties();

		if (fl is null)
		{
			return (string.Empty, -1, -1);
		}

		var fileName = fl[0].GetValue(dpcForElement)?.ToString() ?? string.Empty;

		// Don't return details for embedded controls.
		if (fileName.StartsWith("ms-appx:///Uno.UI/", StringComparison.InvariantCultureIgnoreCase)
			|| fileName.EndsWith("mergedstyles.xaml", StringComparison.InvariantCultureIgnoreCase))
		{
			return (string.Empty, -1, -1);
		}

		_ = int.TryParse(fl[1].GetValue(dpcForElement)?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int line);
		_ = int.TryParse(fl[2].GetValue(dpcForElement)?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int pos);

		const string FileTypePrefix = "file:///";

		// Strip any file protocol prefix as not expected by the server
		if (fileName.StartsWith(FileTypePrefix, StringComparison.InvariantCultureIgnoreCase))
		{
			fileName = fileName.Substring(FileTypePrefix.Length);
		}

		return (fileName, line, pos);
	}

	private static string StartEnd(string str, uint chars = 10)
		=> str.Length <= chars ? str : $"{str[..(int)chars]}...{str[^(int)chars..]}";
	#endregion
}
#endif