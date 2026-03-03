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
	}
}
