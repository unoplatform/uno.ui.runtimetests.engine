using System;
using System.Linq;
using System.Threading.Tasks;

using Uno.UI.Hosting;

#if USE_UNO_HOT_TESTING
using Microsoft.Testing.Extensions;
#endif // USE_UNO_HOT_TESTING
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
#if !USE_UNO_HOT_TESTING
		return;
#else
		if (!args.Any(a => string.Compare("--dotnet-test-pipe", a, StringComparison.OrdinalIgnoreCase) == 0))
		{
			return;
		}

		var testsBuilder = await Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync(args);
		testsBuilder.AddUnoHotTesting();

		// MSTest-native runtime-tests engine (opt-in via $(UseMSTest)=true): runs tests through
		// MSTest's own engine instead of the hand-rolled one bridged by AddUnoRuntimeTests(),
		// which uses Reflection to load all `*Tests.dll` assemblies.
		// Explicitly mention the assemblies which contain tests.
		testsBuilder.AddMSTest(() => [typeof(Program).Assembly]);
		testsBuilder.AddTrxReportProvider();
		using var testsApp = await testsBuilder.BuildAsync();
		await testsApp.RunUnoHotTestingAppAsync();
#endif
	}
}
