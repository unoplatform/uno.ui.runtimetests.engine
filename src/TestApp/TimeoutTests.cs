using System;
using System.Threading;
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
		[Timeout(200, CooperativeCancellation = true)]
		public async Task When_Timeout_Is_Exceeded_With_Cooperative_Cancellation(CancellationToken cancellationToken)
		{
			// With CooperativeCancellation, the token passed to the test method is cancelled
			// once the timeout elapses, instead of the runner racing and abandoning the test's task.
			await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await Task.Delay(10_000, cancellationToken));
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
