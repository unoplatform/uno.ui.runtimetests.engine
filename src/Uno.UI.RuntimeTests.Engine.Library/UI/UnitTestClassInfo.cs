#nullable enable

using System;
using System.Linq;
using System.Reflection;

namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_UI

public class UnitTestClassInfo
{
	public UnitTestClassInfo(
		Type type,
		MethodInfo[] tests,
		MethodInfo? initialize,
		MethodInfo? cleanup)
	{
		Type = type;
		Tests = tests;
		Initialize = initialize;
		Cleanup = cleanup;
	}

	public Type Type { get; }

	public MethodInfo[] Tests { get; }

	public MethodInfo? Initialize { get; }

	public MethodInfo? Cleanup { get; }

	public override string ToString() => Type.Name;
}

#endif