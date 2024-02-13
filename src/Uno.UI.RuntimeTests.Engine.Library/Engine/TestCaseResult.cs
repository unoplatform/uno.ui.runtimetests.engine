#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#pragma warning disable CA1852 // Make class final : unnecessary breaking change
#nullable enable

using System;
using System.Linq;

namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_UI

internal record TestCaseResult
{
	public TestResult TestResult { get; init; }
	public string? TestName { get; init; }
	public TimeSpan Duration { get; init; }
	public string? Message { get; init; }
	public TestCaseError? Error { get; init; }
	public string? ConsoleOutput { get; init; }
}

internal record TestCaseError(string Type, string Message)
{
	public static implicit operator TestCaseError?(Exception? ex)
		=> ex is null ? default : new TestCaseError(ex.GetType().Name, ex.Message);
}


#endif