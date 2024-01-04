#if !UNO_RUNTIMETESTS_DISABLE_UI

#nullable enable
#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#pragma warning disable CA1852 // Make class final : unnecessary breaking change

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Windows.Devices.Input;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests;

internal record UnitTestMethodInfo
{
	private readonly List<ITestDataSource> _casesParameters;
	private readonly IList<PointerDeviceType> _injectedPointerTypes;

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

		_casesParameters  = method
			.GetCustomAttributes()
			.Where(x => x is ITestDataSource)
			.Cast<ITestDataSource>()
			.ToList();

		_injectedPointerTypes = method
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

	public IEnumerable<TestCase> GetCases(CancellationToken ct)
	{
		List<TestCase> cases = new();

		if (_casesParameters is { Count: 0 })
		{
			cases.Add(Method.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken))
				? new TestCase { Parameters = new object[] { ct } }
				: new TestCase());
		}

		foreach (var testCaseSource in _casesParameters)
		{
			// Note: CT is not propagated when using a test data source
			var testCases = testCaseSource
				.GetData(Method)
				.Select(caseData => new TestCase
				{
					Parameters = caseData, 
					DisplayName = testCaseSource.GetDisplayName(Method, caseData)
				});

			cases.AddRange(testCases);
		}

		if (_injectedPointerTypes.Any())
		{
			var currentCases = cases;
			cases = _injectedPointerTypes.SelectMany(pointer => currentCases.Select(testCase => testCase with { Pointer = pointer })).ToList();
		}

		return cases;
	}
}
#endif