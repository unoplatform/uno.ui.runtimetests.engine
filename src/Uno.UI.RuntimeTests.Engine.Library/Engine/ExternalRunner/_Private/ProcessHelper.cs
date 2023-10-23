#if !UNO_RUNTIMETESTS_DISABLE_UI && (__SKIA__ || IS_SECONDARY_APP_SUPPORTED)
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#pragma warning disable CA1848 // Log perf

using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Uno.UI.RuntimeTests.Internal.Helpers;

/// <summary>
/// An helper class used by test engine tu run tests in a separate process.
/// </summary>
/// <remarks>
/// This class is intended to be used only by the the test engine itself and should not be used by applications.
/// API contract is not guaranteed and might change in future releases.
/// </remarks>
internal static partial class ProcessHelper
{
	public static async Task<string> ExecuteAsync(
		CancellationToken ct,
		string executable,
		List<string> parameters,
		string workingDirectory,
		ILogger log,
		Dictionary<string, string>? environmentVariables = null)
	{
		var process = SetupProcess(executable, parameters, workingDirectory, environmentVariables);
		var output = new StringBuilder();
		var error = new StringBuilder();

		log.LogTrace("Waiting for process exit");

		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;

		// hookup the event handlers to capture the data that is received
		process.OutputDataReceived += (sender, args) => output.AppendLine(args.Data);
		process.ErrorDataReceived += (sender, args) => error.AppendLine(args.Data);

		if (ct.IsCancellationRequested)
		{
			return "";
		}

		var pi = process.StartInfo;
		log.LogDebug($"Started process (wd:{pi.WorkingDirectory}): {pi.FileName} {string.Join(" ", pi.ArgumentList)})");

		process.Start();

		// start our event pumps
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		await process.WaitForExitWithCancellationAsync(ct);

		process.EnsureSuccess(log, error);

		return output.ToString();
	}

	public static async Task ExecuteAndLogAsync(
		this Process process,
		ILogger log,
		CancellationToken ct)
	{
		process.StartAndLog(log);

		log.LogTrace("Waiting for process exit");

		await process.WaitForExitWithCancellationAsync(ct);

		process.EnsureSuccess(log);
	}

	public static Process StartAndLog(
		string executable,
		List<string> parameters,
		string workingDirectory,
		ILogger log,
		Dictionary<string, string>? environmentVariables = null)
		=> SetupProcess(executable, parameters, workingDirectory, environmentVariables).StartAndLog(log);

	public static Process StartAndLog(
		this Process process,
		ILogger log)
	{
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;

		// hookup the event handlers to capture the data that is received
		process.OutputDataReceived += (sender, args) => log.LogDebug(args.Data ?? "<Empty>");
		process.ErrorDataReceived += (sender, args) => log.LogError(args.Data ?? "<Empty>");

		var pi = process.StartInfo;
		log.LogDebug($"Started process (wd:{pi.WorkingDirectory}): {pi.FileName} {string.Join(" ", pi.ArgumentList)})");

		process.Start();

		// start our event pumps
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		return process;
	}

	private static Process SetupProcess(
		string executable,
		List<string> parameters,
		string workingDirectory,
		Dictionary<string, string>? environmentVariables = null)
	{
		var pi = new ProcessStartInfo(executable)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden,
			WorkingDirectory = workingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};

		foreach (var param in parameters)
		{
			pi.ArgumentList.Add(param);
		}

		if (environmentVariables is not null)
		{
			foreach (var env in environmentVariables)
			{
				pi.EnvironmentVariables[env.Key] = env.Value;
			}
		}

		var process = new System.Diagnostics.Process
		{
			StartInfo = pi
		};

		return process;
	}

	public static async Task WaitForExitWithCancellationAsync(this Process process, CancellationToken ct)
	{
		await using var cancel = ct.Register(process.Close);
		await process.WaitForExitAsync(CancellationToken.None); // If the ct has been cancelled, we want to wait for exit!
	}

	public static void EnsureSuccess(this Process process, ILogger log, StringBuilder error)
	{
		if (process.ExitCode != 0)
		{
			var processError = new InvalidOperationException(error.ToString());
			log.LogError($"Process '{process.StartInfo.FileName}' failed with code {process.ExitCode}", processError);

			throw new InvalidOperationException($"Process '{process.StartInfo.FileName}' failed with code {process.ExitCode}", processError);
		}
	}

	public static void EnsureSuccess(this Process process, ILogger log)
	{
		if (process.ExitCode != 0)
		{
			log.LogError($"Process '{process.StartInfo.FileName}' failed with code {process.ExitCode}");

			throw new InvalidOperationException($"Process '{process.StartInfo.FileName}' failed with code {process.ExitCode}");
		}
	}
}
#endif
