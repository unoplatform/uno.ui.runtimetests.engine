using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Engine;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();
	}
}

public partial class UnitTestsControl
#if USE_UNO_HOT_TESTING
	: Uno.HotTesting.UI.UnitTestsControl
#else
	: Uno.UI.RuntimeTests.UnitTestsControl
#endif
{
}
