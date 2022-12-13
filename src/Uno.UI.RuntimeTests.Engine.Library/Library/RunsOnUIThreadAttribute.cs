#nullable enable

using System;

namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY && !UNO_RUNTIMETESTS_DISABLE_RUNSONUITHREADATTRIBUTE
/// <summary>
/// Indicates that the test should be run on the UI thread.
/// </summary>
public sealed class RunsOnUIThreadAttribute : Attribute
{
}
#endif