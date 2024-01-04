#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY && HAS_UNO_DEVSERVER // Set as soon as the DevServer package is referenced, cf. Uno.UI.RuntimeTests.Engine.targets
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#pragma warning disable CA1848 // Log perf

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.UI.RuntimeTests.Internal.Helpers;
using Uno.UI.RemoteControl; // DevServer
using Uno.UI.RemoteControl.HotReload; // DevServer
using Uno.UI.RemoteControl.HotReload.Messages; // DevServer
using Uno.UI.RemoteControl.HotReload.MetadataUpdater; // DevServer

namespace Uno.UI.RuntimeTests;

partial class HotReloadHelper
{
	static partial void TryUseDevServerFileUpdater()
		=> Use(new DevServerUpdater());

	partial record FileEdit
	{
		public UpdateFile ToMessage()
			=> new UpdateFile
			{
				FilePath = FilePath,
				OldText = OldText,
				NewText = NewText
			};
	}

	private sealed class DevServerUpdater : IFileUpdater
	{
		/// <inheritdoc />
		public async ValueTask EnsureReady(CancellationToken ct)
		{
			_log.LogTrace("Getting dev-server ready ...");

			if (RemoteControlClient.Instance is null)
			{
				throw new InvalidOperationException("Dev server is not available.");
			}

			if (RemoteControlClient.Instance.Processors.OfType<ClientHotReloadProcessor>().FirstOrDefault() is not { } hotReload)
			{
				throw new InvalidOperationException("App is not configured to accept hot-reload.");
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

			var hotReloadReady = hotReload.WaitForWorkspaceLoaded(ct);
			timeout = Task.Delay(WorkspaceTimeout, ct);
			if (await Task.WhenAny(timeout, hotReloadReady) == timeout)
			{
				throw new TimeoutException(
					"Timeout while waiting for hot reload workspace to be loaded. "
					+ "This usually indicates that the dev-server failed to load the solution, you will find more info in dev-server logs "
					+ "(output in main app logs with [DEV_SERVER] prefix for runtime-tests run in secondary app instance).");
			}

			_log.LogTrace("Workspace is ready on dev-server, sending file update request ...");
		}

		/// <inheritdoc />
		public async ValueTask<ReConnect?> Apply(FileEdit edition, CancellationToken ct)
		{
			try
			{
				await RemoteControlClient.Instance!.SendMessage(edition.ToMessage());

				return null;
			}
			catch (WebSocketException)
			{
				return ct =>
				{
					_log.LogWarning($"WAITING {RemoteControlClient.Instance!.ConnectionRetryInterval:g} to let client reconnect before retry**.");
					return Task.Delay(RemoteControlClient.Instance.ConnectionRetryInterval + TimeSpan.FromMilliseconds(100), ct);
				};
			}
		}
	}
}
#endif
