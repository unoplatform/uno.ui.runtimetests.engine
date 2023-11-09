namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_UI

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

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