using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Engine
{
	[TestClass]
	public class TestContextTests
	{
		public TestContext TestContext
		{
			get => _testContext ?? throw new InvalidOperationException($"TestContext has not been set!");
			set => _testContext = value;
		}
		private TestContext? _testContext;

		private TestContext? previousTestContext;

		private bool RunningWithMSTest => !(TestContext.GetType().FullName?.StartsWith("Uno.", StringComparison.OrdinalIgnoreCase) ?? true);

		private bool initializeRan;
		private bool testContextSetBeforeInitialize;
		private object? testContext_atTestInitialize;
		private object? testContext_CancellationTokenSource;

		[TestInitialize]
		public void TestInitialize()
		{
			initializeRan = true;
			testContextSetBeforeInitialize = _testContext is not null;
			testContext_atTestInitialize = TestContext;
			testContext_CancellationTokenSource = TestContext.CancellationTokenSource;
		}

		[TestCleanup]
		public void TestCleanup()
		{
			if (!initializeRan)
			{
				throw new InvalidOperationException($"[TestInitialize] methods must be called before [TestCleanup].");
			}
			if (!object.ReferenceEquals(TestContext, testContext_atTestInitialize))
			{
				throw new InvalidOperationException($"TestContext instance MUST be the same between [TestInitialize] and [TestCleanup].");
			}
			if (object.ReferenceEquals(TestContext.CancellationTokenSource, testContext_CancellationTokenSource))
			{
				throw new InvalidOperationException($"TestContext.CancellationTokenSource MUST be changed between [TestMethod] invocation and [TestCleanup] invocation.");
			}
			// TestContext.CancellationToken should not throw.
			_ = TestContext.CancellationToken;

			previousTestContext = TestContext;

			initializeRan = false;
			testContextSetBeforeInitialize = false;
			testContext_atTestInitialize = null;
			testContext_CancellationTokenSource = null;
		}

		private void AssertSameInstanceUsedAcrossTests()
		{
			if (RunningWithMSTest)
			{
				// MSTest uses a separate instance for *each* [TestMethod] invocation.
				Assert.IsNull(previousTestContext);
				return;
			}
			if (previousTestContext is not null)
			{
			    Assert.AreNotSame(previousTestContext, TestContext);
			}
		}

		private void Tests()
		{
			AssertSameInstanceUsedAcrossTests();
			Assert.IsTrue(initializeRan, "[TestInitialize] method was not called before [TestMethod].");
			Assert.IsTrue(testContextSetBeforeInitialize, "[TestContext] was not set before [TestMethod].");
			Assert.AreSame(testContext_CancellationTokenSource, TestContext.CancellationTokenSource);
		}

		[TestMethod]
		public void Test1()
		{
			Tests();
		}

		[TestMethod]
		public void Test2()
		{
			Tests();
		}
	}
}
