#if !UNO_RUNTIMETESTS_DISABLE_UI

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.Devices.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests;

internal record UnitTestMethodInfo
{
	public UnitTestMethodInfo(object testClassInstance, MethodInfo method)
	{
		Method = method;
		RunsOnUIThread =
			HasCustomAttribute<RunsOnUIThreadAttribute>(method) ||
			HasCustomAttribute<RunsOnUIThreadAttribute>(method.DeclaringType);
		RequiresFullWindow =
			HasCustomAttribute<RequiresFullWindowAttribute>(method) ||
			HasCustomAttribute<RequiresFullWindowAttribute>(method.DeclaringType);
		ExpectedException = method
			.GetCustomAttributes<ExpectedExceptionAttribute>()
			.SingleOrDefault()
			?.ExceptionType;

		DataSources  = method
			.GetCustomAttributes()
			.Where(x => x is ITestDataSource)
			.Cast<ITestDataSource>()
			.ToList();

		InjectedPointerTypes = method
			.GetCustomAttributes<InjectedPointerAttribute>()
			.Select(attr => attr.Type)
			.Distinct()
			.ToList();
	}

	public string Name => Method.Name;

	public MethodInfo Method { get; }

	public Type? ExpectedException { get; }

	public bool RequiresFullWindow { get; }

	public bool RunsOnUIThread { get; }

	public IReadOnlyList<ITestDataSource> DataSources { get; }

	public IReadOnlyList<PointerDeviceType> InjectedPointerTypes { get; }

	private static bool HasCustomAttribute<T>(MemberInfo? testMethod)
		=> testMethod?.GetCustomAttribute(typeof(T)) != null;

	public bool IsIgnored(out string ignoreMessage)
	{
		var ignoreAttribute = Method.GetCustomAttribute<IgnoreAttribute>();
		if (ignoreAttribute == null)
		{
			ignoreAttribute = Method.DeclaringType?.GetCustomAttribute<IgnoreAttribute>();
		}

		if (ignoreAttribute != null)
		{
			ignoreMessage = string.IsNullOrEmpty(ignoreAttribute.IgnoreMessage) ? "Test is marked as ignored" : ignoreAttribute.IgnoreMessage;
			return true;
		}

		ignoreMessage = "";
		return false;
	}
}
#endif