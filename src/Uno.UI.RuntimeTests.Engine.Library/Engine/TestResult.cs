#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
# endif

using System;
using System.Linq;

namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_UI

internal enum TestResult
{
	Passed,
	Failed,
	Error,
	Skipped,
}

#endif