using System.Runtime.CompilerServices;

namespace MSTestLifcycleTests;

// Use non-Async() TextWriter.WriteLine() invocations, as we intermix between async and non-async contexts.
#pragma warning disable CA1849

[TestClass]
public sealed class LifecycleLogging
{
	static readonly TextWriter _o = File.AppendText("/tmp/mstest-events.txt");

	public LifecycleLogging(TestContext testContext)
	{
		var message = $"# jonp: LifecycleLogging..ctor: {GetDescription(this)} {GetDescription(testContext)}";
		testContext.WriteLine(message);
		_o.WriteLine(message);
	}

	private object? _testContext;
	public TestContext TestContext
	{
		get => (TestContext)(_testContext ?? throw new Exception("TestContext should have been set!"));
		set
		{
			var message = $"# jonp: set_TestContext: {GetDescription(this)} {GetDescription(value)} {GetDescription(_testContext)} {GetDescription(referenceTestContext)}";
			_testContext = value;
			TestContext.WriteLine(message);
			_o.WriteLine(message);
		}
	}

	private TestContext? referenceTestContext;

	static string GetDescription(object? value, [CallerArgumentExpression(nameof(value))] string description = "")
		=> value == null
		? $"{description}(0)"
		: $"{description}(0x{RuntimeHelpers.GetHashCode(value).ToString("x2")})";

	[AssemblyInitialize]
	public static async Task AssemblyInit(TestContext testContext)
	{
		var message = $"# jonp: AssemblyInit! {GetDescription(testContext)}";
		testContext.WriteLine(message);
		_o.WriteLine(message);
	}

	[AssemblyCleanup]
	public static async Task AssemblyCleanup(TestContext testContext)
	{
		var message = $"# jonp: AssemblyCleanup! {GetDescription(testContext)}";
		testContext.WriteLine(message);
		_o.WriteLine(message);
		_o.Flush();
	}

	[ClassInitialize]
	public static async Task ClassInit(TestContext testContext)
	{
		var message = $"# jonp: ClassInit! {GetDescription(testContext)}";
		testContext.WriteLine(message);
		_o.WriteLine(message);
	}

	[ClassCleanup]
	public static async Task ClassCleanup(TestContext testContext)
	{
		var message = $"# jonp: ClassCleanup! {GetDescription(testContext)}";
		testContext.WriteLine(message);
		_o.WriteLine(message);
	}

	[TestInitialize]
	public async Task TestInit()
	{
		var message = $"# jonp: TestInit! {GetDescription(this)} {GetDescription(referenceTestContext)}, {GetDescription(TestContext)}, {GetDescription(TestContext.CancellationTokenSource)}";
		TestContext.WriteLine(message);
		referenceTestContext = TestContext;
		_o.WriteLine(message);
	}

	[TestCleanup]
	public async Task TestCleanup()
	{
		var message = $"# jonp: TestCleanup! {GetDescription(this)} {GetDescription(referenceTestContext)}, {GetDescription(TestContext)}, {GetDescription(TestContext.CancellationTokenSource)}";
		TestContext.WriteLine(message);
		_o.WriteLine(message);
	}


	[TestMethod]
	public void TestMethod1()
	{
		var message = $"# jonp: TestMethod1! {GetDescription(this)} {GetDescription(TestContext)}, {GetDescription(TestContext.CancellationTokenSource)}";
		TestContext.WriteLine(message);
		_o.WriteLine(message);
	}

	[TestMethod]
	public void TestMethod2()
	{
		var message = $"# jonp: TestMethod2! {GetDescription(this)} {GetDescription(TestContext)}, {GetDescription(TestContext.CancellationTokenSource)}";
		TestContext.WriteLine(message);
		_o.WriteLine(message);
	}
}
