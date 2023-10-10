#if !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RuntimeTests.Engine;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class RuntimeTestsSourceProjectAttribute : Attribute
{
	public string ProjectFullPath { get; }

	public RuntimeTestsSourceProjectAttribute(string projectFullPath)
	{
		ProjectFullPath = projectFullPath;
	}
}
#endif