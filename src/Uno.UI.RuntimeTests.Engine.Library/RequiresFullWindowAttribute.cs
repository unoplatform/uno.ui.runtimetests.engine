#nullable enable

using System;

namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_REQUIRESFULLWINDOWATTRIBUTE
/// <summary>
/// Marks a test which sets its test UI to be full-screen (Window.Content).
/// </summary>
public class RequiresFullWindowAttribute : Attribute
{
}
#endif