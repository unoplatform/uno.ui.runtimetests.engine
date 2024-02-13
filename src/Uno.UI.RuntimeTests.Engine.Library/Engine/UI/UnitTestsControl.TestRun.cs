#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#pragma warning disable CA1852 // Make class final : unnecessary breaking change

namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_UI
public sealed partial class UnitTestsControl
{
	private class TestRun
	{
		public int Run { get; set; }
		public int Ignored { get; set; }
		public int Succeeded { get; set; }
		public int Failed { get; set; }

		public int CurrentRepeatCount { get; set; }
	}
}

#endif