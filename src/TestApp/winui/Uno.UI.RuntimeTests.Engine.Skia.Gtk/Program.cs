﻿using GLib;
using System;
using Uno.UI.Runtime.Skia.Gtk;

namespace Uno.UI.RuntimeTests.Engine.Skia.Gtk;

internal sealed class Program
{
	static void Main(string[] args)
	{
		ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
		{
			Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
			expArgs.ExitApplication = true;
		};

		var host = new GtkHost(() => new App());

		host.Run();
	}
}