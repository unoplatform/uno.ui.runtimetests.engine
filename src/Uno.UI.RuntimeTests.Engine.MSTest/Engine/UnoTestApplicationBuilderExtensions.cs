using Microsoft.Testing.Platform.Builder;

namespace Uno.UI.RuntimeTests.Engine;

public static class UnoTestApplicationBuilderExtensions
{

	public static ITestApplicationBuilder AddUnoRuntimeTests(this ITestApplicationBuilder builder)
	{
		builder.TestHost.AddDataConsumer(_ => new UnitTestsMSTestReporter());
		return builder;
	}
}
