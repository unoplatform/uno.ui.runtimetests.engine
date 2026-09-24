using Microsoft.Testing.Platform.Builder;

namespace Uno.HotTesting;

public static class UnoTestApplicationBuilderExtensions
{

	public static ITestApplicationBuilder AddUnoHotTesting(this ITestApplicationBuilder builder)
	{
		builder.TestHost.AddDataConsumer(_ => new UI.UnitTestsMSTestReporter());
		return builder;
	}
}
