#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests;

public static partial class HotReloadHelper
{
	private class NotSupported : IFileUpdater
	{
		/// <inheritdoc />
		public ValueTask EnsureReady(CancellationToken ct)
			=> throw new NotSupportedException("Source code file edition is not supported on this platform.");

		/// <inheritdoc />
		public ValueTask<ReConnect?> Apply(FileEdit edition, CancellationToken ct)
			=> throw new NotSupportedException("Source code file edition is not supported on this platform.");
	}
}
#endif