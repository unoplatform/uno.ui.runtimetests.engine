#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER
#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RuntimeTests.Engine;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class RuntimeTestsSourceProjectAttribute : Attribute
{
	// Note: This is somehow a duplicate of the Uno.UI.RemoteControl.ProjectConfigurationAttribute but it allows us to have the attribute
	//		 to be defined on the assembly that is compiling the runtime test engine (instead on the app head only)
	//		 which allows us to use it in HotReloadTestHelper without having any dependency on the app type.

	public string ProjectFullPath { get; }

	public RuntimeTestsSourceProjectAttribute(string projectFullPath)
	{
		ProjectFullPath = projectFullPath;
	}
}

#endif