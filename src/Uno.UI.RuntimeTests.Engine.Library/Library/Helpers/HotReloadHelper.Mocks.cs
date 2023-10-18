#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
#nullable enable

#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

using System;
using System.Collections.Generic;
using System.Text;

#if !HAS_UNO_DEVSERVER
namespace Uno.UI.RemoteControl
{
	public partial class RemoteControlClient
	{
		public static RemoteControlClient? Instance => null;
	}
}

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	public partial class UpdateFile
	{
		public string FilePath { get; set; } = string.Empty;
		public string OldText { get; set; } = string.Empty;
		public string NewText { get; set; } = string.Empty;
	}
}
#endif
#endif