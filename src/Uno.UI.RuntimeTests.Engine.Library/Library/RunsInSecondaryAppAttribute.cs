#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;

namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY && !UNO_RUNTIMETESTS_DISABLE_RUNSINSECONDARYAPPATTRIBUTE
/// <summary>
/// Indicates that the test should be run in a separated app.
/// </summary>
/// <remarks>
/// As starting an external app is a costly operation, this attribute can be set only at the test class level.
/// All tests of the class will be run in the same secondary app instance.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RunsInSecondaryAppAttribute : Attribute
{
	/// <summary>
	/// Indicates if the test should be ignored if the platform does not support running tests in a secondary app.
	/// </summary>
	public bool IgnoreIfNotSupported { get; }

	public RunsInSecondaryAppAttribute(bool ignoreIfNotSupported = false)
	{
		IgnoreIfNotSupported = ignoreIfNotSupported;
	}
}
#endif