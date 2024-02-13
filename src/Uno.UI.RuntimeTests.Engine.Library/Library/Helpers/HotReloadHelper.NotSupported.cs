#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
namespace Uno.UI.RuntimeTests;

public static partial class HotReloadHelper
{
	private sealed class NotSupported : IFileUpdater
	{
		/// <inheritdoc />
		public global::System.Threading.Tasks.ValueTask EnsureReady(global::System.Threading.CancellationToken ct)
			=> throw new global::System.NotSupportedException("Source code file edition is not supported on this platform.");

		/// <inheritdoc />
		public global::System.Threading.Tasks.ValueTask<ReConnect?> Apply(FileEdit edition, global::System.Threading.CancellationToken ct)
			=> throw new global::System.NotSupportedException("Source code file edition is not supported on this platform.");
	}
}
#endif