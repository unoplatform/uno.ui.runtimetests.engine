using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Engine.Wasm.Runner.Tests;

/// <summary>
/// Validates the test-level sharding algorithm used by UnitTestsControl.IsTestInShard.
/// The algorithm: SHA1(fullyQualifiedTestName) % totalShards == shardIndex.
/// These tests verify the mathematical properties without requiring Uno Platform dependencies.
/// </summary>
[TestClass]
public class ShardingAlgorithmTests
{
	/// <summary>
	/// Replicates the exact sharding logic from UnitTestsControl.IsTestInShard
	/// so we can validate its properties in a unit test.
	/// </summary>
	private static int GetShardForTest(string testFullName, int totalShards)
	{
		using var sha1 = SHA1.Create();
		var buffer = Encoding.UTF8.GetBytes(testFullName);
		var hash = sha1.ComputeHash(buffer);
		return (int)(BitConverter.ToUInt64(hash, 0) % (ulong)totalShards);
	}

	[TestMethod]
	public void AllTestsCovered_NoTestOrphaned()
	{
		// Every test must be assigned to exactly one shard
		var testNames = GenerateTestNames(200);
		var totalShards = 4;

		var assignedTests = new HashSet<string>();
		for (var shard = 0; shard < totalShards; shard++)
		{
			foreach (var name in testNames)
			{
				if (GetShardForTest(name, totalShards) == shard)
				{
					assignedTests.Add(name);
				}
			}
		}

		Assert.AreEqual(testNames.Count, assignedTests.Count,
			"Union of all shards must cover every test");
	}

	[TestMethod]
	public void NoOverlap_TestAssignedToExactlyOneShard()
	{
		var testNames = GenerateTestNames(200);
		var totalShards = 4;

		foreach (var name in testNames)
		{
			var assignedShards = new List<int>();
			for (var shard = 0; shard < totalShards; shard++)
			{
				if (GetShardForTest(name, totalShards) == shard)
				{
					assignedShards.Add(shard);
				}
			}

			Assert.AreEqual(1, assignedShards.Count,
				$"Test '{name}' should be in exactly 1 shard, but found in {assignedShards.Count}");
		}
	}

	[TestMethod]
	public void Deterministic_SameTestAlwaysSameShard()
	{
		var testName = "MyNamespace.MyClass.MyTestMethod";
		var totalShards = 4;

		var first = GetShardForTest(testName, totalShards);
		var second = GetShardForTest(testName, totalShards);
		var third = GetShardForTest(testName, totalShards);

		Assert.AreEqual(first, second);
		Assert.AreEqual(second, third);
	}

	[TestMethod]
	public void Distribution_RoughlyUniform()
	{
		// With 1000 tests and 4 shards, each shard should get ~250 tests.
		// Allow a generous +-15% tolerance for hash distribution variance.
		var testNames = GenerateTestNames(1000);
		var totalShards = 4;
		var counts = new int[totalShards];

		foreach (var name in testNames)
		{
			counts[GetShardForTest(name, totalShards)]++;
		}

		var expected = testNames.Count / totalShards; // 250
		var tolerance = expected * 0.15; // +-37

		for (var i = 0; i < totalShards; i++)
		{
			Assert.IsTrue(
				counts[i] >= expected - tolerance && counts[i] <= expected + tolerance,
				$"Shard {i} has {counts[i]} tests, expected ~{expected} (tolerance +-{tolerance:F0})");
		}
	}

	[TestMethod]
	public void SingleShard_AllTestsIncluded()
	{
		var testNames = GenerateTestNames(50);
		var totalShards = 1;

		foreach (var name in testNames)
		{
			Assert.AreEqual(0, GetShardForTest(name, totalShards),
				$"With totalShards=1, all tests must be in shard 0");
		}
	}

	[TestMethod]
	public void MoreShardsThanTests_EmptyShardsAllowed()
	{
		var testNames = GenerateTestNames(3);
		var totalShards = 10;
		var nonEmptyShards = 0;

		for (var shard = 0; shard < totalShards; shard++)
		{
			var count = testNames.Count(name => GetShardForTest(name, totalShards) == shard);
			if (count > 0)
			{
				nonEmptyShards++;
			}
		}

		// At most 3 shards can be non-empty (one per test)
		Assert.IsTrue(nonEmptyShards <= testNames.Count,
			$"With {testNames.Count} tests and {totalShards} shards, at most {testNames.Count} shards should be non-empty");
		// At least 1 shard must be non-empty
		Assert.IsTrue(nonEmptyShards >= 1, "At least one shard must have tests");
	}

	[TestMethod]
	public void StableAcrossShardCounts_SameHashDifferentModulo()
	{
		// Changing totalShards should redistribute but not crash or produce invalid indices
		var testName = "Some.Namespace.TestClass.MethodA";

		for (var totalShards = 1; totalShards <= 20; totalShards++)
		{
			var shard = GetShardForTest(testName, totalShards);
			Assert.IsTrue(shard >= 0 && shard < totalShards,
				$"Shard index {shard} out of range for totalShards={totalShards}");
		}
	}

	[TestMethod]
	public void DifferentMethodsSameClass_CanLandOnDifferentShards()
	{
		// Verifies that sharding is at method level, not class level.
		// With enough methods, at least 2 different shards should be hit.
		var totalShards = 4;
		var shards = new HashSet<int>();

		for (var i = 0; i < 20; i++)
		{
			shards.Add(GetShardForTest($"MyNamespace.MyClass.Method{i}", totalShards));
		}

		Assert.IsTrue(shards.Count > 1,
			"Methods from the same class should distribute across multiple shards");
	}

	private static List<string> GenerateTestNames(int count)
	{
		var names = new List<string>(count);
		for (var i = 0; i < count; i++)
		{
			// Simulate realistic fully-qualified test method names
			var ns = i % 5 switch
			{
				0 => "StudioLive.Worker.Tests",
				1 => "StudioLive.Agent.Tests",
				2 => "StudioLive.Build.Tests",
				3 => "StudioLive.Conversation.Tests",
				_ => "StudioLive.Roslyn.Tests"
			};
			var cls = $"TestClass{i / 10}";
			var method = $"When_Scenario{i}_Then_ExpectedBehavior";
			names.Add($"{ns}.{cls}.{method}");
		}

		return names;
	}
}
