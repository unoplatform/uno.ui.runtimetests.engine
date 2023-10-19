#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests;

partial class HotReloadHelper
{
	static partial void TryUseLocalFileUpdater()
		=> Use(new LocalFileUpdater());

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void UseLocalFileUpdater()
		=> Use(new LocalFileUpdater());

	private class LocalFileUpdater : IFileUpdater
	{
		/// <inheritdoc />
		public async ValueTask EnsureReady(CancellationToken ct) { }

		/// <inheritdoc />
		public async ValueTask<ReConnect?> Apply(FileEdit edition, CancellationToken ct)
		{
			if (!File.Exists(edition.FilePath))
			{
				throw new InvalidOperationException($"Source file {edition.FilePath} does not exist!");
			}

			var originalContent = await File.ReadAllTextAsync(edition.FilePath, ct);
			var updatedContent = originalContent.Replace(edition.OldText, edition.NewText);

			await File.WriteAllTextAsync(edition.FilePath, updatedContent, ct);

			return null;
		}
	}
}
#endif