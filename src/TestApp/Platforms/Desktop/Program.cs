using System;
using System.Linq;
using System.Threading.Tasks;

using Uno.UI.Hosting;

using Microsoft.Testing.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

		Task.WaitAll(RunTests(args), host.RunAsync());
	}

	static async Task RunTests(string[] args)
	{
		if (!args.Any(a => string.Compare("--dotnet-test-pipe", a, StringComparison.OrdinalIgnoreCase) == 0))
		{
			return;
		}

		var testsBuilder = await Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync(args);
		testsBuilder.AddUnoRuntimeTests();
#if USE_UNO_MSTEST_ENGINE
		// MSTest-native runtime-tests engine (opt-in via $(UseMSTest)=true): runs tests through
		// MSTest's own engine instead of the hand-rolled one bridged by AddUnoRuntimeTests(),
		// which uses Reflection to load all `*Tests.dll` assemblies.
		// Explicitly mention the assemblies which contain tests.
		testsBuilder.AddMSTest(() => [typeof(Program).Assembly]);
		testsBuilder.AddTrxReportProvider();
#endif // USE_UNO_MSTEST_ENGINE
		using var testsApp = await testsBuilder.BuildAsync();
		await testsApp.RunUnoAppAsync();
	}
}
