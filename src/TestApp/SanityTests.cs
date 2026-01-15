using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Engine
{
	[TestClass]
	public class SanityTests
	{
		[TestMethod]
		public void Is_Sane()
		{
		}

		[TestMethod]
		public async Task Is_Still_Sane()
		{
			await Task.Delay(2000);
		}

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
		public void Is_Sane_With_Cases(string text)
		{
#pragma warning disable CA1861 // Prefer static readonly
			Assert.IsTrue(new[] { "hello", "goodbye" }.Contains(text));
		}

		[TestMethod]
		[DynamicData(nameof(DynamicData), DynamicDataSourceType.Property)]
		[DynamicData(nameof(GetDynamicData), DynamicDataSourceType.Method)]
		public void Is_Sane_With_DynamicData(string text)
		{
			Assert.IsTrue(new[] { "hello", "goodbye" }.Contains(text));
		}

		public static IEnumerable<object[]> DynamicData { get; } = new[]
		{
			new object[] { "hello" },
			new object[] { "goodbye" },
		};

		public static IEnumerable<object[]> GetDynamicData()
		{
			yield return new object[] { "hello" };
			yield return new object[] { "goodbye" };
		}

#if DEBUG
		[TestMethod]
		public async Task No_Longer_Sane() // expected to fail
		{
			await Task.Delay(2000);

			throw new Exception("Great works require a touch of insanity.");
		}

		[TestMethod, Ignore]
		public void Is_An_Illusion() // expected to be ignored
		{
		}
#endif
	}
}
