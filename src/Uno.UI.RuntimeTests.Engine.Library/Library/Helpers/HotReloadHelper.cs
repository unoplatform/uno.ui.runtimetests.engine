// *********************************************************************************************************************
// *********************************************************************************************************************
//				Be aware that this file is referenced only if the package Uno.[Win]UI.DevServer is referenced.
// *********************************************************************************************************************
// *********************************************************************************************************************

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Uno.UI.RemoteControl; // DevServer
using Uno.UI.RemoteControl.HotReload.Messages; // DevServer
using Uno.UI.RemoteControl.HotReload.MetadataUpdater; // DevServer
using System.IO;
using System.Diagnostics;
using System.Net.WebSockets;
using Uno.UI.RuntimeTests.Engine;


#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml;
#else
using Windows.UI.Xaml;
#endif

namespace Uno.UI.RuntimeTests;

public static partial class HotReloadHelper
{
	public static async ValueTask<IAsyncDisposable?> UpdateServerFile<T>(string filPathRelativeToProject, string originalText, string replacementText, CancellationToken ct)
		=> await UpdateServerFile<T>(filPathRelativeToProject, originalText, replacementText, true, ct);

	/// <summary>
	/// Update the 
	/// </summary>
	/// <typeparam name="T">The type that is expected to be altered</typeparam>
	/// <param name="filPathRelativeToProject"></param>
	/// <param name="originalText"></param>
	/// <param name="replacementText"></param>
	/// <param name="waitForMetadataUpdate"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public static async ValueTask<IAsyncDisposable?> UpdateServerFile<T>(string filPathRelativeToProject, string originalText, string replacementText, bool waitForMetadataUpdate, CancellationToken ct)
	{
		var projectFile = typeof(HotReloadHelper).Assembly.GetCustomAttribute<RuntimeTestsSourceProjectAttribute>()?.ProjectFullPath;
		if (projectFile is null or { Length: 0 })
		{
			throw new InvalidOperationException("The project file path could not be found.");
		}

		if (!File.Exists(projectFile)) // Sanity!
		{
			throw new InvalidOperationException("Unable to find project file.");
		}

		var projectDir = Path.GetDirectoryName(projectFile);
		if (!Directory.Exists(projectDir))
		{
			throw new InvalidOperationException("Unable to find project directory.");
		}

		var message = new UpdateFile
		{
			FilePath = Path.Combine(projectDir, filPathRelativeToProject),
			OldText = originalText,
			NewText = replacementText
		};

		return await UpdateServerFile(message, waitForMetadataUpdate, ct);
	}

	public static async ValueTask<IAsyncDisposable?> UpdateServerFile<T>(string originalText, string replacementText, CancellationToken ct)
		where T : FrameworkElement, new()
	{
		if (RemoteControlClient.Instance is null)
		{
			return default;
		}
		
		var message = new T().CreateUpdateFileMessage(
			originalText: originalText,
			replacementText: replacementText);

		return await UpdateServerFile(message, true, ct);
	}

	public static async Task<IAsyncDisposable?> UpdateServerFile(UpdateFile message, bool waitForMetadataUpdate, CancellationToken ct)
	{
		if (RemoteControlClient.Instance is null)
		{
			return default;
		}
		Console.WriteLine("========== Waiting for connection");

		await RemoteControlClient.Instance.WaitForConnection(ct);

		Console.WriteLine("========== Connected");

		
		var revertMessage = new RevertFileUpdate(RemoteControlClient.Instance, message);

		Console.WriteLine("========== Sending update message for file: " + message.FilePath);

		if (waitForMetadataUpdate)
		{
			var cts = new TaskCompletionSource();
			await using var _ = ct.Register(() => cts.TrySetCanceled());
			void UpdateReceived(object? sender, object? args) => cts.TrySetResult();

			await RemoteControlClient.Instance.SendMessage(message);

			Console.WriteLine("========== Sent message, waiting for update");

			try
			{
				MetadataUpdaterHelper.MetadataUpdated += UpdateReceived;

				var timeout = Task.Delay(TestHelper.DefaultTimeout, ct);
				if (await Task.WhenAny(timeout, cts.Task) == timeout)
				{
					throw new TimeoutException(
						"Timeout while waiting for metadata update. "
						+ "This usually indicates that the dev-server failed to process the requested update, you will find more info in dev-server logs "
						+ "(output in main app logs with [DEV_SERVER] prefix for runtime-tests run in secondary app instance).");
				}
			}
			catch
			{
				await revertMessage.DisposeAsync();

				throw;
			}
			finally
			{
				MetadataUpdaterHelper.MetadataUpdated -= UpdateReceived;
			}

			Console.WriteLine("========== Got updates");
		}
		else
		{
			await RemoteControlClient.Instance.SendMessage(message);
		}

		return revertMessage;
	}

	private record RevertFileUpdate(RemoteControlClient DevServer, UpdateFile Message) : IAsyncDisposable
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
				await DevServer.SendMessage(revertMessage);
			}
			catch (WebSocketException)
			{
				Console.WriteLine("Failed to revert file update, connection to the dev-server might have been closed *** WAITING 5 sec to let client reconnect before retry**.");

				// Wait for the client to attempt re-connection
				await Task.Delay(DevServer.ConnectionRetryInterval + TimeSpan.FromMilliseconds(100));

				try
				{
					await DevServer.SendMessage(revertMessage);
				}
				catch (WebSocketException)
				{
					Console.WriteLine($"Failed to revert file update, connection to the dev-server might have been closed. Cannot revert changes made on file {Message.FilePath}.");
				}
			}
		}
	}

	public static UpdateFile CreateUpdateFileMessage(
		this FrameworkElement element,
		string originalText,
		string replacementText)
	{
		var fileInfo = element.GetDebugParseContext();
		return new UpdateFile
		{
			FilePath = fileInfo.FileName,
			OldText = originalText,
			NewText = replacementText
		};

	}

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
}

#endif