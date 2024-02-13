#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

using System;
using System.Linq;
using System.Reflection;

namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_UI

public class UnitTestClassInfo
{
	public UnitTestClassInfo(
		Type? type,
		MethodInfo[]? tests,
		MethodInfo? initialize,
		MethodInfo? cleanup)
	{
		Type = type;
		TestClassName = Type?.Name ?? "(null)";
		Tests = tests?.Select(test => new UnitTestMethodInfo(test)).ToArray() ?? Array.Empty<UnitTestMethodInfo>();
		Initialize = initialize;
		Cleanup = cleanup;

		RunsInSecondaryApp = type?.GetCustomAttribute<RunsInSecondaryAppAttribute>();
	}

	public string TestClassName { get; }

	public Type? Type { get; }

	public UnitTestMethodInfo[] Tests { get; }

	public MethodInfo? Initialize { get; }

	public MethodInfo? Cleanup { get; }

	public RunsInSecondaryAppAttribute? RunsInSecondaryApp { get; }

	public override string ToString() => TestClassName;

	private static bool HasCustomAttribute<T>(MemberInfo? testMethod)
		=> testMethod?.GetCustomAttribute(typeof(T)) != null;
}

#endif