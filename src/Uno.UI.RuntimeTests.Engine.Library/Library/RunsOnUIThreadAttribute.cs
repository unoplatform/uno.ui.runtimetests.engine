#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

using System;

#if USE_UNO_HOT_TESTING
namespace Uno.HotTesting;
#else // !USE_UNO_HOT_TESTING
namespace Uno.UI.RuntimeTests;
#endif // USE_UNO_HOT_TESTING

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY && !UNO_RUNTIMETESTS_DISABLE_RUNSONUITHREADATTRIBUTE
/// <summary>
/// Indicates that the test should be run on the UI thread.
/// </summary>
public sealed class RunsOnUIThreadAttribute : Attribute
{
}
#endif