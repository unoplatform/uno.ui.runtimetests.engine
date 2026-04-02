# Feature Specification: Test-Level Sharding

**Feature Branch**: `feature/002-sharding`
**Created**: 2026-04-02
**Status**: In Progress
**Depends on**: [000-wasm-runtime-tests](../000-wasm-runtime-tests/spec.md)

## Purpose

The runtime tests engine runs all tests sequentially within a single CI job. For large test suites this is a bottleneck. The existing class-level partitioning (`CITestGroup`/`CITestGroupCount` DependencyProperties) is:

- Only settable via XAML binding — not via env vars, CLI, or the embedded CI runner
- Coarse-grained (entire classes assigned to a shard, not individual test methods)
- Not wired into `RuntimeTestEmbeddedRunner` or the WASM runner

`ITestSharding` adds **test-method-level** deterministic sharding, accessible via CLI args and environment variables, for both desktop and WASM runtime tests

## Goals

- **G1**: Split test execution across N parallel CI jobs at the individual test method level
- **G2**: Provide deterministic, reproducible shard assignment (same test always runs on the same shard)
- **G3**: Support both desktop (env vars) and WASM (CLI + env vars) entry points
- **G4**: Auto-detect Azure Pipelines `parallel: N` matrix without explicit configuration
- **G5**: Maintain backward compatibility with existing `CITestGroup`/`CITestGroupCount`

## Non-Goals

- Replacing the existing `CITestGroup`/`CITestGroupCount` class-level partitioning
- Dynamic load-balancing based on test execution time
- Cross-shard test dependency resolution
- Splitting data-driven test rows (`[DataRow]`) across shards

## User Stories

### P1: CI Pipeline Sharding (WASM)

**As a** CI engineer
**I want to** split WASM runtime tests across N parallel jobs
**So that** CI wall-clock time decreases proportionally

**Acceptance Criteria:**
- Given a WASM test app with 100 test methods
- When I run `uno-runtimetests-wasm --shard-index 1 --total-shards 4`
- Then approximately 25 tests execute
- And the results file contains only those tests
- And the exit code reflects pass/fail for the shard

### P2: CI Pipeline Sharding (Desktop)

**As a** CI engineer
**I want to** split desktop runtime tests across parallel jobs using environment variables
**So that** I can use `strategy: parallel: N` in Azure Pipelines without script logic

**Acceptance Criteria:**
- Given the Azure Pipelines variables `System.JobPositionInPhase=2` and `System.TotalJobsInPhase=4`
- When the embedded runner starts with `UNO_RUNTIME_TESTS_RUN_TESTS=true`
- Then sharding is auto-detected from the pipeline variables
- And only the tests assigned to shard 2 execute

### P3: Empty Shard Handling

**As a** CI engineer
**I want** empty shards (more shards than tests) to exit cleanly
**So that** the CI pipeline does not fail spuriously

**Acceptance Criteria:**
- Given 5 test methods and `--total-shards 10`
- When shard 8 runs and receives 0 tests
- Then the process exits with code 0
- And the results file is valid (empty test run)

## Requirements

### FR-001: CLI Parameters (WASM Runner)

The WASM CLI tool SHALL accept optional `--shard-index <n>` and `--total-shards <n>` parameters. Both are 1-based. Both must be provided together or omitted together.

### FR-002: Environment Variables

The runtime library SHALL read `UNO_RUNTIME_TESTS_SHARD_INDEX` and `UNO_RUNTIME_TESTS_TOTAL_SHARDS` to configure sharding. Values are 1-based.

### FR-003: Azure Pipelines Fallback

When the explicit env vars are absent, the runtime library SHALL fall back to `SYSTEM_JOBPOSITIONINPHASE` and `SYSTEM_TOTALJOBSINPHASE` (both 1-based).

### FR-004: Deterministic Assignment

Test assignment SHALL be deterministic: given the same `(testFullName, totalShards)`, the result is always the same shard index. The algorithm is `SHA1(FullyQualifiedMethodName) % totalShards`.

### FR-005: Test-Level Granularity

Sharding SHALL operate at the individual test method level, not the test class level. All `[DataRow]` cases for a method run on the same shard.

### FR-006: Composability with Filters

Sharding SHALL compose with the existing `UnitTestFilter` system. Filter is applied first (reducing the test set), then sharding selects from the filtered set.

### FR-007: JSON Config Support

`UnitTestEngineConfig` JSON deserialization SHALL support `ShardIndex` and `TotalShards` properties, allowing sharding via the `UNO_RUNTIME_TESTS_RUN_TESTS` JSON config path.

### NFR-001: Distribution Uniformity

SHA1-based hashing SHALL provide statistically uniform distribution. For N tests across M shards, each shard SHALL have approximately N/M tests (within expected hash-collision variance).

### NFR-002: No Performance Overhead When Disabled

When no sharding is configured, the execution path SHALL have zero additional overhead (no hashing, no filtering).

## Technical Design

### Algorithm: Hash-Based Modulo

SHA1 hash of the fully-qualified test method name (`Namespace.Class.Method`) modulo `totalShards`. This approach:

- Is deterministic regardless of test discovery order
- Fits the existing per-class iteration architecture (no two-pass refactor)
- Provides statistically uniform distribution
- Matches the existing `GetTypeTestGroup` pattern (SHA1 + modulo) already in the codebase

**Public API is 1-based** (`--shard-index 1` through `N`), converted to 0-based internally at the boundary.

### Data Flow

```
CLI args (WASM runner)          Environment variables              Azure Pipelines
  --shard-index 2          UNO_RUNTIME_TESTS_SHARD_INDEX=2    SYSTEM_JOBPOSITIONINPHASE=2
  --total-shards 4         UNO_RUNTIME_TESTS_TOTAL_SHARDS=4   SYSTEM_TOTALJOBSINPHASE=4
         │                            │                                │
         └─────────────┬──────────────┘                                │
                       │ (fallback chain: CLI > env var > Azure)       │
                       ▼                                               │
              UnitTestEngineConfig                                     │
                ShardIndex: 1  (0-based)  ◄────────────────────────────┘
                TotalShards: 4
                       │
                       ▼
              UnitTestsControl.ExecuteTestsForInstance()
                → IsTestInShard(class, method, shardIndex, totalShards)
                → SHA1(FullName) % totalShards == shardIndex
```

### Execution Order

1. `InitializeTests()` — discovers test classes (legacy `CITestGroup` filter applies here if set)
2. `ExecuteTestsForInstance()` — per class:
   a. Apply `UnitTestFilter` (existing `--filter` / `UNO_RUNTIME_TESTS_RUN_TESTS`)
   b. **NEW**: Apply shard filter (`IsTestInShard`)
   c. Execute remaining tests

### Interaction with Legacy Partitioning

The old `CITestGroup`/`CITestGroupCount` operates at class enumeration time in `InitializeTests()`. The new test-level sharding operates later, inside `ExecuteTestsForInstance`. If both are active:
1. `InitializeTests()` filters classes by the old class-level grouping
2. `ExecuteTestsForInstance` then further filters individual tests by the new shard assignment

This is composable but not recommended — the new system is the preferred path.

### Files to Modify

1. **`src/Uno.UI.RuntimeTests.Engine.Library/Engine/UnitTestEngineConfig.cs`**
   - Add `ShardIndex` (int?, 0-based) and `TotalShards` (int?) properties
   - Both null = sharding disabled
   - JSON-serializable automatically since it's a record

2. **`src/Uno.UI.RuntimeTests.Engine.Library/Engine/ExternalRunner/RuntimeTestEmbeddedRunner.cs`**
   - New method `ApplyShardingFromEnvironment(config)`:
     - Read `UNO_RUNTIME_TESTS_SHARD_INDEX` / `UNO_RUNTIME_TESTS_TOTAL_SHARDS` via `GetConfigValue()`
     - Fallback to `SYSTEM_JOBPOSITIONINPHASE` / `SYSTEM_TOTALJOBSINPHASE`
     - Convert 1-based → 0-based, validate ranges
     - Merge into config with `config with { ... }`
     - Skip if JSON config already has sharding set

3. **`src/Uno.UI.RuntimeTests.Engine.Library/Engine/UI/UnitTestsControl.cs`**
   - Add `IsTestInShard(Type, UnitTestMethodInfo, int, int)` static method near `GetTypeTestGroup`
   - Insert shard filtering in `ExecuteTestsForInstance` after `Filter` application, before the `tests.Length <= 0` guard

4. **`src/Uno.UI.RuntimeTests.Engine.Wasm.Runner/Program.cs`**
   - Add `--shard-index` and `--total-shards` `Option<int?>` CLI options
   - Resolve with fallback chain (CLI → env var → Azure Pipelines)
   - Inject as `UNO_RUNTIME_TESTS_SHARD_INDEX`/`UNO_RUNTIME_TESTS_TOTAL_SHARDS` in `injectedEnvVars` dict and URL query params

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| Empty shard (0 tests assigned) | Runs zero tests, exits successfully (code 0) |
| `totalShards=1` | Sharding skipped entirely (`> 1` guard) |
| Sharding + Filter | Filter applied first, then sharding selects from filtered set |
| Data-driven tests (`[DataRow]`) | All rows for a method run on same shard |
| Old `CITestGroup` + new sharding | Both apply (class-level first, then test-level) |
| Invalid shard index (out of range) | Logged as error, sharding disabled, all tests run |
| Non-integer env var values | Logged as error, sharding disabled, all tests run |

## Success Criteria

| Metric | Target |
|--------|--------|
| Deterministic assignment | Same test → same shard across runs (100%) |
| Union of all shards covers all tests | 100% (no test orphaned) |
| Intersection of any two shards is empty | 0 tests in common |
| Distribution uniformity (100 tests, 4 shards) | Each shard within 20-30 tests |
| CLI backward compatibility | 100% (no breaking changes) |
| No performance overhead when disabled | 0 extra allocations |

## Usage

### WASM CLI

```bash
uno-runtimetests-wasm run \
  --app-path ./publish --output results.xml \
  --shard-index 2 --total-shards 4
```

### Desktop (Explicit Env Vars)

```bash
UNO_RUNTIME_TESTS_SHARD_INDEX=1 UNO_RUNTIME_TESTS_TOTAL_SHARDS=4 \
  UNO_RUNTIME_TESTS_RUN_TESTS=true UNO_RUNTIME_TESTS_OUTPUT_PATH=results.xml \
  dotnet run
```

### Azure Pipelines (Auto-Detected)

```yaml
strategy:
  parallel: 4
steps:
  - script: |
      uno-runtimetests-wasm run \
        --app-path $(appPath) \
        --output $(Build.ArtifactStagingDirectory)/results-$(System.JobPositionInPhase).xml \
        --shard-index $(System.JobPositionInPhase) \
        --total-shards $(System.TotalJobsInPhase)
```

Or without explicit args (auto-detection):

```yaml
strategy:
  parallel: 4
steps:
  - script: |
      uno-runtimetests-wasm run \
        --app-path $(appPath) \
        --output $(Build.ArtifactStagingDirectory)/results.xml
    # System.JobPositionInPhase / System.TotalJobsInPhase picked up automatically
```

### JSON Config

```bash
UNO_RUNTIME_TESTS_RUN_TESTS='{"ShardIndex":0,"TotalShards":4}' \
  UNO_RUNTIME_TESTS_OUTPUT_PATH=results.xml dotnet run
```

## Verification Steps

1. Build in Release mode — zero CS compilation errors/warnings
2. Run with `--shard-index 1 --total-shards 2` and `--shard-index 2 --total-shards 2` — verify union covers all tests and intersection is empty
3. Run WASM runner with `--shard-index`/`--total-shards` — verify subset executes
4. Run without sharding args — verify all tests execute (backward compat)
5. Run with `--total-shards` greater than test count — verify empty shards exit 0
6. Set `SYSTEM_JOBPOSITIONINPHASE`/`SYSTEM_TOTALJOBSINPHASE` — verify auto-detection works
