using System;
using System.Linq;
using System.Windows;
using Uno.UI.Runtime.Skia.Wpf;

namespace Uno.UI.RuntimeTests.Engine.WPF;

public partial class App : Application
{
	public App()
	{
		new WpfHost(Dispatcher, () => new Uno.UI.RuntimeTests.Engine.App()).Run();
	}
}