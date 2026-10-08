using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Engine;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();
#if USE_UNO_HOT_TESTING
		this.unitTestsControl.UnitTestAssemblies = [typeof(MainPage).Assembly];
#endif // USE_UNO_HOT_TESTING
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
