using System;
using Android.Runtime;

namespace Uno.UI.RuntimeTests.Engine.Droid;

[global::Android.App.ApplicationAttribute(
	Label = "@string/ApplicationName",
	Icon = "@mipmap/iconapp",
	LargeHeap = true,
	HardwareAccelerated = true,
	Theme = "@style/AppTheme"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
	public Application(IntPtr javaReference, JniHandleOwnership transfer)
		: base(() => new App(), javaReference, transfer)
	{
	}
}
