#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Internal.Helpers;

#if HAS_UNO
#endif

#if false
using Windows.UI.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Microsoft.UI.Dispatching;

using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

using _DispatcherQueueHandler = Windows.UI.Core.DispatchedHandler;
#endif

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests;

public static partial class UnitTestsUIContentHelper
{
	private static Window? _currentTestWindow;
	private static UIElement? _originalWindowContent;

	internal static (UIElement Control, Func<UIElement?> GetContent, Action<UIElement?> SetContent) EmbeddedTestRoot { get; set; }

	public static bool UseActualWindowRoot { get; set; }

	public static UIElement? RootElement => UseActualWindowRoot
		? CurrentTestWindow?.Content
		: EmbeddedTestRoot.Control;

	// Dispatcher is a separate property, as accessing CurrentTestWindow.Content when
	// not on the UI thread will throw an exception.
	public static UnitTestDispatcherCompat RootElementDispatcher => UseActualWindowRoot && CurrentTestWindow is not null
		? UnitTestDispatcherCompat.From(CurrentTestWindow)
		: UnitTestDispatcherCompat.From(EmbeddedTestRoot.Control);

	public static void SaveOriginalContent()
	{
		_originalWindowContent = CurrentTestWindow?.Content;
	}

	public static void RestoreOriginalContent()
	{
		if (_originalWindowContent != null && CurrentTestWindow is not null)
		{
			CurrentTestWindow.Content = _originalWindowContent;
			_originalWindowContent = null;
		}
	}

	public static Window? CurrentTestWindow
	{
		get => _currentTestWindow ?? throw new InvalidOperationException("Current test window not set.");
		set => _currentTestWindow = value;
	}

	public static UIElement? Content
	{
		get => UseActualWindowRoot && CurrentTestWindow is not null
			? CurrentTestWindow.Content
			: EmbeddedTestRoot.GetContent?.Invoke();

		internal set
		{
			if (UseActualWindowRoot && CurrentTestWindow is not null)
			{
				CurrentTestWindow.Content = value;
			}
			else if (EmbeddedTestRoot.SetContent is { } setter)
			{
				setter(value);
			}
			else
			{
				Console.WriteLine("Failed to get test content control");
			}
		}
	}

	/// <summary>
	/// Waits for the dispatcher to finish processing pending requests
	/// </summary>
	/// <returns></returns>
	public static async Task WaitForIdle()
	{
		await RootElementDispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Low, () => { });
		await RootElementDispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Low, () => { });
	}

	/// <summary>
	/// Waits for <paramref name="element"/> to be loaded and measured in the visual tree.
	/// </summary>
	/// <remarks>
	/// On UWP, <see cref="WaitForIdle"/> may not always wait long enough for the control to be properly measured.
	///
	/// This method assumes that the control will have a non-zero size once loaded, so it's not appropriate for elements that are
	/// collapsed, empty, etc.
	/// </remarks>
	public static async Task WaitForLoaded(FrameworkElement element)
	{
		async Task Do()
		{
			bool IsLoaded()
			{
				if (element.ActualHeight == 0 || element.ActualWidth == 0)
				{
					return false;
				}

				if (element is ListView listView && listView.Items.Count > 0 && listView.ContainerFromIndex(0) == null)
				{
					// If it's a ListView, wait for items to be populated
					return false;
				}

				return element.IsLoaded;
			}

			await WaitFor(IsLoaded, message: $"{element} loaded");
		}
#if __WASM__ // Adjust for re-layout failures in When_Inline_Items_SelectedIndex, When_Observable_ItemsSource_And_Added, When_Presenter_Doesnt_Take_Up_All_Space
		await Do();
#else
		var dispatcher = UnitTestDispatcherCompat.From(element);
		if (dispatcher.HasThreadAccess)
		{
			await Do();
		}
		else
		{
			await dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, async () => await Do());
		}
#endif
	}

	/// <summary>
	/// Wait until a specified <paramref name="condition"/> is met. 
	/// </summary>
	/// <param name="timeoutMS">The maximum time to wait before failing the test, in milliseconds.</param>
	private static async Task WaitFor(Func<bool> condition, int timeoutMS = 1000, string? message = null, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int lineNumber = 0)
	{
		if (condition())
		{
			return;
		}

		var stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < timeoutMS)
		{
			await WaitForIdle();
			if (condition())
			{
				return;
			}
		}

		message ??= $"{callerMemberName}():{lineNumber}";

		throw new AssertFailedException("Timed out waiting for condition to be met. " + message);
	}

	public static async Task SetContentAndWait(FrameworkElement element)
	{
		Content = element;

		await WaitForIdle();
		await WaitForLoaded(element);
	}
}
