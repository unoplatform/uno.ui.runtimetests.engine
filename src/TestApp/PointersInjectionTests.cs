using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
[RunsOnUIThread]
public class PointersInjectionTests
{
	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
	[InjectedPointer(PointerDeviceType.Touch)]
#if !HAS_UNO_SKIA && !WINDOWS
	[ExpectedException(typeof(NotSupportedException))]
#endif
	public async Task When_TapCoordinates()
	{
		var elt = new Button { Content = "Tap me" };
		var clicked = false;
		elt.Click += (snd, e) => clicked = true;

		UnitTestsUIContentHelper.Content = elt;

		await UnitTestsUIContentHelper.WaitForLoaded(elt);

		InputInjectorHelper.Current.Tap(elt);

		Assert.IsTrue(clicked);
	}
}