using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
public class TimeoutAttributeTests
{
	[TestMethod]
	public void When_NoTimeoutAttribute_Then_TimeoutIsNull()
	{
		var method = typeof(TimeoutAttributeTests).GetMethod(nameof(When_NoTimeoutAttribute_Then_TimeoutIsNull))!;
		var sut = new UnitTestMethodInfo(method);

		Assert.IsNull(sut.Timeout);
	}

	[TestMethod]
	public void When_TimeoutAttribute_Then_TimeoutHasValue()
	{
		var method = typeof(TimeoutAttributeTests).GetMethod(nameof(Method_WithTimeout), BindingFlags.NonPublic | BindingFlags.Static)!;
		var sut = new UnitTestMethodInfo(method);

		Assert.AreEqual(TimeSpan.FromMilliseconds(5000), sut.Timeout);
	}

	[TestMethod]
	public void When_TimeoutAttributeZero_Then_TimeoutIsNull()
	{
		var method = typeof(TimeoutAttributeTests).GetMethod(nameof(Method_WithZeroTimeout), BindingFlags.NonPublic | BindingFlags.Static)!;
		var sut = new UnitTestMethodInfo(method);

		Assert.IsNull(sut.Timeout);
	}

	[TestMethod]
	[Timeout(10_000)]
	public async Task When_TestWithTimeoutAttribute_And_CompletesInTime_Then_Passes()
	{
		await Task.Delay(50);
	}

	[Timeout(5000)]
	private static void Method_WithTimeout() { }

	[Timeout(0)]
	private static void Method_WithZeroTimeout() { }
}
