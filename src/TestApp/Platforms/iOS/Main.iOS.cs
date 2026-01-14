using Uno.UI.Hosting;

namespace Uno.UI.RuntimeTests.Engine.iOS;

public class EntryPoint
{
	static void Main(string[] args)
	{
		App.InitializeLogging();

		var host = UnoPlatformHostBuilder.Create()
			.App(() => new App())
			.UseAppleUIKit()
			.Build();

		host.Run();
	}
}
