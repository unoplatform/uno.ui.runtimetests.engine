#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY

[assembly: System.Reflection.Metadata.MetadataUpdateHandlerAttribute(typeof(global::Uno.UI.RuntimeTests.HotReloadHelper.MetadataUpdateHandler))]

namespace Uno.UI.RuntimeTests;

partial class HotReloadHelper
{
	internal static class MetadataUpdateHandler
	{
		public static event global::System.EventHandler? MetadataUpdated;

		internal static void UpdateApplication(global::System.Type[]? types)
		{
			MetadataUpdated?.Invoke(null, global::System.EventArgs.Empty);
		}
	}
}
#endif
