#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Linq;

#if USE_UNO_HOT_TESTING
namespace Uno.HotTesting;
#else // !USE_UNO_HOT_TESTING
namespace Uno.UI.RuntimeTests;
#endif // USE_UNO_HOT_TESTING

#if USE_UNO_HOT_TESTING || !UNO_RUNTIMETESTS_DISABLE_UI

internal enum TestResult
{
	Passed,
	Failed,
	Error,
	Skipped,
}

#endif