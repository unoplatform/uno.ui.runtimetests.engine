
#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml.Controls;
#else
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Engine
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class HotReloadTests_Subject : Page
	{
		public HotReloadTests_Subject()
		{
			this.InitializeComponent();
		}
	}
}
