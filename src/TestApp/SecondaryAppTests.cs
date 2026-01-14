#pragma warning disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Internal.Helpers;

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
public class SecondaryAppSanity
{
	[TestMethod]
	public void Is_SecondaryApp_Supported()
	{
#if __SKIA__
		Assert.IsTrue(SecondaryApp.IsSupported);
#else
		Assert.IsFalse(SecondaryApp.IsSupported);
#endif
	}
}

[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
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
#if HAS_UNO_DEVSERVER
		Assert.IsNotNull(Uno.UI.RemoteControl.RemoteControlClient.Instance);
		
		var connected = Uno.UI.RemoteControl.RemoteControlClient.Instance.WaitForConnection(CancellationToken.None);
		var timeout = Task.Delay(500);

		Assert.AreEqual(connected, await Task.WhenAny(connected, timeout));
#else
		Assert.Inconclusive("Dev server in not supported on this platform.");
#endif
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