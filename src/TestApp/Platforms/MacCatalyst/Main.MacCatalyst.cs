using UIKit;

namespace Uno.UI.RuntimeTests.Engine.MacCatalyst;

public class EntryPoint
{
	static void Main(string[] args)
	{
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}

public class AppDelegate : Microsoft.UI.Xaml.ApplicationDelegate
{
	public AppDelegate() : base(() => new App())
	{
	}
}
