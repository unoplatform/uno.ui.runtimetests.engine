using Uno.UI.Hosting;

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

        await host.RunAsync();
	}
}
