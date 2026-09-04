using System.Threading.Tasks;

using Windows.UI.Core;

using Microsoft.Testing.Platform.Builder;

using Uno.UI.RuntimeTests;

namespace Uno.UI.RuntimeTests.Engine;

public static class UnoTestApplicationExtensions
{
    public static async Task RunUnoAppAsync(this ITestApplication app, CancellationToken cancellationToken = default)
    {
		int exitCode = 1;
		try
		{
			Action<TestCaseResult>? onResult = null;
			Action<string>? onInProgress = null;

			var window = await WaitForWindowAsync(cancellationToken);

			await window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var engine = new UnitTestsControl();

				onResult = engine.RegisterExternalResult;
				onInProgress = engine.ReportInProgress;

				UnitTestsMSTestReporter.OnTestCaseResult += onResult;
				UnitTestsMSTestReporter.OnTestCaseInProgress += onInProgress;
				window.Content = engine;
			});

			try
			{
				exitCode = await app.RunAsync();
			}
			finally
			{
				UnitTestsMSTestReporter.OnTestCaseResult -= onResult;
				UnitTestsMSTestReporter.OnTestCaseInProgress -= onInProgress;
			}
		}
		catch (Exception e)
		{
			RuntimeTestEmbeddedRunner.LogError("Failed to run MSTest-hosted runtime tests.");
			RuntimeTestEmbeddedRunner.LogError(e.ToString());
			exitCode = 1;
			throw;
		}
		finally
		{
			Environment.ExitCode = exitCode;
			Microsoft.UI.Xaml.Application.Current?.Exit();
		}
    }

	private static async Task<Window> WaitForWindowAsync(CancellationToken ct)
	{
		await Task.Delay(2000, ct);
		await RuntimeTestEmbeddedRunner.WaitForCheckSafe(() => Application.Current is not null, ct: ct, timeout: TimeSpan.FromSeconds(5));
		await RuntimeTestEmbeddedRunner.WaitForIdle(ct);
		await RuntimeTestEmbeddedRunner.WaitForCheckSafe(() => Window.Current?.Dispatcher is not null, ct: ct, timeout: TimeSpan.FromSeconds(30));

		return Window.Current is { Dispatcher: { } } window
			? window
			: throw new InvalidOperationException("Window.Current is null or does not have a valid dispatcher after waiting.");
	}
}
