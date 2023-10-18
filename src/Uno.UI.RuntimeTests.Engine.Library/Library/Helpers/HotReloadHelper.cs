#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#pragma warning disable CA1848 // Log perf

using System;
using System.Collections;
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

#if HAS_UNO_DEVSERVER // Set as soon as the DevServer package is referenced, cf. Uno.UI.RuntimeTests.Engine.targets
using Uno.UI.RemoteControl; // DevServer
using Uno.UI.RemoteControl.HotReload; // DevServer
using Uno.UI.RemoteControl.HotReload.Messages; // DevServer
using Uno.UI.RemoteControl.HotReload.MetadataUpdater; // DevServer
#else
using Uno.UI.RemoteControl; // Mock, cf. HotReloadHelper.Mocks.cs
using Uno.UI.RemoteControl.HotReload.Messages; // Mock, cf. HotReloadHelper.Mocks.cs

#pragma warning disable CA1823 // Field not used
#endif

namespace Uno.UI.RuntimeTests;

public static partial class HotReloadHelper
{
	public static bool IsSupported => // Note: not as const to avoid "CS0162 unreachable code" warning
#if HAS_UNO_DEVSERVER
		true;
#else
		false;
#endif

	// The delay for the client to connect to the dev-server
	private static TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(3);

	// The delay for the server to load the workspace and let the client know it's ready
	private static TimeSpan WorkspaceTimeout = TimeSpan.FromSeconds(30);

	// The delay for the server to send metadata update once a file has been modified
	private static TimeSpan MetadataUpdateTimeout = TimeSpan.FromSeconds(5);

	private static readonly ILogger _log = typeof(HotReloadHelper).Log();

	/// <summary>
	/// Request on the server to replace all occurrences of <paramref name="originalText"/> by the <paramref name="replacementText"/> in the given file.
	/// </summary>
	/// <param name="filPathRelativeToProject">Path of file in which the replacement should take place, relative to the project which reference the runtime-test engine (usually the application head).</param>
	/// <param name="originalText">The original text to replace.</param>
	/// <param name="replacementText">The replacement text.</param>
	/// <param name="ct">An cancellation token to abort the asynchronous operation.</param>
	/// <returns>An IAsyncDisposable object that will revert the change when disposed.</returns>
	public static async ValueTask<IAsyncDisposable> UpdateServerFile(string filPathRelativeToProject, string originalText, string replacementText, CancellationToken ct = default)
		=> await UpdateServerFile(filPathRelativeToProject, originalText, replacementText, true, ct);

	/// <summary>
	/// Request on the server to replace all occurrences of <paramref name="originalText"/> by the <paramref name="replacementText"/> in the given file.
	/// </summary>
	/// <param name="filPathRelativeToProject">Path of file in which the replacement should take place, relative to the project which reference the runtime-test engine (usually the application head).</param>
	/// <param name="originalText">The original text to replace.</param>
	/// <param name="replacementText">The replacement text.</param>
	/// <param name="waitForMetadataUpdate">Request to wait for a metadata update to consider the file update to be applied.</param>
	/// <param name="ct">An cancellation token to abort the asynchronous operation.</param>
	/// <returns>An IAsyncDisposable object that will revert the change when disposed.</returns>
	public static async ValueTask<IAsyncDisposable> UpdateServerFile(string filPathRelativeToProject, string originalText, string replacementText, bool waitForMetadataUpdate, CancellationToken ct = default)
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

		var message = new UpdateFile
		{
			FilePath = Path.Combine(projectDir, filPathRelativeToProject),
			OldText = originalText,
			NewText = replacementText
		};

		return await UpdateServerFile(message, waitForMetadataUpdate, ct);
	}

	/// <summary>
	/// Request on the server to replace all occurrences of <paramref name="originalText"/> by the <paramref name="replacementText"/> in the source file of the given <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">Type of the <see cref="FrameworkElement"/> for which the source code should be edited.</typeparam>
	/// <param name="originalText">The original text to replace.</param>
	/// <param name="replacementText">The replacement text.</param>
	/// <param name="ct">An cancellation token to abort the asynchronous operation.</param>
	/// <returns>An IAsyncDisposable object that will revert the change when disposed.</returns>
	public static async ValueTask<IAsyncDisposable> UpdateServerFile<T>(string originalText, string replacementText, CancellationToken ct = default)
		where T : FrameworkElement, new()
	{
		if (RemoteControlClient.Instance is null)
		{
			throw new InvalidOperationException("Dev server is not available.");
		}

		var message = new T().CreateUpdateFileMessage(
			originalText: originalText,
			replacementText: replacementText);

		return await UpdateServerFile(message, true, ct);
	}

	/// <summary>
	/// Request on the server to apply teh given file update request.
	/// </summary>
	/// <param name="message">The file update request.</param>
	/// <param name="waitForMetadataUpdate">Request to wait for a metadata update to consider the file update to be applied.</param>
	/// <param name="ct">An cancellation token to abort the asynchronous operation.</param>
	/// <returns>An IAsyncDisposable object that will revert the change when disposed.</returns>
#if !HAS_UNO_DEVSERVER
	public static async Task<IAsyncDisposable> UpdateServerFile(UpdateFile message, bool waitForMetadataUpdate, CancellationToken ct = default)
	{
		_log.LogError("Dev server is not available.");
		throw new InvalidOperationException("Dev server is not available.");
	}
#else
	public static async Task<IAsyncDisposable> UpdateServerFile(UpdateFile message, bool waitForMetadataUpdate, CancellationToken ct = default)
	{
		_log.LogTrace($"Waiting for connection in order to update file {message.FilePath} (from: \"{StartEnd(message.OldText)}\" to \"{StartEnd(message.NewText)}\") ...");

		if (RemoteControlClient.Instance is null)
		{
			throw new InvalidOperationException("Dev server is not available.");
		}

		var timeout = Task.Delay(ConnectionTimeout, ct);
		if (await Task.WhenAny(timeout, RemoteControlClient.Instance.WaitForConnection(ct)) == timeout)
		{
			throw new TimeoutException(
				"Timeout while waiting for the app to connect to the dev-server. "
				+ "This usually indicates that the dev-server is not running on the expected combination of hots and port"
				+ "(For runtime-tests run in secondary app instance, the server should be listening for connection on "
				+ $"{Environment.GetEnvironmentVariable("UNO_DEV_SERVER_HOST")}:{Environment.GetEnvironmentVariable("UNO_DEV_SERVER_PORT")}).");
		}

		_log.LogTrace("Client connected, waiting for dev-server to load the workspace (i.e. initializing roslyn with the solution) ...");

		var processors = typeof(RemoteControlClient).GetField("_processors", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(RemoteControlClient.Instance) as IDictionary ?? throw new InvalidOperationException("Processors is null");
		var processor = processors["hotreload"] as ClientHotReloadProcessor ?? throw new InvalidOperationException("HotReloadProcessor is null");
		var hotReloadReady = typeof(ClientHotReloadProcessor).GetProperty("HotReloadWorkspaceLoaded", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(processor) as Task ?? throw new IOException("HotReloadWorkspaceLoaded is null");

		timeout = Task.Delay(WorkspaceTimeout, ct);
		if (await Task.WhenAny(timeout, hotReloadReady) == timeout)
		{
			throw new TimeoutException(
				"Timeout while waiting for hot reload workspace to be loaded. "
				+ "This usually indicates that the dev-server failed to load the solution, you will find more info in dev-server logs "
				+ "(output in main app logs with [DEV_SERVER] prefix for runtime-tests run in secondary app instance).");
		}

		_log.LogTrace("Workspace is ready on dev-server, sending file update request ...");

		var revertMessage = new RevertFileUpdate(RemoteControlClient.Instance, message, waitForMetadataUpdate);
		await SendMessageCore(message, revertMessage, waitForMetadataUpdate, ct);

		return revertMessage;
	}

	private static async Task SendMessageCore(UpdateFile message, RevertFileUpdate? revertMessage, bool waitForMetadataUpdate, CancellationToken ct)
	{
		if (waitForMetadataUpdate)
		{
			var cts = new TaskCompletionSource();
			await using var _ = ct.Register(() => cts.TrySetCanceled());
			void UpdateReceived(object? sender, object? args) => cts.TrySetResult();

			await RemoteControlClient.Instance!.SendMessage(message);

			_log.LogTrace("File update message sent to the dev-server, waiting for metadata update (i.e. the code changes to be applied in the current app) ...");

			try
			{
				MetadataUpdaterHelper.MetadataUpdated += UpdateReceived;

				var timeout = Task.Delay(MetadataUpdateTimeout, ct); //TestHelper.DefaultTimeout * 45, ct);
				if (await Task.WhenAny(timeout, cts.Task) == timeout)
				{
					throw new TimeoutException(
						"Timeout while waiting for metadata update. "
						+ "This usually indicates that the dev-server failed to process the requested update, you will find more info in dev-server logs "
						+ "(output in main app logs with [DEV_SERVER] prefix for runtime-tests run in secondary app instance).");
				}
			}
			catch when (revertMessage is not null)
			{
				await revertMessage.DisposeAsync();

				throw;
			}
			finally
			{
				MetadataUpdaterHelper.MetadataUpdated -= UpdateReceived;
			}

			_log.LogTrace("Received **a** metadata update, continuing test.");
		}
		else
		{
			await RemoteControlClient.Instance!.SendMessage(message);

			_log.LogTrace("File update message sent to the dev-server, continuing test without waiting for metadata update.");
		}
	}

	private record RevertFileUpdate(RemoteControlClient DevServer, UpdateFile Message, bool WaitForMetadataUpdate) : IAsyncDisposable
	{
		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			var revertMessage = new UpdateFile
			{
				FilePath = Message.FilePath,
				NewText = Message.OldText,
				OldText = Message.NewText
			};

			try
			{
				_log.LogInformation($"Reverting changes made on {Message.FilePath} (from: \"{StartEnd(revertMessage.OldText)}\" to \"{StartEnd(revertMessage.NewText)}\").");

				// Note: We also wait for metadata update here to ensure that the file is reverted before the test continues / we run another test.
				await SendMessageCore(revertMessage, null, WaitForMetadataUpdate, CancellationToken.None);
			}
			catch (WebSocketException)
			{
				_log.LogWarning($"Failed to revert file update, connection to the dev-server might have been closed *** WAITING {DevServer.ConnectionRetryInterval:g} to let client reconnect before retry**.");

				// Wait for the client to attempt re-connection
				await Task.Delay(DevServer.ConnectionRetryInterval + TimeSpan.FromMilliseconds(100));

				try
				{
					// Note: We also wait for metadata update here to ensure that the file is reverted before the test continues / we run another test.
					await SendMessageCore(revertMessage, null, WaitForMetadataUpdate, CancellationToken.None);
				}
				catch (WebSocketException)
				{
					_log.LogError($"Failed to revert file update, connection to the dev-server might have been closed. Cannot revert changes made on file {Message.FilePath}.");
				}
			}
		}
	}
#endif

	/// <summary>
	/// Creates a file update request to replace text in the source file of the given <paramref name="element"/>.
	/// </summary>
	/// <param name="element">An instance of the <see cref="FrameworkElement"/> for which the source code should be edited.</param>
	/// <param name="originalText">The original text to replace.</param>
	/// <param name="replacementText">The replacement text.</param>
	/// <returns>A file update request that can be sent to the dev-server.</returns>
	public static UpdateFile CreateUpdateFileMessage(
		this FrameworkElement element,
		string originalText,
		string replacementText)
		=> new()
		{
			FilePath = element.GetDebugParseContext().FileName,
			OldText = originalText,
			NewText = replacementText
		};

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
}
#endif