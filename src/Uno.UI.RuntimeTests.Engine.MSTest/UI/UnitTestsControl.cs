#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CA1848 // Use the LoggerMessage delegates
#nullable enable

#if USE_UNO_MSTEST_ENGINE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Internal.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

using Uno.UI.RuntimeTests.Engine;

using XamlWindow = Microsoft.UI.Xaml.Window;

namespace Uno.UI.RuntimeTests;

/// <summary>
/// MSTest-engine-native counterpart of the hand-rolled <c>UnitTestsControl</c> from
/// <c>Uno.UI.RuntimeTests.Engine.Library</c>. Discovery narrows a candidate set of test methods
/// (filter/CI-group/sharding), which is then handed to MSTest's own engine (via
/// <see cref="TestApplicationBuilderExtensions.AddMSTest"/>) as a <c>--filter</c> expression --
/// actual test execution (including <see cref="UnoTestClassAttribute"/>/<see cref="UnoTestMethodAttribute"/>
/// dispatch) happens inside MSTest's real pipeline, not in a hand-rolled loop.
/// </summary>
public sealed partial class UnitTestsControl : UserControl
{
#pragma warning disable CS0109
	private new readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(UnitTestsControl));
#pragma warning restore CS0109

	private Task? _runner;
	private CancellationTokenSource? _cts = new CancellationTokenSource();

	private readonly UnitTestDispatcherCompat _dispatcher;

#pragma warning disable CS0649 // Unused field
	private ApplicationView? _applicationView;
#pragma warning restore CS0649 // Unused field

	private readonly List<TestCaseResult> _testCases = new();
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
		UnitTestsUIContentHelper.CurrentTestWindow ??= XamlWindow.Current;

		DataContext = null;

		EnableConfigPersistence();
		OverrideDebugProviderAsserts();

#if HAS_UNO
		_applicationView = ApplicationView.GetForCurrentView();
#endif

		ConstructPartial();
	}

	partial void ConstructPartial();

	internal IEnumerable<TestCaseResult> Results => _testCases;

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

#pragma warning disable CA2201 // Do not raise reserved exception types
	static void FailCore(string stackTrace, string message, string detailMessage, string errorSource)
		=> throw new Exception($"{message} ({detailMessage}) {stackTrace}");
#pragma warning restore CA2201

	public bool IsRunningOnCI
	{
		get { return (bool)GetValue(IsRunningOnCIProperty); }
		set { SetValue(IsRunningOnCIProperty, value); }
	}

	public static readonly DependencyProperty IsRunningOnCIProperty =
		DependencyProperty.Register("IsRunningOnCI", typeof(bool), typeof(UnitTestsControl), new PropertyMetadata(false));

	public bool IsSecondaryApp
	{
		get { return (bool)GetValue(IsSecondaryAppProperty); }
		set { SetValue(IsSecondaryAppProperty, value); }
	}

	public static readonly DependencyProperty IsSecondaryAppProperty =
		DependencyProperty.Register(nameof(IsSecondaryApp), typeof(bool), typeof(UnitTestsControl), new PropertyMetadata(false));

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
		// Note: under the MSTest-native engine, Stop can no longer preemptively abort an
		// in-flight run -- Microsoft.Testing.Platform's ITestApplication.RunAsync() doesn't
		// accept an external CancellationToken. This still prevents a *queued* run from
		// starting and is checked before the MSTest host is built.
		var cts = Interlocked.Exchange(ref _cts, null);
		cts?.Cancel();
	}

	private async Task ReportMessage(string message, bool isRunning = true)
	{
		_log.LogInformation(message);

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

	private void ReportTestResult(string testName, TimeSpan duration, TestResult testResult, Exception? error = null, string? message = null, string? console = null)
		=> ReportTestResult(
			new TestCaseResult
			{
				TestName = testName,
				Duration = duration,
				TestResult = testResult,
				Message = error?.ToString() ?? message,
				Error = error,
				ConsoleOutput = console
			});

	private void ReportTestResult(params TestCaseResult[] results)
	{
		_testCases.AddRange(results);
		_dispatcher.Invoke(() =>
		{
			foreach (var result in results)
			{
				UpdateUI(result);
			}
		});

		if (_log.IsEnabled(LogLevel.Information))
		{
			foreach (var result in results)
			{
				_log.LogInformation("Test completed '{TestName}'='{TestResult}'", result.TestName, result.TestResult);
			}
		}

		void UpdateUI(TestCaseResult result)
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

			testResultBlock.Inlines.Add(new Run
			{
				Text = GetTestResultIcon(result.TestResult) + ' ' + result.TestName,
				FontSize = 13.5d,
				Foreground = new SolidColorBrush(GetTestResultColor(result.TestResult)),
				FontWeight = FontWeights.ExtraBold
			});

			if (result.Message is { })
			{
				testResultBlock.Inlines.Add(new Run { Text = "\n  ..." + result.Message, FontStyle = Windows.UI.Text.FontStyle.Italic });
			}

			if (result.Error is { })
			{
				var isFailed = result.TestResult == TestResult.Failed || result.TestResult == TestResult.Error;

				var foreground = isFailed ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Yellow);
				testResultBlock.Inlines.Add(new Run { Text = "\nEXCEPTION>" + result.Error.Message, Foreground = foreground });

				if (isFailed)
				{
					failedTestDetails.Text += $"{result.TestResult}: {result.TestName} [{result.Error.Type}] \n {result.Error.Message}\n\n";
					if (failedTestDetailsRow.Height.Value == 0)
					{
						failedTestDetailsRow.Height = new GridLength(100);
					}
				}
			}

			if (result.ConsoleOutput is { })
			{
				testResultBlock.Inlines.Add(new Run { Text = "\nOUT>" + result.ConsoleOutput, Foreground = new SolidColorBrush(Colors.Gray) });
			}

			if (!IsRunningOnCI)
			{
				testResults.Children.Add(testResultBlock);
				testResultBlock.StartBringIntoView();
			}

			if (result.TestResult == TestResult.Error || result.TestResult == TestResult.Failed)
			{
				failedTests.Text += "§" + result.TestName;
			}
		}
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

		using var w = new Utf8StringWriter();
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
				var config = JsonSerializer.Deserialize<UnitTestEngineConfig>(configStr);

				if (config is not null)
				{
					consoleOutput.IsChecked = config.IsConsoleOutputEnabled;
					showSecondaryApp.IsChecked = config.IsSecondaryAppVisible;
					testFilter.Text = config.Filter;
				}
			}
			catch (Exception error)
			{
				_log.LogError(error, "Failed to restore runtime tests config.");
			}
		}

		ListenConfigChanged();
	}

	private void ListenConfigChanged()
	{
		consoleOutput.Checked += (snd, e) => StoreConfig();
		consoleOutput.Unchecked += (snd, e) => StoreConfig();
		showSecondaryApp.Checked += (snd, e) => StoreConfig();
		showSecondaryApp.Unchecked += (snd, e) => StoreConfig();
		testFilter.TextChanged += (snd, e) => StoreConfig();

		void StoreConfig()
		{
			var config = BuildConfig();
			ApplicationData.Current.LocalSettings.Values["unitestcontrols_config"] = JsonSerializer.Serialize(config);
		}
	}

	/// <remarks>
	/// The MSTest-native engine has no equivalent of the old engine's "Auto retry" (<see cref="UnitTestEngineConfig.Attempts"/>)
	/// or "Run [Ignore]" (<see cref="UnitTestEngineConfig.IsRunningIgnored"/>) toggles -- MSTest filters
	/// [Ignore]d tests out before our hooks run, and there is no retry hook. Both toggles remain in the
	/// shared XAML for visual/layout parity but aren't wired to any behavior here.
	/// </remarks>
	private UnitTestEngineConfig BuildConfig()
	{
		var isConsoleOutput = consoleOutput.IsChecked ?? false;
		var isSecondaryAppVisible = showSecondaryApp.IsChecked;
		var filter = testFilter.Text.Trim();
		if (string.IsNullOrEmpty(filter))
		{
			filter = null;
		}

		return new UnitTestEngineConfig
		{
			Filter = filter,
			IsConsoleOutputEnabled = isConsoleOutput,
			IsSecondaryAppVisible = isSecondaryAppVisible,
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

	public async Task RunTestsForInstance(object testClassInstance)
	{
#pragma warning disable CA1849
		Interlocked.Exchange(ref _cts, new CancellationTokenSource())?.Cancel(); // cancel any previous CTS

		testResults.Children.Clear();

		var config = BuildConfig() with { Filter = testClassInstance.GetType().FullName };

		await RunTests(_cts!.Token, config);
	}

	public async Task RunTests(CancellationToken ct, UnitTestEngineConfig config)
	{
		_currentRun = new TestRun();
		_testCases.Clear();

		try
		{
			_ = ReportMessage("Enumerating tests");

			var candidates = EnumerateTestMethods().ToArray();

			IEnumerable<(Type Type, MethodInfo Method, string Fqn)> selected = candidates;

			if (config.Filter is { } filter)
			{
				selected = selected.Where(c => filter.IsMatch(c.Method));
			}

			if (config is { ShardIndex: { } shardIndex, TotalShards: { } totalShards } && totalShards > 1)
			{
				selected = selected.Where(c => IsTestInShard(c.Fqn, shardIndex, totalShards));
			}

			var selectedArray = selected.ToArray();

			if (ct.IsCancellationRequested)
			{
				_ = ReportMessage("Stopped by user.", false);
				return;
			}

			if (selectedArray.Length == 0)
			{
				_ = ReportMessage("No tests match the current filter/shard.", isRunning: false);
				ReportTestsResults();
				return;
			}

			_ = ReportMessage($"Running {selectedArray.Length} test methods...");

			var filterExpression = string.Join("|", selectedArray.Select(c => $"FullyQualifiedName={c.Fqn}"));
			var assemblies = selectedArray.Select(c => c.Type.Assembly).Distinct().ToArray();

			// Note: per-test console-output capture (the old engine's "Console Output" toggle) isn't
			// carried over -- MSTest's real engine invokes tests outside of our control, so there's no
			// single point left to wrap Console.Out per test case. Microsoft.Testing.Platform captures
			// and reports process-level output separately when running under `dotnet test`.
			await RunMSTestApplicationAsync(
				args: ["--filter", filterExpression],
				assemblies: assemblies,
				onResult: RegisterExternalResult,
				onInProgress: ReportInProgress);

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
				testFilter.IsEnabled = runButton.IsEnabled = true;
				if (!IsRunningOnCI)
				{
					testResults.Visibility = Visibility.Visible;
				}
				stopButton.IsEnabled = false;
			});
		}

		await GenerateTestResults();
	}

	/// <summary>
	/// Records one <see cref="TestCaseResult"/> reported live by a <see cref="UnitTestsMSTestReporter"/>
	/// (whether from this instance's own <see cref="RunTests"/> or from an externally-driven run, e.g.
	/// <c>MSTestRuntimeTestsRunner</c>'s <c>dotnet test</c> CLI flow), updating both the in-app UI and
	/// the accumulated <see cref="Results"/>/<see cref="NUnitTestResultsDocument"/>.
	/// </summary>
	internal void RegisterExternalResult(TestCaseResult result)
	{
		_currentRun ??= new TestRun();
		_currentRun.Run++;
		switch (result.TestResult)
		{
			case TestResult.Passed:
				_currentRun.Succeeded++;
				break;
			case TestResult.Skipped:
				_currentRun.Ignored++;
				break;
			default:
				_currentRun.Failed++;
				break;
		}

		ReportTestResult(result);
	}

	internal void ReportInProgress(string testName) => _ = ReportMessage($"Running test {testName}");

	/// <summary>
	/// Builds and runs a Microsoft.Testing.Platform <c>TestApplication</c> hosting MSTest's real
	/// engine (<see cref="TestApplicationBuilderExtensions.AddMSTest"/>), reporting live results through
	/// a <see cref="UnitTestsMSTestReporter"/>. Shared by the env-var/UI-driven flow (this file, with a
	/// synthesized <c>--filter</c>) and the <c>dotnet test</c> CLI flow (<c>MSTestRuntimeTestsRunner</c>,
	/// with the real process argv).
	/// </summary>
	internal static async Task<int> RunMSTestApplicationAsync(
		string[] args,
		IEnumerable<Assembly> assemblies,
		Action<TestCaseResult> onResult,
		Action<string>? onInProgress = null)
	{
		var builder = await TestApplication.CreateBuilderAsync(args);
		builder.AddMSTest(() => assemblies);
		builder.TestHost.AddDataConsumer(_ => new UnitTestsMSTestReporter());

		using var app = await builder.BuildAsync();
		return await app.RunAsync();
	}

	/// <summary>
	/// Enumerates candidate test methods (metadata-only reflection, doesn't run anything) so that
	/// filter/CI-group/sharding can narrow the set *before* handing a <c>--filter</c> expression to
	/// MSTest's own engine, mirroring the discovery scan the hand-rolled engine used to do in
	/// <c>InitializeTests()</c>.
	/// </summary>
	/// <summary>
	/// Candidate assemblies that may contain runtime tests, mirroring the hand-rolled engine's own
	/// heuristic (own assembly + any loaded assembly whose name ends with "Tests"). Shared with
	/// <c>MSTestRuntimeTestsRunner</c> so the <c>dotnet test</c> CLI flow scans the same set.
	/// </summary>
	internal static IEnumerable<Assembly> GetCandidateTestAssemblies()
		=> AppDomain.CurrentDomain.GetAssemblies()
			.Where(x => x.GetName()?.Name?.EndsWith("Tests", StringComparison.OrdinalIgnoreCase) ?? false)
			.Concat(new[] { typeof(UnitTestsControl).Assembly })
			.Distinct();

	private IEnumerable<(Type Type, MethodInfo Method, string Fqn)> EnumerateTestMethods()
	{
		foreach (var assembly in GetCandidateTestAssemblies())
		{
			foreach (var type in SafeGetTypes(assembly))
			{
				if (type.GetCustomAttribute(typeof(TestClassAttribute)) is null)
				{
					continue;
				}

				if (_ciTestsGroupCountCache != -1 && (GetTypeTestGroup(type.FullName ?? type.Name) % _ciTestsGroupCountCache) != _ciTestGroupCache)
				{
					continue;
				}

				foreach (var method in type.GetMethods())
				{
					if (method.GetCustomAttribute(typeof(TestMethodAttribute)) is null)
					{
						continue;
					}

					yield return (type, method, $"{type.FullName}.{method.Name}");
				}
			}
		}
	}

	private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException e)
		{
			return e.Types.Where(t => t is not null)!;
		}
		catch
		{
			return Array.Empty<Type>();
		}
	}

	private static int GetTypeTestGroup(string fullyQualifiedName)
	{
		var buffer = Encoding.UTF8.GetBytes(fullyQualifiedName);
		var hash = SHA1.HashData(buffer);

		return (int)BitConverter.ToUInt64(hash, 0);
	}

	/// <summary>
	/// Determines if a test method belongs to the specified shard using a deterministic
	/// hash-based modulo assignment on the fully-qualified test method name.
	/// </summary>
	private static bool IsTestInShard(string testFullName, int shardIndex, int totalShards)
	{
		var buffer = Encoding.UTF8.GetBytes(testFullName);
		var hash = SHA1.HashData(buffer);
		var hashValue = (int)(BitConverter.ToUInt64(hash, 0) % (ulong)totalShards);

		return hashValue == shardIndex;
	}

	private void UpdateFailedTestDetailsSize(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
		=> failedTestDetailsRow.Height = new GridLength(Math.Max(0, failedTestDetailsRow.ActualHeight + e.Delta.Translation.Y));

	private void UpdateOuputSize(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
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

	/// <summary>
	/// A StringWriter that uses UTF-8 encoding for the XML declaration.
	/// </summary>
	private sealed class Utf8StringWriter : StringWriter
	{
		public override Encoding Encoding => Encoding.UTF8;
	}
}
#endif
