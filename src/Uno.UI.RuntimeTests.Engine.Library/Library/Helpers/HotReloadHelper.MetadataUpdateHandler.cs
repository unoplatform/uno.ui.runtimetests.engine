#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.RuntimeTests;

[assembly:System.Reflection.Metadata.MetadataUpdateHandlerAttribute(typeof(HotReloadHelper.MetadataUpdateHandler))]

namespace Uno.UI.RuntimeTests;

partial class HotReloadHelper
{
	internal static class MetadataUpdateHandler
	{
		public static event EventHandler? MetadataUpdated;

		internal static void UpdateApplication(Type[]? types)
		{
			MetadataUpdated?.Invoke(null, EventArgs.Empty);
		}
	}
}
#endif
