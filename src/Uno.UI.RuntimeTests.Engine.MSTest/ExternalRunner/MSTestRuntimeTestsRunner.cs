#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

// This bridge references Microsoft.Testing.Platform types directly, so it can only compile where
// those packages are actually referenced (i.e. when $(UseMSTest)=true, see TestApp.csproj).
//#if USE_UNO_MSTEST_ENGINE
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Engine;

/// <summary>
/// Opt-in bridge that runs MSTest's real engine inside a Microsoft.Testing.Platform (MTP)
/// <c>TestApplication</c>, so it can be discovered/run via <c>dotnet test</c> or an MTP-aware IDE
/// Test Explorer. This is the MSTest-native counterpart of the (hand-rolled-engine, superseded for
/// this opt-in) <c>MicrosoftTestingPlatformRunner</c> -- see <c>specs/003-mtp-integration</c>
/// for the original design this mirrors.
/// </summary>
/// <remarks>
/// This class is intended to be used only by the test engine itself and should not be used by applications.
/// API contract is not guaranteed and might change in future releases.
/// </remarks>
public static class MSTestRuntimeTestsRunner
{
#if false
	/// <summary>
	/// Builds and runs an MTP <c>TestApplication</c> hosting MSTest's real engine, then hard-exits the
	/// process with the resulting exit code (mirrors <c>RuntimeTestEmbeddedRunner.ExitApplication</c>).
	/// </summary>
	/// <remarks>
	/// Must be started concurrently with -- not instead of -- the app's normal native host loop
	/// (e.g. Uno's <c>host.Run()</c>), since that loop blocks the calling thread. Call this from a
	/// fire-and-forget background task before calling <c>host.Run()</c>.
	/// </remarks>
	public static async Task RunAsync(string[] args)
	{
		var exitCode = 1;
		try
		{
			var window = await WaitForWindowAsync(CancellationToken.None);

			// UnitTestsControl's constructor touches dependency properties, which -- like the rest of
			// the XAML tree -- must happen on the UI thread. Set it as the window content too, so a
			// `dotnet test` run also shows live progress if the window happens to be visible (headless
			// CI just won't render it).
			var engine = await CreateEngineOnDispatcherAsync(window);

			exitCode = await UnitTestsControl.RunMSTestApplicationAsync(
				args,
				UnitTestsControl.GetCandidateTestAssemblies(),
				onResult: engine.RegisterExternalResult,
				onInProgress: engine.ReportInProgress);
		}
		catch (Exception error)
		{
			RuntimeTestEmbeddedRunner.LogError("Failed to run MSTest-hosted runtime tests.");
			RuntimeTestEmbeddedRunner.LogError(error.ToString());
			exitCode = 1;
		}
		finally
		{
			Application.Current?.Exit();
			RuntimeTestEmbeddedRunner.ExitApplication(exitCode);
		}
	}
#endif

	/// <summary>
	/// Waits for the app's window/dispatcher to become available, mirroring
	/// <see cref="RuntimeTestEmbeddedRunner"/>'s own startup-readiness sequence.
	/// </summary>
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

	/// <summary>
	/// Constructs <see cref="UnitTestsControl"/> and sets it as the window content on the UI thread --
	/// its constructor (via the generated <c>InitializeComponent</c>) touches dependency properties,
	/// which, like the rest of the XAML tree, may only be accessed from the UI thread.
	/// </summary>
	private static async Task<UnitTestsControl> CreateEngineOnDispatcherAsync(Window window)
	{
		var tcs = new TaskCompletionSource<UnitTestsControl>();

		await window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			try
			{
				var engine = new UnitTestsControl();
				window.Content = engine;
				tcs.TrySetResult(engine);
			}
			catch (Exception error)
			{
				tcs.TrySetException(error);
			}
		});

		return await tcs.Task.ConfigureAwait(false);
	}
}
