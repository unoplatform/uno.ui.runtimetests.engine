#if !UNO_RUNTIMETESTS_DISABLE_UI
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if __SKIA__
#define HAS_UNO_DEV_SERVER
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarfBuzzSharp;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.RuntimeTests.Engine;

namespace Uno.UI.RuntimeTests.Internal.Helpers;

/// <summary>
/// Helper class to start a dev server instance.
/// </summary>
/// <remarks>
/// This class is intended to be used only by the the test engine itself and should not be used by applications.
/// API contract is not guaranteed and might change in future releases.
/// </remarks>
internal sealed partial class DevServer : IAsyncDisposable
{
	private static readonly ILogger _log = typeof(DevServer).Log();
	private static int _instance;
	private static string? _devServerPath;

	/// <summary>
	/// Starts a new dev server instance
	/// </summary>
	/// <param name="ct">Cancellation token to abort the initialization of the server.</param>
	/// <returns>The new dev server instance.</returns>
	public static async Task<DevServer> Start(CancellationToken ct)
	{
#if !HAS_UNO_DEV_SERVER
		throw new NotSupportedException("Dev server is not supported on this platform.");
#else
		var path = await GetDevServer(ct);
		var port = GetTcpPort();

		return StartCore(path, port);
#endif
	}

	private readonly Process _process;

	private DevServer(Process process, int port)
	{
		Port = port;
		_process = process;
	}

	/// <summary>
	/// The port on which the dev server is listening
	/// </summary>
	public int Port { get; }

	private static async Task<string> GetDevServer(CancellationToken ct)
		=> _devServerPath ??= await PullDevServer(ct);

	/// <summary>
	/// Pulls the latest version of dev server from NuGet and returns the path to the executable
	/// </summary>
	private static async Task<string> PullDevServer(CancellationToken ct)
	{
		var dir = Path.Combine(Path.GetTempPath(), $"DevServer_{Guid.NewGuid():N}");
		Directory.CreateDirectory(dir);

		try
		{
			using (var log = _log.Scope("GET_DOTNET_VERSION"))
			{
				var rawVersion = await ProcessHelper.ExecuteAsync(
					ct,
					"dotnet",
					new() { "--version" },
					Environment.CurrentDirectory, // Needed to get the version used by the current app (i.e. including global.json)
					log);
				var dotnetVersion = GetDotnetVersion(rawVersion);

				var csProj = @$"<Project Sdk=""Microsoft.NET.Sdk"">
	<PropertyGroup>
		<OutputType>Exe</OutputType>
		<TargetFramework>net{dotnetVersion.Major}.{dotnetVersion.Minor}</TargetFramework>
	</PropertyGroup>
</Project>";
				await File.WriteAllTextAsync(Path.Combine(dir, "PullDevServer.csproj"), csProj, ct);
			}

			using (var log = _log.Scope("PULL_DEV_SERVER"))
			{
				var args = new List<string> { "add", "package" };
#if HAS_UNO_WINUI || WINDOWS_WINUI
				args.Add("Uno.WinUI.DevServer");
#else
				args.Add("Uno.UI.DevServer");
#endif
				// If the assembly is not a debug version it should have a valid version
				// Note: This is the version of the RemoteControl assembly, not the RemoteControl.Host, but they should be in sync (both are part of the DevServer package)
				if (Type.GetType("Uno.UI.RemoteControl.RemoteControlClient, Uno.UI.RemoteControl", throwOnError: false)
					?.Assembly
					.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
					?.InformationalVersion is { Length: > 0 } runtimeVersion
					&& Regex.Match(runtimeVersion, @"^(?<version>\d+\.\d+\.\d+(-\w+\.\d+))+") is {Success: true} match)
				{
					args.Add("--version");
					args.Add(match.Groups["version"].Value);
				}
				// Otherwise we use the version used to compile the test engine
				else if (typeof(DevServer).Assembly.GetCustomAttribute<RuntimeTestDevServerAttribute>()?.Version is { Length: > 0 } version)
				{
					args.Add("--version");
					args.Add(version);
				}
				// As a last chance, we just use the latest version
				else
				{
					args.Add("--prerelease"); // latest version
				}

				await ProcessHelper.ExecuteAsync(ct, "dotnet", args, dir, log);
			}

			using (var log = _log.Scope("GET_DEV_SERVER_PATH"))
			{
				var data = await ProcessHelper.ExecuteAsync(
					ct,
					"dotnet",
					new() { "build", "/t:GetRemoteControlHostPath" },
					dir,
					log);

				return GetConfigurationValue(data, "RemoteControlHostPath") is { Length: > 0 } path 
					? path 
					: throw new InvalidOperationException("Failed to get remote control host path");
			}
		}
		finally
		{
			try
			{
				Directory.Delete(dir, recursive: true);
			}
			catch { /* Nothing to do */ }
		}
	}

	/// <summary>
	/// Starts the dev server on the given port
	/// </summary>
	private static DevServer StartCore(string hostBinPath, int port)
	{
		if (!File.Exists(hostBinPath))
		{
			_log.Error($"DevServer {hostBinPath} does not exist");
			throw new InvalidOperationException($"Unable to find {hostBinPath}");
		}

		var arguments = $"\"{hostBinPath}\" --httpPort {port} --ppid {Environment.ProcessId} --metadata-updates true";
		var pi = new ProcessStartInfo("dotnet", arguments)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden,
			WorkingDirectory = Path.GetDirectoryName(hostBinPath),
		};

		var process = new System.Diagnostics.Process { StartInfo = pi };

		process.StartAndLog(_log.Scope($"DEV_SERVER_{Interlocked.Increment(ref _instance):D2}"));

		return new DevServer(process, port);
	}

	#region Misc helpers
	private static string? GetConfigurationValue(string msbuildResult, string nodeName)
		=> Regex.Match(msbuildResult, $"<{nodeName}>(?<value>.*?)</{nodeName}>") is { Success: true } match
			? match.Groups["value"].Value
			: null;

	private static Version GetDotnetVersion(string dotnetRawVersion)
		=> Version.TryParse(dotnetRawVersion?.Split('-').FirstOrDefault(), out var version)
			? version
			: throw new InvalidOperationException("Failed to read dotnet version");

	private static int GetTcpPort()
	{
		var l = new TcpListener(IPAddress.Loopback, 0);
		l.Start();
		var port = ((IPEndPoint)l.LocalEndpoint).Port;
		l.Stop();
		return port;
	}
	#endregion

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_process is null or { HasExited: true })
		{
			return;
		}

		try
		{
			_process.Kill(true); // Best effort, the app should kill itself anyway
		}
		catch (Exception e)
		{
			typeof(DevServer).Log().Error("Failed to kill dev server", e);
		}

		await _process.WaitForExitAsync(CancellationToken.None);
	}
}

#endif