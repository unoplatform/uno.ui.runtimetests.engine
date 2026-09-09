#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

		TestContextProperty = type?.GetProperty("TestContext") is { SetMethod: not null } testContextProp &&
			// string comparison?! It's what testfx does!
			// https://github.com/microsoft/testfx/blob/3986221f7d14db927ac1c5975aeccad0dc35fe17/src/Adapter/MSTestAdapter.PlatformServices/Execution/TestClassInfo.TestContext.cs#L34-L39
			string.Equals(testContextProp.PropertyType.FullName, typeof(TestContext).FullName, StringComparison.Ordinal)
			? testContextProp
			: null;
	}

	public string TestClassName { get; }

	public Type? Type { get; }

	public UnitTestMethodInfo[] Tests { get; }

	public MethodInfo? Initialize { get; }

	public MethodInfo? Cleanup { get; }

	public RunsInSecondaryAppAttribute? RunsInSecondaryApp { get; }

	/// <summary>
	/// The test class' settable <c>TestContext</c> property (if any), cached so it doesn't need
	/// to be resolved via reflection for every test invocation.
	/// </summary>
	public PropertyInfo? TestContextProperty { get; }

	public override string ToString() => TestClassName;

	private static bool HasCustomAttribute<T>(MemberInfo? testMethod)
		=> testMethod?.GetCustomAttribute(typeof(T)) != null;
}

#endif