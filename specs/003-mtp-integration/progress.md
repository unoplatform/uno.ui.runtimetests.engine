# 003 — Microsoft.Testing.Platform Integration: Implementation Progress

## Phase 1: Risk spike — Main-flow coexistence — DONE

| File | Action | Status |
|---|---|---|
| (scratchpad, not committed) standalone console app | Prove `host.Run()`-style blocking loop and `TestApplication.RunAsync()` can coexist in one process | Done |

Verified empirically (not just by reading docs): a background `Task` drives `TestApplication.CreateBuilderAsync → RegisterTestFramework → BuildAsync → RunAsync` while the calling thread blocks in a loop simulating Uno's native message pump; the resulting exit code is propagated via a hard `Environment.Exit` from the background task. Confirmed working for `--list-tests`, default run, a failing test (exit code `2`), `--filter-uid` (delivered as `TestNodeUidListFilter`), and a real `dotnet test` invocation (`--server dotnettestcli --dotnet-test-pipe <path>` argv). No hangs or deadlocks observed. Full notes are in [spec.md](spec.md)'s "Open Questions / Risks" section.

## Phase 2: Core implementation — DONE

| File | Action | Status |
|---|---|---|
| `src/global.json` | Restored `sdk` pinning block, added `test.runner: Microsoft.Testing.Platform` | Done |
| `src/Uno.UI.RuntimeTests.Engine.Library/Engine/ExternalRunner/UnoRuntimeTestsFramework.cs` | New `ITestFramework`/`IDataProducer` bridge (`UnoRuntimeTestsFramework`) + `MicrosoftTestingPlatformRunner.RunAsync` entry point | Done |
| `src/Uno.UI.RuntimeTests.Engine.Library/Uno.UI.RuntimeTests.Engine.Library.projitems` | Added the new file to the shared-items file list (explicit list, not a glob) | Done |
| `src/Uno.UI.RuntimeTests.Engine.Library/Engine/UI/UnitTestsControl.cs` | Widened `InitializeTests()` from `private` to `internal` so the bridge can enumerate tests for Discovery | Done |
| `src/Uno.UI.RuntimeTests.Engine.Library/Engine/ExternalRunner/RuntimeTestEmbeddedRunner.cs` | Widened several `private static` helpers (`WaitForCheckSafe`, `WaitForIdle`, `TryParseConfig`, `ApplyShardingFromEnvironment`, `ExitApplication`, `GetConfigValue`, `Log`, `LogError`) to `internal static` for reuse by the bridge | Done |
| `src/TestApp/Platforms/Desktop/Program.cs` | Added opt-in MTP branch, gated on `args.Length > 0` | Done |
| `src/TestApp/Uno.UI.RuntimeTests.Engine.TestApp.csproj` | Added `Microsoft.Testing.Platform`/`.MSBuild`/`Microsoft.Testing.Extensions.TrxReport` package refs, `IsTestingPlatformApplication`, `GenerateTestingPlatformEntryPoint=false`, `HAS_UNO_RUNTIMETESTS_MTP` define — all conditioned on `net10.0-desktop` only | Done |

Notes:
* The new bridge file is gated by `#if !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER && HAS_UNO_RUNTIMETESTS_MTP` — required because it's a shared-items file compiled into every TFM (android/ios/browserwasm too), but only the desktop TFM references the `Microsoft.Testing.Platform.*` packages its `using`s depend on.
* Discovery/filter Uids ended up needing to be method-level FQNs (`Namespace.Class.Method`), not per-`[DataRow]`-case display names as originally drafted in the spec — see "Course correction" below.

## Phase 3: Validation — DONE

| Check | Result |
|---|---|
| `dotnet build` desktop TFM | Succeeds |
| `dotnet build` browserwasm TFM | Succeeds, confirming the MTP code path compiles out cleanly on non-desktop TFMs |
| `TestApp.exe --list-tests` | 94 real tests discovered via reflection, correct exit code 0 |
| `TestApp.exe --filter-uid <FQN>` (no sibling-prefix collision) | Correctly narrows to exactly 1 test |
| `TestApp.exe --filter-uid <FQN>` (sibling-prefix collision, e.g. `Is_Sane` vs `Is_Sane_With_DynamicData`) | Runs both — confirmed as a **pre-existing** property of `UnitTestFilter`'s substring-based matching, not a bug introduced here (see Course correction below) |
| `dotnet test --project TestApp/....csproj -f net10.0-desktop` (full run, real end-to-end path) | 92 tests, 85 passed / 2 failed / 5 skipped, exit code `2` correctly reflects the 2 real failures, ~77s |
| Legacy env-var flow (`UNO_RUNTIME_TESTS_RUN_TESTS=... TestApp.exe`) after all changes | Unchanged — same NUnit XML output, same exit code, confirming NFR-001 |

## Course correction during implementation

The spec originally assumed Discovery/Run could share a per-`[DataRow]`-case Uid built from the same display-name convention as the existing NUnit output (`test.Name + testCase.ToString()`). Two problems surfaced only through actual testing (not caught by review of the design alone):

1. **`TestCaseResult` (used for Run reporting) has no class-name field** — only a bare method/display name. So Discovery (which has full type info) and Run (which doesn't) can't produce identical Uids for data-driven cases without deeper surgery. Resolved by keeping Discovery at **method-level granularity** (`Namespace.Class.Method`, matching the existing sharding engine's granularity) and disclosing that Run still reports one node per `[DataRow]` case using the bare display name — a known, bounded Discovery/Run correlation gap for parameterized tests only.
2. **Filter round-tripping was silently broken**: joining raw display-name Uids (e.g. `"Is_Sane()"`) straight into `UnitTestFilter`'s OR-expression syntax collided with the filter grammar's own use of `(`/`)` as grouping characters, degrading to a match-everything filter. Fixed by switching filter translation to the same method-level FQNs used for Discovery, which are safe (no special characters) and match exactly what `UnitTestFilter.IsMatch(MethodInfo)` compares against. Verified by manual testing before and after the fix.

Both are documented in spec.md's Edge Cases table.

## Not started / deferred (see spec.md Non-Goals and Open Questions)

* Real-time/streaming per-test progress (`InProgressTestNodeStateProperty`) — batched at the end of the run for this iteration.
* Mobile/WASM MTP support.
* `RunsInSecondaryAppAttribute` interaction with an MTP-hosted parent process — not exercised in manual validation (the full `dotnet test` run above did include `Is_SecondaryApp_Supported`/secondary-app-adjacent tests and they passed/skipped correctly, but the specific "parent process is itself under `--server` pipe mode while spawning a child" scenario hasn't been stress-tested).
* CI workflow wiring (`.github/workflows/dotnet.yml`) — README documents the usage, but no CI job exercises this path yet.
