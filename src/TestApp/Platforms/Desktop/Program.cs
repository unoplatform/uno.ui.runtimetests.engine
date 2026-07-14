using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Testing.Extensions;
using Uno.UI.Hosting;

namespace Uno.UI.RuntimeTests.Engine.Desktop;

public class Program
{
	[STAThread]
	public static async Task Main(string[] args)
	{
		App.InitializeLogging();

		var host = UnoPlatformHostBuilder.Create()
			.App(() => new App())
			.UseX11()
			.UseLinuxFrameBuffer()
			.UseMacOS()
			.UseWin32()
			.Build();

		var tests = Task.Run(async () =>
		{
			if (!args.Any(a => string.Compare("--dotnet-test-pipe", a, StringComparison.OrdinalIgnoreCase) == 0))
			{
				return;
			}
			var testsBuilder = await Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync(args);
			testsBuilder.AddUnoRuntimeTests();
			testsBuilder.AddTrxReportProvider();
			using var testsApp = await testsBuilder.BuildAsync();
			try
			{
				Environment.ExitCode = await testsApp.RunAsync();
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to run MTP-hosted runtime tests: {e}");
				Environment.ExitCode = 1;
				throw;
			}
			finally
			{
				Microsoft.UI.Xaml.Application.Current?.Exit();
			}
		});

		Task.WaitAll(tests, host.RunAsync());
	}

	static Task StartTestRunnerAsync(string[] args)
	{
		// Opt-in Microsoft.Testing.Platform support (see specs/003-mtp-integration/spec.md).
		// Only engage when launched with CLI args -- e.g. by `dotnet test` (which passes
		// `--server dotnettestcli ...`) or directly with MTP switches like `--list-tests`.
		// A plain launch (double-click, `dotnet TestApp.dll` with no args, or the existing
		// UNO_RUNTIME_TESTS_* env-var flow) always has zero args, so this never hijacks it.
		if (args.Any(a => string.Compare("--dotnet-test-pipe", a, StringComparison.OrdinalIgnoreCase) == 0))
		{
#if HAS_UNO_RUNTIMETESTS_MTP
			return Uno.UI.RuntimeTests.Engine.MicrosoftTestingPlatformRunner.RunAsync(args);
#else
			return Task.CompletedTask;
#endif
		}
		else
		{
			return Task.CompletedTask;
		}
	}

}
