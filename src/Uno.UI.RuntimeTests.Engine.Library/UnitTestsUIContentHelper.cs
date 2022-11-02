#nullable enable

using System;

#if HAS_UNO
#endif

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
#else
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

using _DispatcherQueueHandler = Windows.UI.Core.DispatchedHandler;
#endif

namespace Uno.UI.RuntimeTests;

public static class UnitTestsUIContentHelper
{
    private static Window? _currentTestWindow;
    private static UIElement? _originalWindowContent;


    internal static (UIElement control, Func<UIElement?> getContent, Action<UIElement?> setContent) EmbeddedTestRoot { get; set; }
    
    public static bool UseActualWindowRoot { get; set; }

    public static UIElement? RootElement => UseActualWindowRoot ?
        CurrentTestWindow?.Content : EmbeddedTestRoot.control;

    // Dispatcher is a separate property, as accessing CurrentTestWindow.COntent when
    // not on the UI thread will throw an exception in WinUI.
    public static DispatcherQueue RootElementDispatcher => UseActualWindowRoot && CurrentTestWindow is not null ?
        CurrentTestWindow.DispatcherQueue : EmbeddedTestRoot.control.DispatcherQueue;
    
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
        get
        {
            if (_currentTestWindow is null)
            {
                throw new InvalidOperationException("Current test window not set.");
            }
            return _currentTestWindow;
        }
        set => _currentTestWindow = value;
    }

    public static UIElement? Content
    {
        get => UseActualWindowRoot && CurrentTestWindow is not null
            ? CurrentTestWindow.Content
            : EmbeddedTestRoot.getContent?.Invoke();

        internal set
        {
            if (UseActualWindowRoot && CurrentTestWindow is not null)
            {
                CurrentTestWindow.Content = value;
            }
            else if (EmbeddedTestRoot.setContent is { } setter)
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
        async Task InnerWaitForIdle()
        {
            var tcs = new TaskCompletionSource<bool>();

            RootElementDispatcher.TryEnqueue(DispatcherQueuePriority.Low, () => { tcs.TrySetResult(true); });

            await tcs.Task;
        }

        await InnerWaitForIdle();
        await InnerWaitForIdle();
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
#if __WASM__   // Adjust for re-layout failures in When_Inline_Items_SelectedIndex, When_Observable_ItemsSource_And_Added, When_Presenter_Doesnt_Take_Up_All_Space
        await Do();
#else
				if (element.DispatcherQueue.HasThreadAccess)
				{
					await Do();
				}
				else
				{
					TaskCompletionSource<bool> cts = new();

					_ = element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
					{
						try
						{

							cts.TrySetResult(true);
						}
						catch (Exception e)
						{
							cts.TrySetException(e);
						}
					});

					await cts.Task;
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
}
