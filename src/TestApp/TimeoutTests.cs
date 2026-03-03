using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Engine
{
	[TestClass]
	public class TimeoutTests
	{
		[TestMethod]
		[Timeout(5000)]
		public async Task When_Timeout_Is_Not_Exceeded()
		{
			await Task.Delay(100);
		}

		[TestMethod]
		[Timeout(200)]
		[ExpectedException(typeof(TimeoutException))]
		public async Task When_Timeout_Is_Exceeded()
		{
			await Task.Delay(10_000);
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Timeout_Is_Higher_Than_Default()
		{
			// This test validates that the [Timeout] attribute properly overrides
			// the DefaultUnitTestTimeout (60s in Release). Without the fix, this
			// test would fail at 60s with a TimeoutException.
			await Task.Delay(TimeSpan.FromSeconds(65));
		}
	}
}
