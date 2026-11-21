using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Engine;

/// <summary>
/// Validates the streaming of the test results to an external runner, which lets it report the completed tests when the run
/// does not complete (https://github.com/unoplatform/uno.ui.runtimetests.engine/issues/220).
/// </summary>
[TestClass]
public class TestRunProgressReporterTests
{
	private const string Url = "http://localhost/progress";

	[TestMethod]
	public async Task When_Tests_Run_Then_Results_Are_Posted_In_Order()
	{
		var posts = new List<JsonElement>();
		var reporter = new TestRunProgressReporter(Url, "My.App", (_, content) => Record(posts, content));

		reporter.OnTestCaseStarted("Test_A()");
		reporter.OnTestCaseCompleted(Result("Test_A()", TestResult.Passed));
		reporter.OnTestCaseStarted("Test_B()");
		await reporter.Idle;
		reporter.OnTestCaseCompleted(Result("Test_B()", TestResult.Failed));
		reporter.OnTestCaseStarted("Test_C()");
		await reporter.Idle;

		Assert.AreEqual("Test_A(), Test_B()", string.Join(", ", posts.SelectMany(GetResultNames)), "Each result is posted once, in order");
		Assert.AreEqual("Test_C()", posts.Last().GetProperty("running").GetString());
	}

	[TestMethod]
	public async Task When_Running_Test_Completes_Then_It_Is_No_Longer_Running()
	{
		var posts = new List<JsonElement>();
		var reporter = new TestRunProgressReporter(Url, "My.App", (_, content) => Record(posts, content));

		reporter.OnTestCaseStarted("Test_A()");
		reporter.OnTestCaseCompleted(Result("Test_A()", TestResult.Passed));
		await reporter.Idle;

		Assert.AreEqual(JsonValueKind.Null, posts.Last().GetProperty("running").ValueKind);
	}

	[TestMethod]
	public async Task When_Post_Fails_Then_Results_Are_Posted_Again()
	{
		var posts = new List<JsonElement>();
		var calls = 0;
		var reporter = new TestRunProgressReporter(Url, "My.App", (_, content) =>
		{
			if (++calls == 1)
			{
				throw new InvalidOperationException("Simulated network error");
			}

			return Record(posts, content);
		});

		reporter.OnTestCaseCompleted(Result("Test_A()", TestResult.Passed));
		await reporter.Idle;

		Assert.AreEqual(2, calls);
		Assert.AreEqual("Test_A()", string.Join(", ", posts.SelectMany(GetResultNames)), "The results of the failed request are posted again");
	}

	[TestMethod]
	public async Task When_Post_Keeps_Failing_Then_Reporting_Stops()
	{
		var calls = 0;
		var reporter = new TestRunProgressReporter(Url, "My.App", (_, _) =>
		{
			calls++;
			return Task.FromResult(false);
		});

		reporter.OnTestCaseCompleted(Result("Test_A()", TestResult.Passed));
		await reporter.Idle;
		reporter.OnTestCaseCompleted(Result("Test_B()", TestResult.Passed));
		await reporter.Idle;

		Assert.AreEqual(3, calls, "Reporting must stop after 3 consecutive failures instead of retrying for the rest of the run");
		Assert.AreEqual(0, reporter.PendingCount, "Once reporting stopped, the results must not be kept, as nothing would ever send them");
	}

	[TestMethod]
	public void When_Serialized_Then_Format_Is_The_One_The_Runner_Expects()
	{
		var content = TestRunProgressReporter.Serialize(
			"My.App",
			"Test_B()",
			new[]
			{
				Result("Test_A()", TestResult.Failed, TimeSpan.FromSeconds(1.5), "Assert failed"),
				Result("Test_C()", TestResult.Skipped),
			});

		using var document = JsonDocument.Parse(content);
		var root = document.RootElement;
		Assert.AreEqual("My.App", root.GetProperty("assembly").GetString());
		Assert.AreEqual("Test_B()", root.GetProperty("running").GetString());

		var results = root.GetProperty("results").EnumerateArray().ToArray();
		Assert.AreEqual(2, results.Length);
		Assert.AreEqual("Test_A()", results[0].GetProperty("name").GetString());
		Assert.AreEqual("Failed", results[0].GetProperty("result").GetString());
		Assert.AreEqual(1.5, results[0].GetProperty("duration").GetDouble());
		Assert.AreEqual("Assert failed", results[0].GetProperty("message").GetString());
		Assert.AreEqual("Skipped", results[1].GetProperty("result").GetString());
		Assert.AreEqual(JsonValueKind.Null, results[1].GetProperty("message").ValueKind);
	}

	private static TestCaseResult Result(string name, TestResult result, TimeSpan duration = default, string? message = null)
		=> new() { TestName = name, TestResult = result, Duration = duration, Message = message };

	private static Task<bool> Record(List<JsonElement> posts, byte[] content)
	{
		using var document = JsonDocument.Parse(content);
		posts.Add(document.RootElement.Clone());

		return Task.FromResult(true);
	}

	private static IEnumerable<string?> GetResultNames(JsonElement post)
		=> post.GetProperty("results").EnumerateArray().Select(result => result.GetProperty("name").GetString());
}
