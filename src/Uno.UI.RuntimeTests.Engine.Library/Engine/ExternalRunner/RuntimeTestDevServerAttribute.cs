#if !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Linq;

namespace Uno.UI.RuntimeTests.Engine;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class RuntimeTestDevServerAttribute : Attribute
{
	// Note: We prefer to capture it at compilation time instead of using reflection,
	//		 so if dev-server assembly has been overriden with invalid version (e.g. for debug purposes),
	//		 we are still able to get the right one.

	/// <summary>
	/// The version of the DevServer package used to compile the test engine.
	/// </summary>
	public string Version { get; }

	public RuntimeTestDevServerAttribute(string version)
	{
		Version = version;
	}
}
#endif