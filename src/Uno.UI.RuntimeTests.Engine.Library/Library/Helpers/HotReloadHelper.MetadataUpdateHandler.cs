#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY

#if USE_UNO_HOT_TESTING
using Uno.HotTesting;
#else // !USE_UNO_HOT_TESTING
using Uno.UI.RuntimeTests;
#endif // USE_UNO_HOT_TESTING

[assembly: System.Reflection.Metadata.MetadataUpdateHandlerAttribute(typeof(HotReloadHelper.MetadataUpdateHandler))]

#if USE_UNO_HOT_TESTING
namespace Uno.HotTesting;
#else // !USE_UNO_HOT_TESTING
namespace Uno.UI.RuntimeTests;
#endif // USE_UNO_HOT_TESTING

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
