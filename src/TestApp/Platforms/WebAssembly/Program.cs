using System;
using System.Linq;
using System.Threading.Tasks;
using Uno.UI.Hosting;
using Uno.UI.RuntimeTests.Engine;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Engine.Wasm;

public sealed class Program
{
    public static async System.Threading.Tasks.Task Main(string[] args)
	{
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        var appRun = host.RunAsync();
        var testRun = RunTests(true, args);
        await Task.WhenAll(appRun, testRun);
	}

	static async Task RunTests(bool runTests, string[] args)
	{
#if !USE_UNO_MSTEST_ENGINE
		return;
#else // USE_UNO_MSTEST_ENGINE
		if (!runTests && !args.Any(a => string.Compare("--dotnet-test-pipe", a, StringComparison.OrdinalIgnoreCase) == 0))
		{
			return;
		}

		var testsBuilder = await Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync(args);
		testsBuilder.AddUnoRuntimeTests();
		// MSTest-native runtime-tests engine (opt-in via $(UseMSTest)=true): runs tests through
		// MSTest's own engine instead of the hand-rolled one bridged by AddUnoRuntimeTests(),
		// which uses Reflection to load all `*Tests.dll` assemblies.
		// Explicitly mention the assemblies which contain tests.
		testsBuilder.AddMSTest(() => [typeof(Program).Assembly]);
		// testsBuilder.AddTrxReportProvider();
		using var testsApp = await testsBuilder.BuildAsync();
		await testsApp.RunUnoAppAsync();
#endif // USE_UNO_MSTEST_ENGINE
	}
}
