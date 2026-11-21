using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Uno.UI.RuntimeTests.Engine.Wasm.Runner;

/// <summary>
/// The test results streamed by the app while the tests are running (see <c>TestRunProgressReporter</c> in the engine),
/// used to report the completed tests when the run does not complete.
/// </summary>
internal sealed class TestRunProgress
{
	private readonly object _gate = new();
	private readonly List<CompletedTestCase> _completed = new();
	private string? _assemblyName;
	private string? _runningTest;

	/// <summary>
	/// Gets the number of test results received.
	/// </summary>
	public int CompletedCount
	{
		get
		{
			lock (_gate)
			{
				return _completed.Count;
			}
		}
	}

	/// <summary>
	/// Gets the test case that was running as of the last update, if any.
	/// </summary>
	public string? RunningTest
	{
		get
		{
			lock (_gate)
			{
				return _runningTest;
			}
		}
	}

	/// <summary>
	/// Applies a progress update posted by the app: <c>{ "assembly": "...", "running": "..." | null, "results": [ { "name", "result", "duration", "message" } ] }</c>.
	/// </summary>
	/// <exception cref="JsonException">The update is not valid; it is then ignored.</exception>
	public void Apply(string json)
	{
		string? assemblyName;
		string? runningTest;
		var results = new List<CompletedTestCase>();

		using (var document = JsonDocument.Parse(json))
		{
			var root = document.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				throw new JsonException("The progress update must be a JSON object.");
			}

			assemblyName = GetString(root, "assembly");
			runningTest = GetString(root, "running");

			if (root.TryGetProperty("results", out var resultsElement))
			{
				if (resultsElement.ValueKind != JsonValueKind.Array)
				{
					throw new JsonException("'results' must be an array.");
				}

				foreach (var item in resultsElement.EnumerateArray())
				{
					if (item.ValueKind != JsonValueKind.Object)
					{
						throw new JsonException("Each result must be a JSON object.");
					}

					var name = GetString(item, "name") ?? throw new JsonException("A result has no 'name'.");
					var result = GetString(item, "result") ?? throw new JsonException($"The result of '{name}' has no 'result'.");
					var duration = item.TryGetProperty("duration", out var durationElement) && durationElement.ValueKind == JsonValueKind.Number
						? durationElement.GetDouble()
						: 0;

					results.Add(new CompletedTestCase(name, result, duration, GetString(item, "message")));
				}
			}
		}

		lock (_gate)
		{
			_assemblyName = assemblyName ?? _assemblyName;
			_runningTest = runningTest;
			_completed.AddRange(results);
		}
	}

	private static string? GetString(JsonElement element, string propertyName)
	{
		if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
		{
			return null;
		}

		if (value.ValueKind != JsonValueKind.String)
		{
			throw new JsonException($"'{propertyName}' must be a string.");
		}

		try
		{
			return value.GetString();
		}
		catch (InvalidOperationException error) // e.g. an escaped lone surrogate, which cannot be converted to a string
		{
			throw new JsonException($"'{propertyName}' is not a valid string: {error.Message}", error);
		}
	}

	/// <summary>
	/// Gets the results of the completed tests plus a failed test case describing why the run did not complete,
	/// in the NUnit format the engine uses for the results of a complete run.
	/// </summary>
	public string ToNUnitXml(string failureName, string failureMessage)
	{
		CompletedTestCase[] testCases;
		string? assemblyName;
		lock (_gate)
		{
			testCases = [.. _completed, new CompletedTestCase(failureName, "Failed", 0, failureMessage)];
			assemblyName = _assemblyName;
		}

		var total = testCases.Length.ToString(CultureInfo.InvariantCulture);
		var passed = testCases.Count(testCase => testCase.Result == "Passed").ToString(CultureInfo.InvariantCulture);
		var failed = testCases.Count(testCase => testCase.IsFailure).ToString(CultureInfo.InvariantCulture);
		var skipped = testCases.Count(testCase => testCase.Result == "Skipped").ToString(CultureInfo.InvariantCulture);
		var resultsId = Guid.NewGuid().ToString();
		var now = DateTimeOffset.Now;

		object[] Summary() =>
		[
			new XAttribute("testcasecount", total),
			new XAttribute("result", "Failed"), // At least the test case describing why the run did not complete
			new XAttribute("time", "0"),
			new XAttribute("total", total),
			new XAttribute("errors", "0"),
			new XAttribute("passed", passed),
			new XAttribute("failed", failed),
			new XAttribute("inconclusive", "0"),
			new XAttribute("skipped", skipped),
			new XAttribute("asserts", "0"),
		];

		var testRun = new XElement(
			"test-run",
			new XAttribute("id", resultsId),
			new XAttribute("name", "Runtime Tests"),
			Summary(),
			new XAttribute("run-date", now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
			new XAttribute("start-time", now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)),
			new XAttribute("end-time", now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)),
			new XElement(
				"test-suite",
				new XAttribute("type", "Assembly"),
				new XAttribute("name", RemoveInvalidXmlChars(assemblyName ?? "Runtime Tests")),
				new XElement(
					"environment",
					new XAttribute("machine-name", Environment.MachineName),
					new XAttribute("platform", "n/a")),
				new XElement(
					"test-suite",
					new XAttribute("type", "TestFixture"),
					new XAttribute("name", resultsId),
					new XAttribute("executed", "true"),
					Summary(),
					testCases.Select(ToTestCaseElement))));

		using var writer = new Utf8StringWriter();
		new XDocument(new XDeclaration("1.0", "utf-8", null), testRun).Save(writer);

		return writer.ToString();
	}

	private static XElement ToTestCaseElement(CompletedTestCase testCase)
	{
		var name = RemoveInvalidXmlChars(testCase.Name);
		var element = new XElement(
			"test-case",
			new XAttribute("name", name),
			new XAttribute("fullname", name),
			new XAttribute("duration", testCase.Duration.ToString(CultureInfo.InvariantCulture)),
			new XAttribute("time", "0"),
			new XAttribute("result", RemoveInvalidXmlChars(testCase.Result)));

		if (testCase.IsFailure)
		{
			element.Add(new XElement("failure", new XElement("message", RemoveInvalidXmlChars(testCase.Message ?? ""))));
		}

		return element;
	}

	/// <summary>
	/// Removes the characters that cannot be written to an XML document (e.g. control characters from an exception message),
	/// which would otherwise fail writing the results.
	/// </summary>
	internal static string RemoveInvalidXmlChars(string text)
	{
		StringBuilder? sanitized = null;
		for (var i = 0; i < text.Length; i++)
		{
			if (XmlConvert.IsXmlChar(text[i]))
			{
				sanitized?.Append(text[i]);
			}
			else if (i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], text[i]))
			{
				sanitized?.Append(text, i, 2);
				i++;
			}
			else
			{
				sanitized ??= new StringBuilder(text, 0, i, text.Length);
			}
		}

		return sanitized?.ToString() ?? text;
	}

	private sealed record CompletedTestCase(string Name, string Result, double Duration, string? Message)
	{
		// Same as the engine, which reports the results of type Failed or Error as failures
		public bool IsFailure => Result is "Failed" or "Error";
	}

	private sealed class Utf8StringWriter : StringWriter
	{
		public Utf8StringWriter()
			: base(CultureInfo.InvariantCulture)
		{
		}

		public override Encoding Encoding => Encoding.UTF8;
	}
}
