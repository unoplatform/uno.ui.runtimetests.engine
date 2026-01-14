#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#pragma warning disable CA1848 // Log perf
#pragma warning disable CS1998 // No await
#nullable enable

#if !UNO_RUNTIMETESTS_DISABLE_UI
using System;
using System.ComponentModel;
using System.IO;

namespace Uno.UI.RuntimeTests.Internal.Helpers;

/// <summary>
/// Helper class to start a dev server instance.
/// </summary>
/// <remarks>
/// This class is intended to be used only by the the test engine itself and should not be used by applications.
/// API contract is not guaranteed and might change in future releases.
/// </remarks>
[global::System.Runtime.Versioning.SupportedOSPlatform("windows")]
[global::System.Runtime.Versioning.SupportedOSPlatform("linux")]
[global::System.Runtime.Versioning.SupportedOSPlatform("freeBSD")]
public sealed partial class DevServer : global::System.IAsyncDisposable
{
	private static readonly global::Microsoft.Extensions.Logging.ILogger _log = global::Uno.Extensions.LogExtensionPoint.Log(typeof(DevServer));
	private static int _instance;
	private static string? _devServerPath;

	/// <summary>
	/// Sets path to the dev-server host assembly (i.e. the Uno.UI.RemoteControl.Host.dll file).
	/// </summary>
	/// <param name="path">Path for the dev-server host assembly.</param>
	[EditorBrowsable(EditorBrowsableState.Advanced)] // To be used by uno only
	public static void SetDefaultPath(string path)
		=> _devServerPath = path;

	/// <summary>
	/// Starts a new dev server instance
	/// </summary>
	/// <param name="ct">Cancellation token to abort the initialization of the server.</param>
	/// <returns>The new dev server instance.</returns>
	public static async global::System.Threading.Tasks.Task<DevServer> Start(string path, global::System.Threading.CancellationToken ct)
		=> StartCore(path, GetTcpPort());

	/// <summary>
	/// Starts a new dev server instance
	/// </summary>
	/// <param name="ct">Cancellation token to abort the initialization of the server.</param>
	/// <returns>The new dev server instance.</returns>
	/// <remarks>
	/// The path of the dev-server will be resolved from the UNO_RUNTIME_TESTS_DEV_SERVER_PATH environment variable (path to the Uno.UI.RemoteControl.Host.dll file),
	/// and if not defined, it the latest version version will be pulled from NuGet.
	/// </remarks>
	public static async global::System.Threading.Tasks.Task<DevServer> Start(global::System.Threading.CancellationToken ct)
	{
#if !HAS_UNO_DEVSERVER
		throw new global::System.NotSupportedException("Dev server has not been referenced.");
#else
		var path = await GetDevServer(ct);
		var port = GetTcpPort();

		return StartCore(path, port);
#endif
	}

	private readonly global::System.Diagnostics.Process _process;

	private DevServer(global::System.Diagnostics.Process process, int port)
	{
		Port = port;
		_process = process;
	}

	/// <summary>
	/// The port on which the dev server is listening
	/// </summary>
	public int Port { get; }

	private static async global::System.Threading.Tasks.Task<string> GetDevServer(global::System.Threading.CancellationToken ct)
		=> _devServerPath ??= Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_DEV_SERVER_PATH") is { Length: > 0 } path ? path : await PullDevServer(ct);

	/// <summary>
	/// Pulls the latest version of dev server from NuGet and returns the path to the executable
	/// </summary>
	private static async global::System.Threading.Tasks.Task<string> PullDevServer(global::System.Threading.CancellationToken ct)
	{
		var dir = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), $"DevServer_{(global::System.Guid.NewGuid()):N}");
		global::System.IO.Directory.CreateDirectory(dir);

		try
		{
			using (var log = _log.Scope<DevServer>("GET_DOTNET_VERSION"))
			{
				var rawVersion = await ProcessHelper.ExecuteAsync(
					ct,
					"dotnet",
					new() { "--version" },
					global::System.Environment.CurrentDirectory, // Needed to get the version used by the current app (i.e. including global.json)
					log);
				var dotnetVersion = GetDotnetVersion(rawVersion);

				var csProj = @$"<Project Sdk=""Microsoft.NET.Sdk"">
	<PropertyGroup>
		<OutputType>Exe</OutputType>
		<TargetFramework>net{dotnetVersion.Major}.{dotnetVersion.Minor}</TargetFramework>
	</PropertyGroup>
</Project>";
				await global::System.IO.File.WriteAllTextAsync(global::System.IO.Path.Combine(dir, "PullDevServer.csproj"), csProj, ct);
			}

			using (var log = _log.Scope<DevServer>("PULL_DEV_SERVER"))
			{
				var args = new global::System.Collections.Generic.List<string> { "add", "package" };
				args.Add("Uno.WinUI.DevServer");
				// If the assembly is not a debug version it should have a valid version
				// Note: This is the version of the RemoteControl assembly, not the RemoteControl.Host, but they should be in sync (both are part of the DevServer package)
				if (global::System.Type.GetType("Uno.UI.RemoteControl.RemoteControlClient, Uno.UI.RemoteControl", throwOnError: false)?.Assembly is { } devServerAssembly
					&& global::System.Reflection.CustomAttributeExtensions.GetCustomAttribute<global::System.Reflection.AssemblyInformationalVersionAttribute>(devServerAssembly)?.InformationalVersion is { Length: > 0 } runtimeVersion
					&& global::System.Text.RegularExpressions.Regex.Match(runtimeVersion, @"^(?<version>\d+\.\d+\.\d+(-\w+\.\d+))+") is { Success: true } match)
				{
					args.Add("--version");
					args.Add(match.Groups["version"].Value);
				}
				// Otherwise we use the version used to compile the test engine
				else if (global::System.Reflection.CustomAttributeExtensions.GetCustomAttribute<global::Uno.UI.RuntimeTests.Engine.RuntimeTestDevServerAttribute>(typeof(DevServer).Assembly)?.Version is { Length: > 0 } version)
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

			using (var log = _log.Scope<DevServer>("GET_DEV_SERVER_PATH"))
			{
				var data = await ProcessHelper.ExecuteAsync(
					ct,
					"dotnet",
					new() { "build", "/t:GetRemoteControlHostPath" },
					dir,
					log);

				return GetConfigurationValue(data, "RemoteControlHostPath") is { Length: > 0 } path
					? path
					: throw new global::System.InvalidOperationException("Failed to get remote control host path");
			}
		}
		finally
		{
			try
			{
				global::System.IO.Directory.Delete(dir, recursive: true);
			}
			catch { /* Nothing to do */ }
		}
	}

	/// <summary>
	/// Starts the dev server on the given port
	/// </summary>
	private static DevServer StartCore(string hostBinPath, int port)
	{
		if (!global::System.IO.File.Exists(hostBinPath))
		{
			global::Microsoft.Extensions.Logging.LoggerExtensions.LogError(_log, $"DevServer {hostBinPath} does not exist");
			throw new global::System.InvalidOperationException($"Unable to find {hostBinPath}");
		}

		var arguments = $"\"{hostBinPath}\" --httpPort {port} --ppid {(global::System.Environment.ProcessId)} --metadata-updates true";
		var pi = new global::System.Diagnostics.ProcessStartInfo("dotnet", arguments)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = global::System.Diagnostics.ProcessWindowStyle.Hidden,
			WorkingDirectory = global::System.IO.Path.GetDirectoryName(hostBinPath),
		};

		var process = new global::System.Diagnostics.Process { StartInfo = pi };

		process.StartAndLog(_log.Scope<DevServer>($"DEV_SERVER_{(global::System.Threading.Interlocked.Increment(ref _instance)):D2}"));

		return new DevServer(process, port);
	}

	#region Misc helpers
	private static string? GetConfigurationValue(string msbuildResult, string nodeName)
		=> global::System.Text.RegularExpressions.Regex.Match(msbuildResult, $"<{nodeName}>(?<value>.*?)</{nodeName}>") is { Success: true } match
			? match.Groups["value"].Value
			: null;

	private static global::System.Version GetDotnetVersion(string dotnetRawVersion)
		=> dotnetRawVersion?.Split('-') is { } versionParts
			&& global::System.Version.TryParse(global::System.Linq.Enumerable.FirstOrDefault(versionParts), out var version)
				? version
				: throw new global::System.InvalidOperationException("Failed to read dotnet version");

	private static int GetTcpPort()
	{
		var l = new global::System.Net.Sockets.TcpListener(global::System.Net.IPAddress.Loopback, 0);
		l.Start();
		var port = ((global::System.Net.IPEndPoint)l.LocalEndpoint).Port;
		l.Stop();
		return port;
	}
	#endregion

	/// <inheritdoc />
	public async global::System.Threading.Tasks.ValueTask DisposeAsync()
	{
		if (_process is null or { HasExited: true })
		{
			return;
		}

		try
		{
			_process.Kill(true); // Best effort, the app should kill itself anyway
		}
		catch (global::System.Exception e)
		{
			global::Microsoft.Extensions.Logging.LoggerExtensions.LogError(_log, e, "Failed to kill dev server");
		}

		await _process.WaitForExitAsync(global::System.Threading.CancellationToken.None);
	}
}

#endif