# WASM AOT Profile Extraction

## Overview

Enable automated AOT profile extraction from the WASM runtime tests runner. When tests execute, the AOT profiler records which methods are called. This profile guides subsequent AOT compilation to optimize frequently-used methods while keeping rarely-used code interpreted.

## Goals

- **G1**: Extract AOT profile data automatically after test execution
- **G2**: Integrate with existing CLI tool without requiring browser interaction
- **G3**: Support CI/CD workflows for automated profile generation
- **G4**: Maintain backward compatibility with existing test runner functionality

## Non-Goals

- Modifying the Uno.Wasm.Bootstrap AOT profiler implementation
- Supporting profile extraction on non-WASM platforms
- Automatic profile application (users configure their own builds)
- Profile merging from multiple test runs

## User Stories

### P1: CI Pipeline Profile Generation

**As a** CI engineer
**I want to** extract AOT profiles automatically during test runs
**So that** I can use them for optimized production builds without manual intervention

**Acceptance Criteria:**
- Given a WASM app built with `WasmShellGenerateAOTProfile=true`
- When I run `uno-runtimetests-wasm --aot-profile-output profile.aot`
- Then the profile file is created with non-zero size
- And the test results are still saved to the output file
- And the exit code reflects test pass/fail status

### P2: Profile Extraction Without Profiling Enabled

**As a** developer
**I want to** receive a clear warning when profile extraction fails
**So that** I know to enable profiling in my build

**Acceptance Criteria:**
- Given a WASM app built without AOT profiling enabled
- When I run with `--aot-profile-output`
- Then a warning is logged: "No AOT profile data available"
- And tests complete successfully
- And no profile file is created

## Requirements

### FR-001: CLI Parameter

The CLI tool SHALL accept an optional `--aot-profile-output <path>` parameter specifying where to save the extracted AOT profile.

### FR-002: Environment Variable

The runtime library SHALL read `UNO_RUNTIME_TESTS_AOT_PROFILE_OUTPUT_URL` to determine where to POST extracted profile data.

### FR-003: Server Endpoint

The test server SHALL provide a `/aot-profile` endpoint that accepts binary POST data and stores it for later retrieval.

### FR-004: Profile Extraction (WASM)

On WASM platforms, after test execution completes, the runtime SHALL:
1. Extract profile data via JavaScript interop
2. POST the binary data to the configured URL
3. Log success or failure

### FR-005: Error Handling

Profile extraction failures SHALL NOT cause test execution to fail. Failures SHALL be logged as warnings.

### FR-006: Timeout

Profile extraction SHALL have a 30-second timeout independent of the test execution timeout.

## Technical Design

### Data Flow

```
CLI (--aot-profile-output)
    │
    ├── Injects UNO_RUNTIME_TESTS_AOT_PROFILE_OUTPUT_URL env var
    ├── Adds /aot-profile endpoint to HTTP server
    │
    ▼
WASM App (after tests complete)
    │
    ├── Extracts profile via JavaScript interop
    ├── POSTs binary profile data to /aot-profile
    │
    ▼
CLI
    │
    ├── Receives profile data
    └── Writes to --aot-profile-output path
```

### JavaScript API

```javascript
// Try Uno.Wasm.Bootstrap API first
globalThis.Uno?.WebAssembly?.Bootstrapper?.getAotProfileData?.()
// Fallback to Module.aot_profile_data if available
```

### Files to Modify

1. **`src/Uno.UI.RuntimeTests.Engine.Wasm.Runner/Program.cs`**
   - Add `--aot-profile-output` CLI option
   - Inject `UNO_RUNTIME_TESTS_AOT_PROFILE_OUTPUT_URL` environment variable
   - Add `/aot-profile` POST endpoint to TestServer
   - Wait for and save profile data after test results

2. **`src/Uno.UI.RuntimeTests.Engine.Library/Engine/ExternalRunner/RuntimeTestEmbeddedRunner.cs`**
   - Read `UNO_RUNTIME_TESTS_AOT_PROFILE_OUTPUT_URL` config
   - Extract AOT profile via JS interop after tests complete (WASM only)
   - POST binary profile data to configured URL

3. **`src/Uno.UI.RuntimeTests.Engine.Library/Engine/ExternalRunner/_Private/WasmTestResultReporter.cs`**
   - Add `PostBinaryAsync` method for binary data uploads

## Success Criteria

| Metric | Target |
|--------|--------|
| Profile extraction success rate (when enabled) | ≥ 99% |
| Profile file size | > 0 bytes |
| No regression in test execution time | < 5% increase |
| CLI backward compatibility | 100% (no breaking changes) |

## Usage

### CLI Command

```bash
uno-runtimetests-wasm run \
  --app-path ./publish/wwwroot \
  --output test-results.xml \
  --aot-profile-output aot.profile \
  --timeout 600
```

### Prerequisites

The WASM app must be built with AOT profiling enabled:

```xml
<PropertyGroup>
  <WasmShellGenerateAOTProfile>true</WasmShellGenerateAOTProfile>
</PropertyGroup>
```

### Using the Extracted Profile

```xml
<PropertyGroup>
  <WasmShellMonoRuntimeExecutionMode>InterpreterAndAOT</WasmShellMonoRuntimeExecutionMode>
</PropertyGroup>

<ItemGroup>
  <WasmShellEnableAotProfile Include="aot.profile" />
</ItemGroup>
```

### CI Integration

```yaml
- name: Run WASM Tests with Profile Generation
  run: |
    uno-runtimetests-wasm run \
      --app-path ./publish/wwwroot \
      --output ./test-results.xml \
      --aot-profile-output ./aot.profile

- name: Upload AOT Profile
  uses: actions/upload-artifact@v4
  with:
    name: aot-profile
    path: ./aot.profile
```

## Verification Steps

1. Build TestApp with `<WasmShellGenerateAOTProfile>true</WasmShellGenerateAOTProfile>`
2. Run tests with `--aot-profile-output profile.aot`
3. Verify profile file is created and non-empty
4. Run without profile generation enabled - verify warning is logged but tests complete
