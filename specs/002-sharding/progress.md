# 002 — Test-Level Sharding: Implementation Progress

## Phase 1: Core Engine + CLI — IN PROGRESS

| File | Action | Status |
|------|--------|--------|
| `Engine/UnitTestEngineConfig.cs` | MODIFY — add `ShardIndex`, `TotalShards` nullable int properties | Done |
| `Engine/ExternalRunner/RuntimeTestEmbeddedRunner.cs` | MODIFY — add `ApplyShardingFromEnvironment()` with env var + Azure fallback | Done |
| `Engine/UI/UnitTestsControl.cs` | MODIFY — add `IsTestInShard()`, shard filtering in `ExecuteTestsForInstance` | Done |
| `Wasm.Runner/Program.cs` | MODIFY — add `--shard-index`/`--total-shards` CLI options, env var + query param injection | Done |

**Build**: No CS compilation errors. NuGet restore unavailable in dev environment (network). Pre-existing infra issues only (missing Android workload, package feed timeout).

**Pending validation**: End-to-end desktop and WASM runtime test with actual sharding.

---

## Phase 2: Unit Tests — IN PROGRESS

| File | Action | Status |
|------|--------|--------|
| `Wasm.Runner.Tests/ShardingAlgorithmTests.cs` | NEW — 8 tests validating sharding algorithm properties | Done |

Tests validate:
- `AllTestsCovered_NoTestOrphaned` — union of all shards covers every test
- `NoOverlap_TestAssignedToExactlyOneShard` — no test appears in two shards
- `Deterministic_SameTestAlwaysSameShard` — same input → same shard
- `Distribution_RoughlyUniform` — 1000 tests / 4 shards within +-15% of expected
- `SingleShard_AllTestsIncluded` — totalShards=1 → all in shard 0
- `MoreShardsThanTests_EmptyShardsAllowed` — 3 tests / 10 shards works
- `StableAcrossShardCounts_SameHashDifferentModulo` — valid index for all shard counts
- `DifferentMethodsSameClass_CanLandOnDifferentShards` — method-level, not class-level

**Build/Run**: NuGet restore unavailable in dev environment (network). Tests will run in CI.

---

## Phase 3: Integration Verification — NOT STARTED

- Run desktop runtime tests with `UNO_RUNTIME_TESTS_SHARD_INDEX=1 UNO_RUNTIME_TESTS_TOTAL_SHARDS=2`
- Run WASM runtime tests with `--shard-index 1 --total-shards 2`
- Verify empty shard exits cleanly (code 0)
- Verify backward compat (no shard args → all tests run)
