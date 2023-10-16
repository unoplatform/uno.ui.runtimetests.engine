#if !UNO_RUNTIMETESTS_DISABLE_UI
#nullable enable

using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Logging;

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
		string logPrefix,
		Dictionary<string, string>? environmentVariables = null)
	{
		var process = SetupProcess(executable, parameters, workingDirectory, environmentVariables);
		var output = new StringBuilder();
		var error = new StringBuilder();

		typeof(ProcessHelper).Log().Debug(logPrefix + " waiting for process exit");

		// hookup the event handlers to capture the data that is received
		process.OutputDataReceived += (sender, args) => output.Append(args.Data);
		process.ErrorDataReceived += (sender, args) => error.Append(args.Data);

		if (ct.IsCancellationRequested)
		{
			return "";
		}

		var pi = process.StartInfo;
		typeof(ProcessHelper).Log().Debug($"Started process (wd:{pi.WorkingDirectory}): {pi.FileName} {string.Join(" ", pi.ArgumentList)})");

		process.Start();

		// start our event pumps
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		await process.WaitForExitWithCancellationAsync(ct);

		process.EnsureSuccess(logPrefix, error);

		return output.ToString();
	}

	public static async Task ExecuteAndLogAsync(
		this Process process,
		string logPrefix,
		CancellationToken ct)
	{
		process.StartAndLog(logPrefix);

		typeof(ProcessHelper).Log().Debug(logPrefix + " waiting for process exit");

		await process.WaitForExitWithCancellationAsync(ct);

		process.EnsureSuccess(logPrefix);
	}

	public static Process StartAndLog(
		string executable,
		List<string> parameters,
		string workingDirectory,
		string logPrefix,
		Dictionary<string, string>? environmentVariables = null)
		=> SetupProcess(executable, parameters, workingDirectory, environmentVariables).StartAndLog(logPrefix);

	public static Process StartAndLog(
		this Process process,
		string logPrefix)
	{
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;

		// hookup the event handlers to capture the data that is received
		process.OutputDataReceived += (sender, args) => typeof(ProcessHelper).Log().Debug(logPrefix + ": " + args.Data ?? "<Empty>");
		process.ErrorDataReceived += (sender, args) => typeof(ProcessHelper).Log().Error(logPrefix + ": " + args.Data ?? "<Empty>");

		var pi = process.StartInfo;
		typeof(ProcessHelper).Log().Debug($"Started process (wd:{pi.WorkingDirectory}): {pi.FileName} {string.Join(" ", pi.ArgumentList)})");

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
		await process.WaitForExitAsync(ct);
	}

	public static void EnsureSuccess(this Process process, string logPrefix, StringBuilder error)
	{
		if (process.ExitCode != 0)
		{
			var processError = new InvalidOperationException(error.ToString());
			typeof(ProcessHelper).Log().Error(logPrefix + $" Process '{process.StartInfo.FileName}' failed with code {process.ExitCode}", processError);

			throw new InvalidOperationException($"Process '{process.StartInfo.FileName}' failed with code {process.ExitCode}", processError);
		}
	}

	public static void EnsureSuccess(this Process process, string logPrefix)
	{
		if (process.ExitCode != 0)
		{
			typeof(ProcessHelper).Log().Error(logPrefix + $" Process '{process.StartInfo.FileName}' failed with code {process.ExitCode}");

			throw new InvalidOperationException($"Process '{process.StartInfo.FileName}' failed with code {process.ExitCode}");
		}
	}
}
#endif
