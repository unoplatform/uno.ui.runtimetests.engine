#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
namespace Uno.UI.RuntimeTests;

partial class HotReloadHelper
{
	static partial void TryUseLocalFileUpdater()
		=> Use(new LocalFileUpdater());

	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Advanced)]
	public static void UseLocalFileUpdater()
		=> Use(new LocalFileUpdater());

	private sealed class LocalFileUpdater : IFileUpdater
	{
		/// <inheritdoc />
		public async global::System.Threading.Tasks.ValueTask EnsureReady(global::System.Threading.CancellationToken ct) { }

		/// <inheritdoc />
		public async global::System.Threading.Tasks.ValueTask<ReConnect?> Apply(FileEdit edition, global::System.Threading.CancellationToken ct)
		{
			if (!global::System.IO.File.Exists(edition.FilePath))
			{
				throw new global::System.InvalidOperationException($"Source file {edition.FilePath} does not exist!");
			}

			var originalContent = await global::System.IO.File.ReadAllTextAsync(edition.FilePath, ct);
			var updatedContent = originalContent.Replace(edition.OldText, edition.NewText);

			await global::System.IO.File.WriteAllTextAsync(edition.FilePath, updatedContent, ct);

			return null;
		}
	}
}
#endif