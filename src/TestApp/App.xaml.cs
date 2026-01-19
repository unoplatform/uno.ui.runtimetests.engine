using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.RuntimeTests.Engine;

public sealed partial class App : Application
{
	private Window? _window;

	public App()
	{
		this.InitializeComponent();
	}

	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		try
		{
			_window = new Window();

			var rootFrame = _window.Content as Frame;

			if (rootFrame == null)
			{
				rootFrame = new Frame();
				rootFrame.NavigationFailed += OnNavigationFailed;
				_window.Content = rootFrame;
			}

			if (rootFrame.Content == null)
			{
				rootFrame.Navigate(typeof(MainPage), args.Arguments);
			}

			_window.Activate();
		}
		catch(Exception ex)
		{
			Console.WriteLine($"Exception in OnLaunched: {ex}");
			throw;
		}
	}

	void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
	{
		throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
	}

	/// <summary>
	/// Configures global Uno Platform logging
	/// </summary>
	public static void InitializeLogging()
	{
		var factory = LoggerFactory.Create(builder =>
		{
#if __WASM__
			builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__ || __MACCATALYST__
			builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#else
			builder.AddConsole();
#endif

			builder.SetMinimumLevel(LogLevel.Debug);

			builder.AddFilter("Uno", LogLevel.Warning);
			builder.AddFilter("Windows", LogLevel.Warning);
			builder.AddFilter("Microsoft", LogLevel.Warning);

			builder.AddFilter("Uno.UI.RuntimeTests.HotReload", LogLevel.Debug);
			builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Trace);
			builder.AddFilter("Uno.UI.RuntimeTests.Internal.Helpers", LogLevel.Debug);
			builder.AddFilter("Uno.UI.RuntimeTests.HotReloadHelper", LogLevel.Trace);
			builder.AddFilter("Uno.UI.RuntimeTests.UnitTestsControl", LogLevel.Information);
		});

		global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
	}
}
