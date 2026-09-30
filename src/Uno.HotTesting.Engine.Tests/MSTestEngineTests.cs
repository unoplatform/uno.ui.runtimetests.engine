using System.Reflection;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace Uno.HotTesting.Engine.Tests;

[TestClass]
public sealed class MSTestEngineTests
{
	[TestMethod]
	public async Task DiscoverTestsAsync()
	{
		var engine = new MSTestEngine([typeof(MSTestEngineTests).Assembly]);
		var discovered = await engine.DiscoverTestsAsync();
		foreach (var d in discovered)
		{
			// # jonp: DiscoverTestsAsync: found: TestNode {
			//   Uid = TestNodeUid {
			//     Value = 14ea02ee-f440-8d32-9f16-5e1d5669f0ce
			//   },
			//   DisplayName = RunTestsAsync,
			//   Properties = [
			//     TestMethodIdentifierProperty {
			//       AssemblyFullName = ,
			//       Namespace = Uno.HotTesting.Engine.Tests,
			//       TypeName = MSTestEngineTests,
			//       MethodName = RunTestsAsync,
			//       MethodArity = 0,
			//       ParameterTypeFullNames = [],
			//       ReturnTypeFullName =
			//     },
			//     TestFileLocationProperty {
			//       FilePath = …/MSTestEngineTests.cs,
			//       LineSpan = LinePositionSpan {
			//         Start = LinePosition {
			//           Line = …,
			//           Column = …
			//         },
			//         End = LinePosition {
			//           Line = …,
			//           Column = …
			//         }
			//       }
			//     },
			//     DiscoveredTestNodeStateProperty { Explanation =  }
			//   ]
			// }
			Console.WriteLine($"# jonp: DiscoverTestsAsync: found: {d}");
		}
		// Filter by tests found within `NestedType` so that we don't need to change the assert count
		// whenever a new test method is added.
		var inNested = discovered.Where(TestDeclaredInNestedTests).ToList();
		Assert.AreEqual(2, inNested.Count);
	}

	[TestMethod]
	public async Task RunTestsAsync()
	{
		var engine = new MSTestEngine([typeof(MSTestEngineTests).Assembly]);
		var discovered = await engine.DiscoverTestsAsync();

		// Filter by tests found within `NestedType` so that we don't re-run all `MSTestEngineTests` recursively!
		var inNested = discovered.Where(TestDeclaredInNestedTests).ToList();

		var ran = await engine.RunTestsAsync(inNested);
		foreach (var d in ran)
		{
			// # jonp: RunTestsAsync: ran: TestNode {
			//   Uid = TestNodeUid {
			//     Value = 1c1255ad-3d94-8be7-bcb9-1d9d51bca9ed
			//   },
			//   DisplayName = A,
			//   Properties = [
			//     TestMethodIdentifierProperty {
			//       AssemblyFullName = ,
			//       Namespace = Uno.HotTesting.Engine.Tests,
			//       TypeName = NestedTests,
			//       MethodName = A,
			//       MethodArity = 0,
			//       ParameterTypeFullNames = [],
			//       ReturnTypeFullName = 
			//     },
			//     TestFileLocationProperty { … },
			//     InProgressTestNodeStateProperty { Explanation =  }
			//   ]
			// }
			// # jonp: RunTestsAsync: ran: TestNode {
			//   Uid = TestNodeUid {
			//     Value = 1c1255ad-3d94-8be7-bcb9-1d9d51bca9ed
			//   },
			//   DisplayName = A,
			//   Properties = [
			//     TimingProperty {
			//       GlobalTiming = TimingInfo {
			//         StartTime = 09/24/2026 07:30:11 -04:00,
			//         EndTime = 09/24/2026 07:30:11 -04:00, Duration = 00:00:00.0000974
			//       },
			//       StepTimings = []
			//     },
			//     TestMethodIdentifierProperty {
			//       AssemblyFullName = ,
			//       Namespace = Uno.HotTesting.Engine.Tests,
			//       TypeName = NestedTests,
			//       MethodName = A,
			//       MethodArity = 0,
			//       ParameterTypeFullNames = [],
			//       ReturnTypeFullName = 
			//     },
			//     TestFileLocationProperty { … },
			//     PassedTestNodeStateProperty {
			//       Explanation = 
			//     }
			//   ]
			// }
			Console.WriteLine($"# jonp: RunTestsAsync: ran: {d}");
		}
		// Each successfully executed test results in (at least?) two nodes:
		// one with an `InProgressTestNodeStateProperty`,
		// and one with a `PassedTestNodeStateProperty` + `TimingProperty`.
		Assert.AreEqual(2*inNested.Count, ran.Count);
	}

	static bool TestDeclaredInNestedTests(TestNode node)
	{			
		if (node.Properties.SingleOrDefault<TestMethodIdentifierProperty>() is TestMethodIdentifierProperty id)
		{
			return id.Namespace == "Uno.HotTesting.Engine.Tests" &&
				id.TypeName == nameof(NestedTests);
		}
		return false;
	}
}

// Two test methods to verify that `RunTestsAsync()` runs all tests.
[TestClass]
public sealed class NestedTests
{
	[TestMethod]
	public void A()
	{
	}

	[TestMethod]
	public void B()
	{
	}
}
