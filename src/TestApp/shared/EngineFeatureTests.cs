using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Engine
{
	/// <summary>
	/// Contains tests relevant to the RTT engine features.
	/// </summary>
	[TestClass]
	public class MetaTests
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Test_ContentHelper()
		{
			var SUT = new TextBlock() { Text = "Hello" };
			UnitTestsUIContentHelper.Content = SUT;

			await UnitTestsUIContentHelper.WaitForIdle();
			await UnitTestsUIContentHelper.WaitForLoaded(SUT);
		}

		[TestMethod]
		[DataRow("hello", DisplayName = "hello test")]
		[DataRow("goodbye", DisplayName = "goodbye test")]
		public void When_DisplayName(string text)
		{
		}

		[TestMethod]
		[DataRow("at index 0")]
		[DataRow("at index 1")]
		[DataRow("at index 2")]
		public void When_DataRows(string arg)
		{
		}

		[TestMethod]
		[DataRow("at index 0", "asd")]
		[DataRow("at index 1", "asd")]
		[DataRow("at index 2", "zxc")]
		public void When_DataRows2(string arg, string arg2)
		{
		}

		[DataTestMethod]
		[DynamicData(nameof(GetDynamicData), DynamicDataSourceType.Method)]
		public void When_DynamicData(string arg, string arg2)
		{
		}

		[TestMethod]
		[InjectedPointer(Windows.Devices.Input.PointerDeviceType.Touch)]
		[InjectedPointer(Windows.Devices.Input.PointerDeviceType.Pen)]
		public void When_InjectedPointers()
		{
		}

		[TestMethod]
		[DataRow("at index 0")]
		[DataRow("at index 1")]
		[InjectedPointer(Windows.Devices.Input.PointerDeviceType.Touch)]
		[InjectedPointer(Windows.Devices.Input.PointerDeviceType.Pen)]
		public void When_InjectedPointers_DataRows(string arg)
		{
		}

		public static IEnumerable<object[]> GetDynamicData()
		{
			yield return new object[] { "at index 0", "asd" };
			yield return new object[] { "at index 1", "asd" };
			yield return new object[] { "at index 2", "zxc" };
		}
	}
}
