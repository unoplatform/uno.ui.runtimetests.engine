using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl;

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
[RunsInSecondaryApp]
public class SecondaryAppTests
{
	[TestMethod]
	public void Is_From_A_Secondary_App()
	{
		Assert.IsTrue(Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_IS_SECONDARY_APP") is "true");
	}

	[TestMethod]
	public async Task Is_DevServer_Connected()
	{
		Assert.IsNotNull(RemoteControlClient.Instance);
		
		var connected = RemoteControlClient.Instance.WaitForConnection(CancellationToken.None);
		var timeout = Task.Delay(500);

		Assert.AreEqual(connected, await Task.WhenAny(connected, timeout));
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

[TestClass]
public class NotSecondaryAppTests
{
	[TestMethod]
	public void Is_From_A_Secondary_App()
	{
		Assert.IsFalse(Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_IS_SECONDARY_APP") is "true");
	}
}