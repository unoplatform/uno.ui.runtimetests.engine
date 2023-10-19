using System;
using System.Linq;
using System.Windows;
using Uno.UI.RemoteControl;
using Uno.UI.Runtime.Skia.Wpf;

namespace Uno.UI.RuntimeTests.Engine.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public App()
	{
		var host = new WpfHost(Dispatcher, () => new Uno.UI.RuntimeTests.Engine.App());
		host.Run();
	}
}