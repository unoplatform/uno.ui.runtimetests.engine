# Feature Specification: Microsoft.Testing.Platform (MTP) Integration

**Feature Branch**: `dev/jonpryor/jonp-MTP-integration`
**Created**: 2026-07-09
**Status**: Draft
**Depends on**: none (interacts with [002-sharding](../002-sharding/spec.md) filter/shard composition)

## Purpose

Today the runtime-tests engine has no relationship to `dotnet test`, VSTest, or Microsoft.Testing.Platform (MTP). Tests run by launching the built app directly with `UNO_RUNTIME_TESTS_*` environment variables (desktop/mobile) or via the standalone `uno-runtimetests-wasm` tool (WASM); a `[ModuleInitializer]` (`RuntimeTestEmbeddedRunner.AutoStartTests`) detects the env vars, runs the hand-rolled reflection-based engine, and writes an NUnit-XML-shaped report before force-exiting the process.

MTP is Microsoft's lightweight, VSTest-independent test runner protocol (`Microsoft.Testing.Platform` core + `Microsoft.Testing.Platform.MSBuild` for `dotnet test`/IDE integration). Since .NET 10 SDK, `dotnet test` has a dedicated "MTP mode" activated via `global.json`'s `"test": {"runner": "Microsoft.Testing.Platform"}`. This feature adds an **opt-in** MTP host to the TestApp's Desktop (Skia) head so that `dotnet test`/`dotnet run` and MTP-aware IDE Test Explorers can discover and run individual runtime tests, reporting results through the standard MTP protocol — without touching the existing env-var/NUnit-XML flow used by mobile, WASM, and any consumer who doesn't opt in.

## Goals

- **G1**: The Desktop head (`net*-desktop` TFM) can be discovered and run via `dotnet test`/`dotnet run` under MTP mode.
- **G2**: Each runtime test method is reported individually to MTP's message bus (`TestNodeUpdateMessage`) — not just as one aggregate NUnit XML file.
- **G3**: `dotnet test --filter` / IDE "run selected tests" map onto the existing `UnitTestFilter` engine, reusing its parser rather than duplicating filter logic.
- **G4**: Purely opt-in — the existing `UNO_RUNTIME_TESTS_*` env-var + NUnit-XML flow (desktop, mobile, WASM) is unchanged for anyone who doesn't add the new package references.
- **G5**: Exit-code semantics stay correct, reusing the existing Linux `_exit()` workaround (`RuntimeTestEmbeddedRunner.ExitApplication`, fixed under commit `3e87673`).
- **G6**: Document the opt-in steps in `README.md`, matching the style of existing `DefineConstants`-based opt-ins (e.g. `UNO_RUNTIMETESTS_DISABLE_UI`).

## Non-Goals

- Mobile (Android/iOS) or WASM MTP support — these platforms aren't naturally invoked via `dotnet test`'s "launch the built exe" process model. WASM keeps using `uno-runtimetests-wasm`.
- Replacing or deprecating the env-var + NUnit-XML invocation model.
- Real-time/streaming per-test progress (`InProgressTestNodeStateProperty`) while a test is executing — the MVP reports results in one batch once `UnitTestsControl.RunTests` completes.
- Full VSTest-mode `dotnet test` compatibility — only the newer MTP mode of `dotnet test` (via `global.json`'s `test.runner`, .NET 10 SDK+) is targeted. (A separate, stale spike branch, `dev/jonpryor/jonp-dotnet-test-integration`, explored the legacy VSTest-mode MSBuild-target hook; not reused here.)
- Live attach-and-debug of individual runtime tests from Test Explorer.
- Validating `RunsInSecondaryAppAttribute` tests (which spawn a child process of the same exe) under an MTP-hosted parent process — flagged as an open risk below, not solved in this iteration.

## User Stories

### P1: Local `dotnet test` / `dotnet run`

**As a** library developer
**I want to** run `dotnet test` against the TestApp Desktop head
**So that** I get pass/fail feedback in my terminal without manually setting env vars or parsing NUnit XML

**Acceptance Criteria:**
- Given `src/global.json` configured with `"test": {"runner": "Microsoft.Testing.Platform"}`
- When I run `dotnet test src/TestApp/Uno.UI.RuntimeTests.Engine.TestApp.csproj -f net10.0-desktop`
- Then the TestApp launches headlessly, executes all runtime tests, and reports one pass/fail/skip result per test method
- And the process exits `0` if all tests passed, non-zero otherwise

### P2: IDE Test Explorer discovery

**As a** developer using VS Code or Visual Studio
**I want** the runtime tests to show up in Test Explorer
**So that** I can run/filter individual tests without leaving the IDE

**Acceptance Criteria:**
- Given the Desktop head project references `Microsoft.Testing.Platform.MSBuild`
- When Test Explorer requests discovery (`DiscoverTestExecutionRequest`)
- Then each `[TestMethod]` appears as a distinct, stably-identified test node
- And running a selected subset only executes those tests

### P3: CI via `dotnet test`

**As a** CI engineer
**I want to** invoke runtime tests through the standard `dotnet test` command
**So that** CI tooling (trx reports, standard exit codes) works the same way it does for ordinary unit-test projects

**Acceptance Criteria:**
- Given the Desktop head references `Microsoft.Testing.Extensions.TrxReport`
- When I run `dotnet test src/MyApp/MyApp.csproj -f net10.0-desktop --report-trx`
- Then a `.trx` file is produced summarizing pass/fail per test
- And CI can publish results the same way it does for the org's other `dotnet test`-based projects, instead of `dorny/test-reporter` + NUnit XML

### P4: Backward compatibility

**As an** existing consumer of Uno.UI.RuntimeTests.Engine
**I want** my current CI pipeline (env vars + NUnit XML) to keep working unmodified
**So that** adopting the new package version does not force me onto MTP

**Acceptance Criteria:**
- Given a consumer app that has not added the MTP opt-in (no `Microsoft.Testing.Platform.MSBuild` reference, no `Program.cs` change)
- When they build and run their app with `UNO_RUNTIME_TESTS_RUN_TESTS` as before
- Then behavior is identical to before this feature existed

## Requirements

### FR-001: Opt-in MTP host wiring

The Desktop head SHALL gain a new, explicitly opt-in code path in `Program.cs` that constructs and runs an `ITestFramework`-backed `TestApplication` when invoked in MTP mode, alongside the existing `UnoPlatformHostBuilder` / `host.Run()` startup. This cannot be a `[ModuleInitializer]` like the existing engine, because MTP's `TestApplication.CreateBuilderAsync(args)` needs `Main`'s actual `args`, which module initializers never receive.

### FR-002: Bridging test framework

The Library SHALL provide an `ITestFramework` + `IDataProducer` implementation (e.g. `UnoRuntimeTestsFramework`) that:
- On `DiscoverTestExecutionRequest`, enumerates test classes/methods using the same reflection scan as `UnitTestsControl.InitializeTests()`, publishing one `TestNode` per discovered test (`Uid` = `Namespace.Class.Method`, matching the FQN convention already established for sharding in [002-sharding](../002-sharding/spec.md)) with `DiscoveredTestNodeStateProperty`.
- On `RunTestExecutionRequest`, waits for the app window/dispatcher using the same sequence as `RuntimeTestEmbeddedRunner.RunTestsAndExit`, invokes `UnitTestsControl.RunTests`, then publishes one `TestNodeUpdateMessage` per `TestCaseResult` with the matching terminal state property (`PassedTestNodeStateProperty` / `FailedTestNodeStateProperty(result.Message)` / `ErrorTestNodeStateProperty` / `SkippedTestNodeStateProperty`, based on `TestCaseResult.TestResult`).

### FR-003: Filter translation

When `ExecuteRequestContext.Request.Filter` is a UID-list filter (`TestNodeUidListFilter`), the bridge SHALL translate the UID list into the existing `UnitTestFilter` OR-expression syntax (e.g. `"A.B.C1 | A.B.C2"`) and pass it through `UnitTestEngineConfig.Filter`, reusing `UnitTestFilter.Parse` unchanged rather than building a second filter engine.

### FR-004: Exit code propagation

The exit code returned by `TestApplication.RunAsync()` SHALL be propagated through the same hard-exit path as `RuntimeTestEmbeddedRunner.ExitApplication` (`_exit()` on Linux, `Environment.Exit` elsewhere), to avoid regressing the Skia/X11 segfault-swallows-exit-code issue fixed by commit `3e87673`.

### FR-005: Package opt-in surface

Consumers who want MTP support SHALL add `Microsoft.Testing.Platform.MSBuild` (and optionally `Microsoft.Testing.Extensions.TrxReport`) as `PackageReference`s to their Desktop head project, conditioned on the desktop `TargetFramework`, plus `<GenerateTestingPlatformEntryPoint>false</GenerateTestingPlatformEntryPoint>` (the Desktop head keeps its own `Main`/`UnoPlatformHostBuilder` entry point — MTP's auto-generated entry point must be disabled).

### FR-006: `global.json` test runner

`src/global.json` SHALL declare `"test": {"runner": "Microsoft.Testing.Platform"}` **alongside** (not replacing) the existing `"sdk"` pinning block, enabling MTP mode of `dotnet test` for this repo's own solution.

### FR-007: Sharding/filter composition

When both sharding (`UNO_RUNTIME_TESTS_SHARD_INDEX`/`_TOTAL_SHARDS` or Azure auto-detection) and an MTP-supplied filter are present, the existing composition order SHALL be preserved: filter narrows the set first, then sharding selects from the filtered set (per [002-sharding](../002-sharding/spec.md) FR-006).

### NFR-001: No behavior change when not opted in

Consumers who do not add the MTP package references and do not modify `Program.cs` SHALL observe zero change in behavior, output, or performance.

### NFR-002: Native `ITestFramework`, not VSTest bridge

The bridge SHALL implement `ITestFramework` natively rather than via `Microsoft.Testing.Extensions.VSTestBridge`, consistent with MTP's own recommendation for non-VSTest-based frameworks.

### NFR-003: Desktop-only scope

MTP wiring SHALL be conditioned on the desktop `TargetFramework`(s) only; the Android/iOS/BrowserWasm TFMs of the same multi-targeted TestApp project SHALL be unaffected (no added package references, no behavior change).

## Technical Design

### Why this can't be a module-initializer drop-in like the existing engine

`RuntimeTestEmbeddedRunner.AutoStartTests` is a `[ModuleInitializer]` — zero-touch for consumers, but it can only read environment variables, because module initializers run before `Main` and never see `args`. MTP's `TestApplication.CreateBuilderAsync(args)` needs the real command line (`dotnet test`/IDE pass `--list-tests`, filters, results-directory, etc. as argv), and `dotnet test`/Test Explorer invoke the built executable directly and read *its* process exit code. So MTP must own a slice of `Main`. This is the one place where MTP support can't be as invisible as today's "reference a NuGet package, change nothing else" model — it needs a few lines added to the consumer's Desktop `Program.cs`.

### Data flow

```
dotnet test (MTP mode, global.json test.runner)
    │  launches TestApp.exe with MTP argv (--list-tests | run + filters)
    ▼
Program.Main(args)                                  [src/TestApp/Platforms/Desktop/Program.cs]
    │
    ├── UnoPlatformHostBuilder...Build().Run()       (unchanged: boots Skia/X11/Win32 host + message loop)
    │
    └── (new) MTP branch:
          var builder = await TestApplication.CreateBuilderAsync(args);
          builder.RegisterTestFramework(
              _ => new UnoRuntimeTestsFrameworkCapabilities(),
              (caps, sp) => new UnoRuntimeTestsFramework(caps, sp));
          using var app = await builder.BuildAsync();
          var exitCode = await app.RunAsync();
          ExitApplication(exitCode);   // reuses RuntimeTestEmbeddedRunner's hard-exit helper
                │
                ▼
       UnoRuntimeTestsFramework.ExecuteRequestAsync   [new, in Library/Engine/ExternalRunner]
          ├── DiscoverTestExecutionRequest → reflection scan (shared with UnitTestsControl.InitializeTests)
          │      → MessageBus.PublishAsync(TestNodeUpdateMessage[Discovered]) per test
          └── RunTestExecutionRequest
                 → same wait-for-window/dispatcher sequence as RunTestsAndExit
                 → translate Filter (TestNodeUidListFilter → UnitTestFilter OR-expression)
                 → UnitTestsControl.RunTests(ct, config)
                 → for each TestCaseResult in engine.Results:
                       MessageBus.PublishAsync(TestNodeUpdateMessage[Passed|Failed|Error|Skipped])
                 → context.Complete()
```

### Files to modify / add

1. **`src/global.json`** — restore the `sdk` block, add the `test.runner` block (FR-006).
2. **`src/Uno.UI.RuntimeTests.Engine.Library/Engine/ExternalRunner/UnoRuntimeTestsFramework.cs`** (new) — `ITestFramework` + `IDataProducer` implementation; guarded by `#if !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER` (same feature-flag convention as `RuntimeTestEmbeddedRunner`). Reuses `RuntimeTestEmbeddedRunner`'s private static helpers (`WaitForCheckSafe`, `WaitForIdle`, `TryParseConfig`, `ApplyShardingFromEnvironment`, `ExitApplication`) — will require widening a few of these from `private` to `internal`.
3. **`src/Uno.UI.RuntimeTests.Engine.Library/Engine/UI/UnitTestsControl.cs`** — no functional change expected; confirm `InitializeTests()`'s discovery step can run standalone (discovery-only, no execution) for `DiscoverTestExecutionRequest`.
4. **`src/TestApp/Platforms/Desktop/Program.cs`** — add the opt-in MTP branch in `Main`, illustrating the pattern consumers would replicate.
5. **`src/TestApp/Uno.UI.RuntimeTests.Engine.TestApp.csproj`** — add `Microsoft.Testing.Platform.MSBuild` / `Microsoft.Testing.Extensions.TrxReport` `PackageReference`s and `GenerateTestingPlatformEntryPoint=false`, conditioned on the desktop TFM only.
6. **`README.md`** — new section "Running via `dotnet test` (Microsoft.Testing.Platform)" documenting the opt-in steps, alongside the existing env-var/CI section (kept as-is).
7. **`specs/003-mtp-integration/progress.md`** (new, once implementation starts) — phased tracking, following the `002-sharding/progress.md` format.

### Open Questions / Risks

These need a short spike before implementation can be considered low-risk; none of them are assumed solved by this spec:

- **Main-flow ownership** — ✅ **Resolved by spike** (standalone console app, `Microsoft.Testing.Platform`/`.MSBuild` 2.3.1, .NET 10 SDK 10.0.201). A background `Task.Run` drives `TestApplication.CreateBuilderAsync → RegisterTestFramework → BuildAsync → RunAsync` while the calling thread blocks in a `while (!done.Wait(20)) { }` loop simulating `host.Run()`'s native message pump; once `RunAsync()` returns, the exit code is propagated via `Environment.Exit` from inside the background task, terminating the "blocked" main thread immediately. Verified working for: `--list-tests` (discovery), default run (execution, `Test run summary: Passed!`), a simulated failing test (`Test run summary: Failed!`, exit code `2`, propagated correctly through `Environment.Exit` despite the main thread being mid-loop), `--filter-uid <uid>` (delivered as `Microsoft.Testing.Platform.Requests.TestNodeUidListFilter` with `.TestNodeUids`, vs. `Requests.NopFilter` when unfiltered), and a real end-to-end `dotnet test` invocation (via a `global.json` with `"test": {"runner": "Microsoft.Testing.Platform"}`) — `dotnet test` correctly launched the exe, captured its stdout, and surfaced exit code `2` for the failing run. No hangs, no deadlocks, no interference between the blocking loop and the MTP session in any of these runs. Confirmed API surface: `ITestFrameworkCapabilities`/`ITestFrameworkCapability` live in `Microsoft.Testing.Platform.Capabilities.TestFramework` (not `Extensions.TestFramework` as the general docs example implies); `DiscoverTestExecutionRequest`/`RunTestExecutionRequest`/`TestNodeUidListFilter`/`NopFilter` live in `Microsoft.Testing.Platform.Requests`.
- **Exact `ITestExecutionFilter` subtype(s)** MTP sends for `--filter` / "run selected tests": `TestNodeUidListFilter` is confirmed to exist; the exact shape used for text-based `--filter` expressions needs confirming against MTP source/samples during implementation.
- **`RunsInSecondaryAppAttribute` tests** spawn a child process of the same exe. Whether that's safe while the *parent* process is itself running under the MTP host (env var/argv inheritance, message-bus confusion) is unverified.
- **MTP invocation detection**: the exact mechanism for `Program.cs` to tell "this process was launched under MTP" apart from "a user double-clicked TestApp.exe" — most likely this doesn't need explicit detection at all, since `TestApplication.CreateBuilderAsync(args)` is expected to no-op gracefully on non-MTP argv, but this needs confirming rather than assuming.

## Edge Cases

| Scenario | Behavior |
|---|---|
| App launched normally (no MTP argv, no env vars) | Behaves exactly as today — normal Uno app UI, no test engine involvement |
| App launched via `dotnet test` (MTP mode) | MTP path only; if `UNO_RUNTIME_TESTS_*` env vars also happen to be set, this is an unsupported combination — log a warning |
| `dotnet test --filter` selects zero tests | Discovery + empty run; MTP reports 0 tests, process exits 0 (mirrors the empty-shard behavior in [002-sharding](../002-sharding/spec.md)) |
| Test fails via unhandled exception vs. assertion failure | Mapped to `ErrorTestNodeStateProperty` vs `FailedTestNodeStateProperty` respectively, based on `TestCaseResult.TestResult` (`Error` vs `Failed`) |
| Consumer hasn't added `Microsoft.Testing.Platform.MSBuild` | `Program.cs`'s MTP branch is simply absent — no behavior difference, no new compile requirement |
| `RunsOnUIThreadAttribute` tests | Same dispatcher-hop logic as today; MTP bridge reuses the identical wait/dispatch code path |
| `--filter-uid`/"run selected test" for a method whose name is a *prefix* of a sibling method in the same class (e.g. `Is_Sane` vs. `Is_Sane_With_DynamicData`) | Both run. Confirmed by manual testing: `UnitTestFilter`'s `TextFilter` does substring `Contains(...)` matching against the FQN (by design, to support hierarchical/dotted filters like `abc & ghi`), not exact-match -- a pre-existing property of the filter engine shared with the classic env-var/CLI filter flow, not something introduced by MTP support |
| Selecting a single `[DataRow]` case via "run selected tests" | Not supported -- Discovery reports one node per *method* (matching the engine's existing method-level filter/shard granularity), so selecting it runs all of that method's rows |

## Success Criteria

| Metric | Target |
|---|---|
| `dotnet test` (MTP mode) pass/fail count vs. existing NUnit XML output for the same run | 100% match |
| Existing env-var/NUnit-XML flow regression | Zero behavior change |
| IDE Test Explorer discovery count vs. reflection-based count from `InitializeTests()` | 100% parity |
| CI adoption path documented | README section + one working GitHub Actions example |

## Usage

### Local

```bash
dotnet test src/TestApp/Uno.UI.RuntimeTests.Engine.TestApp.csproj -f net10.0-desktop
```

### With trx report

```bash
dotnet test src/TestApp/Uno.UI.RuntimeTests.Engine.TestApp.csproj -f net10.0-desktop --report-trx
```

### CI (GitHub Actions, illustrative)

```yaml
- name: Run Runtime Tests (MTP)
  run: |
    xvfb-run --auto-servernum --server-args='-screen 0 1280x1024x24' \
      dotnet test src/MyApp/MyApp.csproj -c Release -f net10.0-desktop --report-trx

- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Runtime Tests
    path: '**/*.trx'
    reporter: dotnet-trx
```

## Verification Steps

1. Restore `sdk` pinning + add `test.runner` in `src/global.json`; confirm the pinned SDK still resolves.
2. Add MTP package refs + `Program.cs` branch to the TestApp Desktop head only; confirm Android/iOS/BrowserWasm TFMs build unchanged.
3. Run `dotnet test ... -f net10.0-desktop` locally (headless via `xvfb-run` on Linux) — confirm per-test pass/fail appears.
4. Run with a `--filter`/single-test selection — confirm only the targeted test(s) execute.
5. Run the existing env-var flow (`UNO_RUNTIME_TESTS_RUN_TESTS=... dotnet TestApp.dll`) unmodified — confirm output is unchanged vs. a pre-change baseline.
6. Open the TestApp in a MTP-aware Test Explorer (VS Code/Visual Studio) — confirm tests are listed and individually runnable.
7. Run with sharding env vars + an MTP filter simultaneously — confirm composition order matches [002-sharding](../002-sharding/spec.md).
