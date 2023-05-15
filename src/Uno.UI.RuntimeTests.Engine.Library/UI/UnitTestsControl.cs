﻿#if !UNO_RUNTIMETESTS_DISABLE_UI

#nullable enable

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Newtonsoft.Json;
using Uno.UI.RuntimeTests.Internal.Helpers;

#if HAS_UNO
using Uno.Foundation.Logging;
using Uno.Logging;
#endif

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Microsoft.UI.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using XamlWindow = Microsoft.UI.Xaml.Window;
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

using XamlWindow = Windows.UI.Xaml.Window;
#endif

namespace Uno.UI.RuntimeTests;

public sealed partial class UnitTestsControl : UserControl
{
#pragma warning disable CS0109
#if HAS_UNO
	private new readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(UnitTestsControl));
#endif
#pragma warning restore CS0109

	private Task? _runner;
	private CancellationTokenSource? _cts = new CancellationTokenSource();
#if DEBUG
	private readonly TimeSpan DefaultUnitTestTimeout = TimeSpan.FromSeconds(300);
#else
	private readonly TimeSpan DefaultUnitTestTimeout = TimeSpan.FromSeconds(60);
#endif

	private readonly UnitTestDispatcherCompat _dispatcher;

#pragma warning disable CS0649 // Unused field
	private ApplicationView? _applicationView;
#pragma warning restore CS0649 // Unused field

	private List<TestCaseResult> _testCases = new List<TestCaseResult>();
	private TestRun? _currentRun;

	// On WinUI/UWP dependency properties cannot be accessed outside of
	// UI thread. This field caches the current value so it can be accessed
	// asynchronously during test enumeration.
	private int _ciTestsGroupCountCache = -1;
	private int _ciTestGroupCache = -1;

	public UnitTestsControl()
	{
		this.InitializeComponent();

		_dispatcher = UnitTestDispatcherCompat.From(this);

		UnitTestsUIContentHelper.EmbeddedTestRoot =
		(
			Control: unitTestContentRoot,
			GetContent: () => unitTestContentRoot.Content as UIElement,
			SetContent: elt => unitTestContentRoot.Content = elt
		);
		UnitTestsUIContentHelper.CurrentTestWindow = XamlWindow.Current;

		DataContext = null;

		EnableConfigPersistence();
		OverrideDebugProviderAsserts();

#if HAS_UNO
		_applicationView = ApplicationView.GetForCurrentView();
#endif
	}

	private static void OverrideDebugProviderAsserts()
	{
#if NETSTANDARD2_0 || NET5_0_OR_GREATER
		if (Type.GetType("System.Diagnostics.DebugProvider") is { } type)
		{
			if (type.GetField("s_FailCore", BindingFlags.NonPublic | BindingFlags.Static) is { } fieldInfo)
			{
				fieldInfo.SetValue(null, (Action<string, string, string, string>)FailCore);
			}
		}
#endif
	}

	static void FailCore(string stackTrace, string message, string detailMessage, string errorSource)
		=> throw new Exception($"{message} ({detailMessage}) {stackTrace}");

	public bool IsRunningOnCI
	{
		get { return (bool)GetValue(IsRunningOnCIProperty); }
		set { SetValue(IsRunningOnCIProperty, value); }
	}

	// Using a DependencyProperty as the backing store for IsRunningOnCI.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty IsRunningOnCIProperty =
		DependencyProperty.Register("IsRunningOnCI", typeof(bool), typeof(UnitTestsControl), new PropertyMetadata(false));

	/// <summary>
	/// Defines the test group for splitting runtime tests on CI
	/// </summary>
	public int CITestGroup
	{
		get => (int)GetValue(CITestGroupProperty);
		set => SetValue(CITestGroupProperty, value);
	}

	public static readonly DependencyProperty CITestGroupProperty =
		DependencyProperty.Register("CITestGroup", typeof(int), typeof(UnitTestsControl), new PropertyMetadata(-1, OnCITestGroupChanged));

	private static void OnCITestGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var unitTestsControl = (UnitTestsControl)d;
		unitTestsControl._ciTestGroupCache = (int)e.NewValue;
	}

	/// <summary>
	/// Defines the test group for splitting runtime tests on CI
	/// </summary>
	public int CITestGroupCount
	{
		get => (int)GetValue(CITestGroupCountProperty);
		set => SetValue(CITestGroupCountProperty, value);
	}

	public static readonly DependencyProperty CITestGroupCountProperty =
		DependencyProperty.Register("CITestGroupCount", typeof(int), typeof(UnitTestsControl), new PropertyMetadata(-1, OnCITestsGroupCountChanged));

	private static void OnCITestsGroupCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var unitTestsControl = (UnitTestsControl)d;
		unitTestsControl._ciTestsGroupCountCache = (int)e.NewValue;
	}

	public string NUnitTestResultsDocument
	{
		get => (string)GetValue(NUnitTestResultsDocumentProperty);
		set => SetValue(NUnitTestResultsDocumentProperty, value);
	}

	public static readonly DependencyProperty NUnitTestResultsDocumentProperty =
		DependencyProperty.Register(nameof(NUnitTestResultsDocument), typeof(string), typeof(UnitTestsControl), new PropertyMetadata(string.Empty));

	/// <summary>
	/// Gets the unit tests runner status (Used by the Uno.UITests test side)
	/// </summary>
	public string RunningStateForUITest
	{
		get => (string)GetValue(RunningStateForUITestProperty);
		set => SetValue(RunningStateForUITestProperty, value);
	}

	public static readonly DependencyProperty RunningStateForUITestProperty =
		DependencyProperty.Register(nameof(RunningStateForUITest), typeof(string), typeof(UnitTestsControl), new PropertyMetadata("n/a"));

	/// <summary>
	/// Gets the unit tests that have run (Used by the Uno.UITests test side)
	/// </summary>
	public string RunTestCountForUITest
	{
		get => (string)GetValue(RunTestCountForUITestProperty);
		set => SetValue(RunTestCountForUITestProperty, value);
	}

	public static readonly DependencyProperty RunTestCountForUITestProperty =
		DependencyProperty.Register(nameof(RunTestCountForUITest), typeof(string), typeof(UnitTestsControl), new PropertyMetadata("-1"));

	/// <summary>
	/// Gets the unit tests that have failed (Used by the Uno.UITests test side)
	/// </summary>
	public string FailedTestCountForUITest
	{
		get => (string)GetValue(FailedTestCountForUITestProperty);
		set => SetValue(FailedTestCountForUITestProperty, value);
	}

	public static readonly DependencyProperty FailedTestCountForUITestProperty =
		DependencyProperty.Register(nameof(FailedTestCountForUITest), typeof(string), typeof(UnitTestsControl), new PropertyMetadata("-1"));

	private void OnRunTests(object sender, RoutedEventArgs e)
	{
		Interlocked.Exchange(ref _cts, new CancellationTokenSource())?.Cancel(); // cancel any previous CTS

		var config = BuildConfig();
		testResults.Children.Clear();

		_runner = Task.Run(() => RunTests(_cts!.Token, config));
	}


	private void OnStopTests(object sender, RoutedEventArgs e)
	{
		StopRunningTests();
	}

	private void StopRunningTests()
	{
		var cts = Interlocked.Exchange(ref _cts, null);
		cts?.Cancel();
	}

	private async Task ReportMessage(string message, bool isRunning = true)
	{
#if HAS_UNO
		_log?.Info(message);
#endif

		void Setter()
		{
			testFilter.IsEnabled = runButton.IsEnabled = !isRunning || _cts == null; // Disable the testFilter to avoid SIP to re-open

			if (IsRunningOnCI)
			{
				// Improves perf on CI by not re-rendering the whole test result live during tests
				testResults.Visibility = Visibility.Collapsed;
			}

			stopButton.IsEnabled = _cts != null && !_cts.IsCancellationRequested || !isRunning;
			RunningStateForUITest = runningState.Text = isRunning ? "Running" : "Finished";
			runStatus.Text = message;
			if (_applicationView != null)
			{
				_applicationView.Title = message;
			}
		}

		await _dispatcher.RunAsync(Setter);
	}

	private void ReportTestsResults()
	{
		void Update()
		{
			RunTestCountForUITest = runTestCount.Text = _currentRun?.Run.ToString() ?? "<no current run>";
			ignoredTestCount.Text = _currentRun?.Ignored.ToString() ?? "<no current run>";
			succeededTestCount.Text = _currentRun?.Succeeded.ToString() ?? "<no current run>";
			FailedTestCountForUITest = failedTestCount.Text = _currentRun?.Failed.ToString() ?? "<no current run>";
		}

		_dispatcher.Invoke(Update);
	}

	private async Task GenerateTestResults()
	{
		void Update()
		{
			if (_currentRun is not null)
			{
				var results = GenerateNUnitTestResults(_testCases, _currentRun);

				NUnitTestResultsDocument = results;
			}
		}

		await _dispatcher.RunAsync(Update);
	}

	private void ReportTestClass(TypeInfo testClass)
	{
		_dispatcher.Invoke(() =>
		{
			if (!IsRunningOnCI)
			{
				var testResultBlock = new TextBlock()
				{
					Text = $"{testClass.Name} ({testClass.Assembly.GetName().Name})",
					Foreground = new SolidColorBrush(Colors.White),
					FontSize = 16d,
					IsTextSelectionEnabled = true
				};

				testResults.Children.Add(testResultBlock);
				testResultBlock.StartBringIntoView();
			}
		});
	}

	private void ReportTestResult(string testName, TimeSpan duration, TestResult testResult, Exception? error = null, string? message = null, string? console = null)
	{
		_testCases.Add(
			new TestCaseResult
			{
				TestName = testName,
				Duration = duration,
				TestResult = testResult,
				Message = error?.ToString() ?? message
			});

		void Update()
		{
			if (_currentRun is null)
			{
				return;
			}

			runTestCount.Text = _currentRun.Run.ToString();
			ignoredTestCount.Text = _currentRun.Ignored.ToString();
			succeededTestCount.Text = _currentRun.Succeeded.ToString();
			failedTestCount.Text = _currentRun.Failed.ToString();

			var testResultBlock = new TextBlock()
			{
				TextWrapping = TextWrapping.Wrap,
				FontFamily = new FontFamily("Courier New"),
				Margin = new Thickness(8, 0, 0, 0),
				Foreground = new SolidColorBrush(Colors.LightGray),
				IsTextSelectionEnabled = true
			};

			var retriesText = _currentRun.CurrentRepeatCount != 0 ? $" (Retried {_currentRun.CurrentRepeatCount} time(s))" : "";

			testResultBlock.Inlines.Add(new Run
			{
				Text = GetTestResultIcon(testResult) + ' ' + testName + retriesText,
				FontSize = 13.5d,
				Foreground = new SolidColorBrush(UnitTestsControl.GetTestResultColor(testResult)),
				FontWeight = FontWeights.ExtraBold
			});

			if (message is { })
			{
				testResultBlock.Inlines.Add(new Run { Text = "\n  ..." + message, FontStyle = FontStyle.Italic });
			}

			if (error is { })
			{
				var isFailed = testResult == TestResult.Failed || testResult == TestResult.Error;

				var foreground = isFailed ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Yellow);
				testResultBlock.Inlines.Add(new Run { Text = "\nEXCEPTION>" + error.Message, Foreground = foreground });

				if (isFailed)
				{
					failedTestDetails.Text += $"{testResult}: {testName} [{error.GetType()}] \n {error}\n\n";
					if (failedTestDetailsRow.Height.Value == 0)
					{
						failedTestDetailsRow.Height = new GridLength(100);
					}
				}
			}

			if (console is { })
			{
				testResultBlock.Inlines.Add(new Run { Text = "\nOUT>" + console, Foreground = new SolidColorBrush(Colors.Gray) });
			}

			if (!IsRunningOnCI)
			{
				testResults.Children.Add(testResultBlock);
				testResultBlock.StartBringIntoView();
			}

			if (testResult == TestResult.Error || testResult == TestResult.Failed)
			{
				failedTests.Text += "§" + testName;
			}
		}

		_dispatcher.Invoke(Update);
	}

	private static string GenerateNUnitTestResults(List<TestCaseResult> testCases, TestRun testRun)
	{
		var resultsId = Guid.NewGuid().ToString();

		var doc = new XmlDocument();
		var rootNode = doc.CreateElement("test-run");
		doc.AppendChild(rootNode);
		rootNode.SetAttribute("id", resultsId);
		rootNode.SetAttribute("name", "Runtime Tests");
		rootNode.SetAttribute("testcasecount", testRun.Run.ToString());
		rootNode.SetAttribute("result", testRun.Failed == 0 ? "Passed" : "Failed");
		rootNode.SetAttribute("time", "0");
		rootNode.SetAttribute("total", testRun.Run.ToString());
		rootNode.SetAttribute("errors", "0");
		rootNode.SetAttribute("passed", testRun.Succeeded.ToString());
		rootNode.SetAttribute("failed", testRun.Failed.ToString());
		rootNode.SetAttribute("inconclusive", "0");
		rootNode.SetAttribute("skipped", testRun.Ignored.ToString());
		rootNode.SetAttribute("asserts", "0");

		var now = DateTimeOffset.Now;
		rootNode.SetAttribute("run-date", now.ToString("yyyy-MM-dd"));
		rootNode.SetAttribute("start-time", now.ToString("HH:mm:ss"));
		rootNode.SetAttribute("end-time", now.ToString("HH:mm:ss"));

		var testSuiteAssemblyNode = doc.CreateElement("test-suite");
		rootNode.AppendChild(testSuiteAssemblyNode);
		testSuiteAssemblyNode.SetAttribute("type", "Assembly");
		testSuiteAssemblyNode.SetAttribute("name", typeof(UnitTestsControl).Assembly.GetName().Name);

		var environmentNode = doc.CreateElement("environment");
		testSuiteAssemblyNode.AppendChild(environmentNode);
		environmentNode.SetAttribute("machine-name", Environment.MachineName);
		environmentNode.SetAttribute("platform", "n/a");

		var testSuiteFixtureNode = doc.CreateElement("test-suite");
		testSuiteAssemblyNode.AppendChild(testSuiteFixtureNode);

		testSuiteFixtureNode.SetAttribute("type", "TestFixture");
		testSuiteFixtureNode.SetAttribute("name", resultsId);
		testSuiteFixtureNode.SetAttribute("executed", "true");

		testSuiteFixtureNode.SetAttribute("testcasecount", testRun.Run.ToString());
		testSuiteFixtureNode.SetAttribute("result", testRun.Failed == 0 ? "Passed" : "Failed");
		testSuiteFixtureNode.SetAttribute("time", "0");
		testSuiteFixtureNode.SetAttribute("total", testRun.Run.ToString());
		testSuiteFixtureNode.SetAttribute("errors", "0");
		testSuiteFixtureNode.SetAttribute("passed", testRun.Succeeded.ToString());
		testSuiteFixtureNode.SetAttribute("failed", testRun.Failed.ToString());
		testSuiteFixtureNode.SetAttribute("inconclusive", "0");
		testSuiteFixtureNode.SetAttribute("skipped", testRun.Ignored.ToString());
		testSuiteFixtureNode.SetAttribute("asserts", "0");

		foreach (var run in testCases)
		{
			var testCaseNode = doc.CreateElement("test-case");
			testSuiteFixtureNode.AppendChild(testCaseNode);

			testCaseNode.SetAttribute("name", run.TestName);
			testCaseNode.SetAttribute("fullname", run.TestName);
			testCaseNode.SetAttribute("duration", run.Duration.TotalSeconds.ToString(CultureInfo.InvariantCulture));
			testCaseNode.SetAttribute("time", "0");

			testCaseNode.SetAttribute("result", run.TestResult.ToString());

			if (run.TestResult == TestResult.Failed || run.TestResult == TestResult.Error)
			{
				var failureNode = doc.CreateElement("failure");
				testCaseNode.AppendChild(failureNode);

				var messageNode = doc.CreateElement("message");
				failureNode.AppendChild(messageNode);

				messageNode.InnerText = run.Message ?? "";
			}
		}

		using var w = new StringWriter();
		doc.Save(w);

		return w.ToString();
	}

	private void EnableConfigPersistence()
	{
		if (ApplicationData.Current.LocalSettings.Values.TryGetValue("unitestcontrols_config", out var configRaw)
			&& configRaw is string configStr)
		{
			try
			{
				var config = JsonConvert.DeserializeObject<UnitTestEngineConfig>(configStr);

				if (config is not null)
				{
					consoleOutput.IsChecked = config.IsConsoleOutputEnabled;
					runIgnored.IsChecked = config.IsRunningIgnored;
					retry.IsChecked = config.Attempts > 1;
					testFilter.Text = config.Query ?? string.Empty;
				}
			}
			catch (Exception)
			{
				// UNO MOVE
				// _log.Error("Failed to restore runtime tests config", e);
			}
		}

		ListenConfigChanged();
	}

	private void ListenConfigChanged()
	{
		consoleOutput.Checked += (snd, e) => StoreConfig();
		consoleOutput.Unchecked += (snd, e) => StoreConfig();
		runIgnored.Checked += (snd, e) => StoreConfig();
		runIgnored.Unchecked += (snd, e) => StoreConfig();
		retry.Checked += (snd, e) => StoreConfig();
		retry.Unchecked += (snd, e) => StoreConfig();
		testFilter.TextChanged += (snd, e) => StoreConfig();

		void StoreConfig()
		{
			var config = BuildConfig();
			ApplicationData.Current.LocalSettings.Values["unitestcontrols_config"] = JsonConvert.SerializeObject(config);
		}
	}

	private UnitTestEngineConfig BuildConfig()
	{
		var isConsoleOutput = consoleOutput.IsChecked ?? false;
		var isRunningIgnored = runIgnored.IsChecked ?? false;
		var attempts = (retry.IsChecked ?? true) ? UnitTestEngineConfig.DefaultRepeatCount : 1;
		var query = testFilter.Text.Trim() is { Length: >0 } text ? text : null;

		return new UnitTestEngineConfig
		{
			Query = query,
			IsConsoleOutputEnabled = isConsoleOutput,
			IsRunningIgnored = isRunningIgnored,
			Attempts = attempts,
		};
	}

	private static string GetTestResultIcon(TestResult testResult)
	{
		switch (testResult)
		{
			default:
			case TestResult.Error:
			case TestResult.Failed:
				return "❌ (F)";

			case TestResult.Skipped:
				return "🚫 (I)";

			case TestResult.Passed:
				return "✔️ (S)";
		}
	}

	private static Color GetTestResultColor(TestResult testResult)
	{
		switch (testResult)
		{
			case TestResult.Error:
			case TestResult.Failed:
			default:
				return Colors.Red;

			case TestResult.Skipped:
				return Colors.Orange;

			case TestResult.Passed:
				return Colors.LightGreen;
		}
	}

	public async Task RunTests(CancellationToken ct, UnitTestEngineConfig config)
	{
		_currentRun = new TestRun();

		try
		{
			SearchPredicateCollection? filters = null;
			if (!string.IsNullOrWhiteSpace(config.Query))
			{
				_ = ReportMessage("Processing search query");
				filters = SearchPredicate.ParseQuery(config.Query);
			}

			_ = ReportMessage("Enumerating tests");
			var testTypes = GetTestClasses(filters);

			_ = ReportMessage("Running tests...");
			foreach (var type in testTypes)
			{
				if (ct.IsCancellationRequested)
				{
					_ = ReportMessage("Stopped by user.", false);
					break;
				}

				var instance = Activator.CreateInstance(type: type.Type);

				await ExecuteTestsForInstance(ct, instance!, type, config, filters);
			}

			_ = ReportMessage("Tests finished running.", isRunning: false);
			ReportTestsResults();
		}
		catch (Exception e)
		{
			_currentRun.Failed = -1;
			_ = ReportMessage($"Tests runner failed {e}");
			ReportTestResult("Runtime exception", TimeSpan.Zero, TestResult.Failed, e);
			ReportTestsResults();
		}
		finally
		{
			await _dispatcher.RunAsync(() =>
			{
				testFilter.IsEnabled = runButton.IsEnabled = true; // Disable the testFilter to avoid SIP to re-open
				if (!IsRunningOnCI)
				{
					testResults.Visibility = Visibility.Visible;
				}
				stopButton.IsEnabled = false;
			});
		}

		await GenerateTestResults();
	}

	private async Task ExecuteTestsForInstance(
		CancellationToken ct,
		object instance,
		UnitTestClassInfo testClassInfo,
		UnitTestEngineConfig config,
		SearchPredicateCollection? filters)
	{
		using var consoleRecorder = config.IsConsoleOutputEnabled
			? ConsoleOutputRecorder.Start()
			: default;

		var tests = GetTestMethods(testClassInfo, filters)
			.Select(method => new UnitTestMethodInfo(instance, method))
			.ToArray();

		if (!tests.Any())
		{
			return;
		}

		ReportTestClass(testClassInfo.Type.GetTypeInfo());
		_ = ReportMessage($"Running {tests.Length} test methods");

		foreach (var test in tests)
		{
			var testName = test.Name;

			if (ct.IsCancellationRequested)
			{
				_ = ReportMessage("Stopped by user.", false);
				return;
			}

			if (test.IsIgnored(out var ignoreMessage))
			{
				if (config.IsRunningIgnored)
				{
					ignoreMessage = $"\n--> [Ignored] IS BYPASSED...";
				}

				if (_currentRun is not null)
				{
					_currentRun.Ignored++;
				}
				ReportTestResult(testName, TimeSpan.Zero, TestResult.Skipped, message: ignoreMessage);

				if (!config.IsRunningIgnored)
				{
					continue;
				}
			}

			foreach (var testCase in GetTestCases(test, filters))
			{
				if (ct.IsCancellationRequested)
				{
					_ = ReportMessage("Stopped by user.", false);
					return;
				}

				await InvokeTestMethod(testCase);
			}

			async Task InvokeTestMethod(TestCase testCase)
			{
				var fullTestName = string.IsNullOrWhiteSpace(testCase.DisplayName) ? $"{testName}{testCase}" : testCase.DisplayName!;

				if (_currentRun is null)
				{
					throw new InvalidOperationException("Invalid current run state");
				}

				_currentRun.Run++;
				_currentRun.CurrentRepeatCount = 0;


				// We await this to make sure the UI is updated before running the test.
				// This will help developpers to identify faulty tests when the app is crashing.
				await ReportMessage($"Running test {fullTestName}");
				ReportTestsResults();

				var cleanupActions = new List<Func<CancellationToken, Task>>();
				var sw = new Stopwatch();
				var canRetry = true;

				while (canRetry)
				{
					canRetry = false;

					try
					{
						if (test.RequiresFullWindow)
						{
							await _dispatcher.RunAsync(() =>
							{
#if __ANDROID__
								// Hide the systray!
								ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
#endif

								UnitTestsUIContentHelper.UseActualWindowRoot = true;
								UnitTestsUIContentHelper.SaveOriginalContent();
							});
							cleanupActions.Add(async _ =>
							{
								await _dispatcher.RunAsync(() =>
								{
#if __ANDROID__
									// Restore the systray!
									ApplicationView.GetForCurrentView().ExitFullScreenMode();
#endif
									UnitTestsUIContentHelper.RestoreOriginalContent();
									UnitTestsUIContentHelper.UseActualWindowRoot = false;
								});
							});
						}

						object? returnValue = null;
						if (test.RunsOnUIThread)
						{
							await _dispatcher.RunAsync(() =>
							{
								if (InputInjectorHelper.TryGetCurrent() is not null)
								{
									InputInjectorHelper.Current.CleanupPointers();
								}

								if (testCase.Pointer is { } pt)
								{
									var ptSubscription = InputInjectorHelper.Current.SetPointerType(pt);
#pragma warning disable CS1998
									cleanupActions.Add(async _ => ptSubscription.Dispose());
#pragma warning restore CS1998
								}

								if (instance.GetType().GetProperty("Pointers", BindingFlags.Instance | BindingFlags.Public) is { SetMethod: not null } pointerProp
									&& pointerProp.PropertyType == typeof(InputInjectorHelper))
								{
									pointerProp.SetMethod.Invoke(instance, new[] { InputInjectorHelper.Current });
								}


								sw.Start();
								testClassInfo.Initialize?.Invoke(instance, Array.Empty<object>());
								returnValue = test.Method.Invoke(instance, testCase.Parameters);
								sw.Stop();
							});
						}
						else
						{
							if (testCase.Pointer is { } pt)
							{
								var ptSubscription = InputInjectorHelper.Current.SetPointerType(pt);
#pragma warning disable CS1998
								cleanupActions.Add(async _ => ptSubscription.Dispose());
#pragma warning restore CS1998
							}

							sw.Start();
							testClassInfo.Initialize?.Invoke(instance, Array.Empty<object>());
							returnValue = test.Method.Invoke(instance, testCase.Parameters);
							sw.Stop();
						}

						if (test.Method.ReturnType == typeof(Task))
						{
							var task = (Task)returnValue!;
							var timeoutTask = Task.Delay(DefaultUnitTestTimeout);

							var resultingTask = await Task.WhenAny(task, timeoutTask);

							if (resultingTask == timeoutTask)
							{
								throw new TimeoutException(
									$"Test execution timed out after {DefaultUnitTestTimeout}");
							}

							// Rethrow exception if failed OR task cancelled if task **internally** raised
							// a TaskCancelledException (we don't provide any cancellation token).
							await resultingTask;
						}

						var console = consoleRecorder?.GetContentAndReset();

						if (test.ExpectedException is null)
						{
							_currentRun.Succeeded++;
							ReportTestResult(fullTestName, sw.Elapsed, TestResult.Passed, console: console);
						}
						else
						{
							_currentRun.Failed++;
							ReportTestResult(fullTestName, sw.Elapsed, TestResult.Failed,
								message: $"Test did not throw the excepted exception of type {test.ExpectedException.Name}",
								console: console);
						}
					}
					catch (Exception ex)
					{
						sw.Stop();

						Exception? e = ex;

						if (e is AggregateException agg)
						{
							e = agg.InnerExceptions.FirstOrDefault();
						}

						if (e is TargetInvocationException tie)
						{
							e = tie.InnerException;
						}

						var console = consoleRecorder?.GetContentAndReset();

						if (e is AssertInconclusiveException inconclusiveException)
						{
							_currentRun.Ignored++;
							ReportTestResult(fullTestName, sw.Elapsed, TestResult.Skipped, message: e.Message, console: console);
						}
						else if (test.ExpectedException is null || !test.ExpectedException.IsInstanceOfType(e))
						{
							if (_currentRun.CurrentRepeatCount < config.Attempts - 1 && !Debugger.IsAttached)
							{
								_currentRun.CurrentRepeatCount++;
								canRetry = true;

								await RunCleanup(instance, testClassInfo, testName, test.RunsOnUIThread);
							}
							else
							{
								_currentRun.Failed++;
								ReportTestResult(fullTestName, sw.Elapsed, TestResult.Failed, e, console: console);
							}
						}
						else
						{
							_currentRun.Succeeded++;
							ReportTestResult(fullTestName, sw.Elapsed, TestResult.Passed, e, console: console);
						}
					}
					finally
					{
						foreach (var cleanup in cleanupActions.Where(action => action is not null))
						{
							await cleanup(CancellationToken.None);
						}
					}
				}
			}

			await RunCleanup(instance, testClassInfo, testName, test.RunsOnUIThread);

			if (ct.IsCancellationRequested)
			{
				_ = ReportMessage("Stopped by user.", false);
				return; // finish processing
			}
		}

		async Task RunCleanup(object instance, UnitTestClassInfo testClassInfo, string testName, bool runsOnUIThread)
		{
			void Run()
			{
				try
				{
					testClassInfo.Cleanup?.Invoke(instance, Array.Empty<object>());
				}
				catch (Exception e)
				{
					if (_currentRun is not null)
					{
						_currentRun.Failed++;
					}
					ReportTestResult(testName + " Cleanup", TimeSpan.Zero, TestResult.Failed, e, console: consoleRecorder?.GetContentAndReset());
				}
			}

			if (runsOnUIThread)
			{
				await _dispatcher.RunAsync(Run);
			}
			else
			{
				Run();
			}
		}
	}

	private static SHA1 _sha1 = SHA1.Create();

	private static int GetTypeTestGroup(Type type)
	{
		// Compute a stable hash of the full metadata name
		var buffer = Encoding.UTF8.GetBytes(type.FullName ?? "");
		var hash = _sha1.ComputeHash(buffer);

		return (int)BitConverter.ToUInt64(hash, 0);
	}

	private static UnitTestClassInfo? BuildType(Type type)
	{
		try
		{
			return new UnitTestClassInfo(
				type: type,
				tests: GetMethodsWithAttribute(type, typeof(TestMethodAttribute)),
				initialize: GetMethodsWithAttribute(type, typeof(TestInitializeAttribute)).FirstOrDefault(),
				cleanup: GetMethodsWithAttribute(type, typeof(TestCleanupAttribute)).FirstOrDefault()
			);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static MethodInfo[] GetMethodsWithAttribute(Type type, Type attributeType)
		=> (
			from method in type.GetMethods()
			where method.GetCustomAttribute(attributeType) != null
			select method
		).ToArray();

	private void UpdateFailedTestDetailsSize(object sender, ManipulationDeltaRoutedEventArgs e)
		=> failedTestDetailsRow.Height = new GridLength(Math.Max(0, failedTestDetailsRow.ActualHeight + e.Delta.Translation.Y));

	private void UpdateOuputSize(object sender, ManipulationDeltaRoutedEventArgs e)
		=> outputColumn.Width = new GridLength(Math.Max(0, outputColumn.ActualWidth + e.Delta.Translation.X));

	private void CopyFailedTestDetails(object sender, RoutedEventArgs e)
	{
		var data = new DataPackage();
		data.SetText(failedTestDetails.Text);

		Clipboard.SetContent(data);
	}

	private void CopyTestResults(object sender, RoutedEventArgs e)
	{
		var data = new DataPackage();
		data.SetText(NUnitTestResultsDocument);

		Clipboard.SetContent(data);
	}
}


#endif