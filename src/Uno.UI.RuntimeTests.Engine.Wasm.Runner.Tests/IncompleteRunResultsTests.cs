using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Engine.Wasm.Runner;

namespace Uno.UI.RuntimeTests.Engine.Wasm.Runner.Tests;

/// <summary>
/// Validates that the results of the completed tests are written when the run does not complete, e.g. on timeout
/// (https://github.com/unoplatform/uno.ui.runtimetests.engine/issues/220).
/// </summary>
[TestClass]
public class IncompleteRunResultsTests
{
	private const string TwoResults = """
		{
			"assembly": "My.App",
			"running": "Test_C()",
			"results": [
				{ "name": "Test_A()", "result": "Passed", "duration": 0.5, "message": null },
				{ "name": "Test_B()", "result": "Failed", "duration": 1.25, "message": "Assert.AreEqual failed." }
			]
		}
		""";

	[TestMethod]
	public void Apply_AccumulatesResultsAndTracksRunningTest()
	{
		var progress = new TestRunProgress();

		progress.Apply(TwoResults);
		Assert.AreEqual(2, progress.CompletedCount);
		Assert.AreEqual("Test_C()", progress.RunningTest);

		progress.Apply("""{ "running": null, "results": [ { "name": "Test_C()", "result": "Skipped", "duration": 0, "message": "Ignored" } ] }""");
		Assert.AreEqual(3, progress.CompletedCount);
		Assert.IsNull(progress.RunningTest, "The running test is cleared once it completed");
	}

	[TestMethod]
	[DataRow("not json")]
	[DataRow("[]")]
	[DataRow("""{ "results": {} }""")]
	[DataRow("""{ "results": [ 42 ] }""")]
	[DataRow("""{ "results": [ { "name": "Test_D()", "result": "Passed" }, { "result": "Passed" } ] }""")]
	[DataRow("""{ "results": [ { "name": "Test_D()" } ] }""")]
	[DataRow("""{ "running": 42 }""")]
	[DataRow("""{ "running": "Test_\ud83d()" }""")] // Escaped lone surrogate
	[DataRow("""{ "results": [ { "name": "Test_\ud83d()", "result": "Passed" } ] }""")]
	public void Apply_InvalidUpdate_ThrowsAndChangesNothing(string update)
	{
		var progress = new TestRunProgress();
		progress.Apply(TwoResults);

		// Not ThrowsExactly: JsonDocument.Parse throws a type derived from JsonException
		Assert.Throws<JsonException>(() => progress.Apply(update), "Invalid updates must be rejected with a JsonException, which the server reports as a 400");

		Assert.AreEqual(2, progress.CompletedCount, "An invalid update must not be partially applied");
		Assert.AreEqual("Test_C()", progress.RunningTest);
	}

	[TestMethod]
	public void ToNUnitXml_ContainsTheCompletedTestsAndTheFailure()
	{
		var progress = new TestRunProgress();
		progress.Apply(TwoResults);
		progress.Apply("""{ "running": "Test_D()", "results": [ { "name": "Test_C()", "result": "Skipped", "duration": 0, "message": "Ignored" } ] }""");

		var document = XDocument.Parse(progress.ToNUnitXml("WasmRunner.Timeout", "Test execution timed out after 30 seconds, while 'Test_D()' was running."));

		var testRun = document.Root!;
		Assert.AreEqual("test-run", testRun.Name.LocalName);
		var assembly = testRun.Element("test-suite")!;
		Assert.AreEqual("My.App", (string?)assembly.Attribute("name"), "Same assembly suite name as the results of a complete run");
		var fixture = assembly.Element("test-suite")!;
		Assert.AreEqual("TestFixture", (string?)fixture.Attribute("type"));

		foreach (var summary in new[] { testRun, fixture })
		{
			Assert.AreEqual("Failed", (string?)summary.Attribute("result"));
			Assert.AreEqual("4", (string?)summary.Attribute("total"));
			Assert.AreEqual("4", (string?)summary.Attribute("testcasecount"));
			Assert.AreEqual("1", (string?)summary.Attribute("passed"));
			Assert.AreEqual("2", (string?)summary.Attribute("failed"));
			Assert.AreEqual("1", (string?)summary.Attribute("skipped"));
		}

		var testCases = fixture.Elements("test-case").ToArray();
		Assert.AreEqual(
			"Test_A():Passed, Test_B():Failed, Test_C():Skipped, WasmRunner.Timeout:Failed",
			string.Join(", ", testCases.Select(testCase => $"{testCase.Attribute("name")?.Value}:{testCase.Attribute("result")?.Value}")),
			"The completed tests, in order, followed by the failure describing why the run did not complete");
		Assert.AreEqual("1.25", (string?)testCases[1].Attribute("duration"));
		Assert.AreEqual("Assert.AreEqual failed.", testCases[1].Element("failure")?.Element("message")?.Value);
		Assert.IsNull(testCases[0].Element("failure"));
		Assert.AreEqual("Test execution timed out after 30 seconds, while 'Test_D()' was running.", testCases[3].Element("failure")?.Element("message")?.Value);
	}

	[TestMethod]
	public void ToNUnitXml_WithoutProgress_ContainsOnlyTheFailure()
	{
		// E.g. an app built with an engine that does not report its progress, or a run that stopped before the first test completed
		var document = XDocument.Parse(new TestRunProgress().ToNUnitXml("WasmRunner.BrowserExited", "Browser exited with code 1 before providing results."));

		var testCase = document.Descendants("test-case").Single();
		Assert.AreEqual("WasmRunner.BrowserExited", (string?)testCase.Attribute("name"));
		Assert.AreEqual("Failed", (string?)testCase.Attribute("result"));
		Assert.AreEqual("1", (string?)document.Root!.Attribute("failed"));
	}

	[TestMethod]
	public void ToNUnitXml_StartsWithUtf8Declaration()
	{
		var xml = new TestRunProgress().ToNUnitXml("WasmRunner.Canceled", "Test execution was canceled.");

		Assert.StartsWith("""<?xml version="1.0" encoding="utf-8"?>""", xml);
	}

	[TestMethod]
	public void ToNUnitXml_InvalidXmlChars_AreRemoved()
	{
		var progress = new TestRunProgress();
		progress.Apply("""{ "results": [ { "name": "Test_\u0001A()", "result": "Failed", "duration": 0, "message": "Bad \u0000char, kept 😀" } ] }""");

		var document = XDocument.Parse(progress.ToNUnitXml("WasmRunner.Timeout", "Test execution timed out after 30 seconds."));

		var testCase = document.Descendants("test-case").First();
		Assert.AreEqual("Test_A()", (string?)testCase.Attribute("name"));
		Assert.AreEqual("Bad char, kept 😀", testCase.Element("failure")?.Element("message")?.Value, "Invalid characters are removed, valid surrogate pairs are kept");
	}

	[TestMethod]
	[DataRow("abc", "abc")]
	[DataRow("a\u0000b\u001Fc", "abc")]
	[DataRow("tab\tnewline\n", "tab\tnewline\n")]
	[DataRow("emoji 😀", "emoji 😀")]
	public void RemoveInvalidXmlChars(string text, string expected)
	{
		Assert.AreEqual(expected, TestRunProgress.RemoveInvalidXmlChars(text));
	}

	[TestMethod]
	public void RemoveInvalidXmlChars_LoneSurrogates_AreRemoved()
	{
		// Not DataRows: attribute arguments are stored as UTF-8, which replaces lone surrogates with U+FFFD
		Assert.AreEqual("lone  surrogate", TestRunProgress.RemoveInvalidXmlChars("lone \ud83d surrogate"));
		Assert.AreEqual("reversed", TestRunProgress.RemoveInvalidXmlChars("\ude00reversed\ud83d"));
	}

	[TestMethod]
	[DataRow(false, null, null, "WasmRunner.Timeout", "Test execution timed out after 30 seconds.")]
	[DataRow(false, null, "Test_A()", "WasmRunner.Timeout", "Test execution timed out after 30 seconds, while 'Test_A()' was running.")]
	[DataRow(false, 1, null, "WasmRunner.BrowserExited", "Browser exited with code 1 before providing results.")]
	[DataRow(false, -1, "Test_A()", "WasmRunner.BrowserExited", "Browser exited with code -1 before providing results, while 'Test_A()' was running.")]
	[DataRow(true, null, null, "WasmRunner.Canceled", "Test execution was canceled.")]
	[DataRow(true, 1, "Test_A()", "WasmRunner.Canceled", "Test execution was canceled, while 'Test_A()' was running.")]
	public void DescribeIncompleteRun(bool isCanceled, int? browserExitCode, string? runningTest, string expectedName, string expectedMessage)
	{
		var (name, message) = Program.DescribeIncompleteRun(isCanceled, browserExitCode, 30, runningTest);

		Assert.AreEqual(expectedName, name);
		Assert.AreEqual(expectedMessage, message);
	}

	[TestMethod]
	public async Task Server_ProgressEndpoint_AppliesValidUpdatesAndRejectsInvalidOnes()
	{
		var appPath = Directory.CreateTempSubdirectory("uno-wasm-runner-tests-");
		try
		{
			using var server = new TestServer(appPath.FullName, 0);
			var port = await server.StartAsync();
			using var client = new HttpClient();
			var url = $"http://localhost:{port}/progress";

			using var valid = await client.PostAsync(url, new StringContent(TwoResults, Encoding.UTF8, "application/json"));
			using var invalid = await client.PostAsync(url, new StringContent("not json", Encoding.UTF8, "application/json"));

			Assert.AreEqual(HttpStatusCode.OK, valid.StatusCode);
			Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
			Assert.AreEqual(2, server.Progress.CompletedCount);
			Assert.AreEqual("Test_C()", server.Progress.RunningTest);
		}
		finally
		{
			appPath.Delete(recursive: true);
		}
	}
}
