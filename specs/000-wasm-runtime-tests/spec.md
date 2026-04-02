# WebAssembly Runtime Tests Support

## Overview

Enable runtime tests execution for WebAssembly targets. The main challenge is that WASM apps run in a browser sandbox and cannot write files to disk, so an alternative result reporting mechanism is needed.

## Problem Statement

- Current `RuntimeTestEmbeddedRunner` writes test results to `UNO_RUNTIME_TESTS_OUTPUT_PATH` using `File.WriteAllTextAsync`
- This does not work in WebAssembly browser sandbox
- Need an HTTP-based mechanism for WASM apps to report results back to the test runner

## Solution Design

### Approach

Based on the pattern used in [Uno Platform's WASM test script](https://github.com/unoplatform/uno/blob/c0162c5edbbd6e8f29330175491f2cc6503e9766/build/test-scripts/wasm-run-skia-runtime-tests.sh):

1. WASM app POSTs test results to a local HTTP endpoint
2. A simple HTTP server receives the results and writes them to disk
3. The CI script polls for the results file

### New Environment Variable

| Variable | Description |
|----------|-------------|
| `UNO_RUNTIME_TESTS_OUTPUT_URL` | HTTP endpoint URL where results will be POSTed |

When set, results are POSTed to this URL instead of (or in addition to) writing to a file.

## Implementation

### 1. New Helper Class: `WasmTestResultReporter.cs`

**Location:** `src/Uno.UI.RuntimeTests.Engine.Library/Engine/ExternalRunner/_Private/WasmTestResultReporter.cs`

**Responsibilities:**
- POST test results to configured URL
- Retry logic (3 attempts with exponential backoff)
- Timeout handling (30 seconds per request)
- Error logging

**Public API:**
```csharp
internal static class WasmTestResultReporter
{
    public static string? OutputUrl { get; }
    public static bool IsEnabled { get; }

    public static Task<bool> PostResultsAsync(
        string content,
        string contentType,
        CancellationToken ct = default);
}
```

### 2. Modifications to `RuntimeTestEmbeddedRunner.cs`

**File:** `src/Uno.UI.RuntimeTests.Engine.Library/Engine/ExternalRunner/RuntimeTestEmbeddedRunner.cs`

**Changes:**

1. **`AutoStartTests()` method:**
   - Accept either `UNO_RUNTIME_TESTS_OUTPUT_PATH` OR `UNO_RUNTIME_TESTS_OUTPUT_URL`
   - For WASM, file path will be empty but URL will be set

2. **`RunTests()` method:**
   - After generating results, check if HTTP reporting is enabled
   - POST results to the configured URL
   - Keep file writing as fallback for non-WASM platforms

### 3. Project File Update

**File:** `src/Uno.UI.RuntimeTests.Engine.Library/Uno.UI.RuntimeTests.Engine.Library.projitems`

Add new source file reference.

## .NET Test Runner Tool

### New Project: `Uno.UI.RuntimeTests.Engine.Wasm.Runner`

**Location:** `src/Uno.UI.RuntimeTests.Engine.Wasm.Runner/`

A .NET global tool that orchestrates WASM test execution:

**Responsibilities:**
1. Serve the WASM app via embedded HTTP server (HttpListener)
2. Provide an endpoint to receive POSTed results (`/results`)
3. Launch headless Chromium browser to run tests
4. Write results to disk when received
5. Exit with appropriate code based on test results

**Key Features:**
- Distributed as a standalone .NET global tool
- Cross-platform (Linux, macOS, Windows)
- Auto-detects Playwright-installed or system browsers
- Configurable ports and output paths via command line
- Timeout handling for stuck tests

**Prerequisites:**
A Chromium-based browser must be available. The tool searches for:
1. Browsers installed via Playwright (`npx playwright install chromium`)
2. System-installed browsers (Chrome, Chromium, Edge)

**Distribution:**
The WASM Runner is distributed as a separate NuGet package `Uno.UI.RuntimeTests.Engine.Wasm.Runner` and can be installed as a .NET global tool:

```bash
# Install globally
dotnet tool install -g Uno.UI.RuntimeTests.Engine.Wasm.Runner

# Or as a local tool
dotnet new tool-manifest  # if you don't have one
dotnet tool install Uno.UI.RuntimeTests.Engine.Wasm.Runner
```

**Usage:**
```bash
# Install Chromium (required once, recommended for CI)
npx playwright install chromium

# Run tests
uno-runtimetests-wasm \
  --app-path ./publish/wwwroot \
  --output ./test-results/wasm-tests.xml \
  --timeout 300
```

**Command-line Options:**

- `--app-path`: Path to the published WASM app directory (required)
- `--output`: Path where test results will be written (required)
- `--filter`: Test filter expression (optional)
- `--timeout`: Timeout in seconds for test execution (default: 300)
- `--port`: Port to serve the WASM app on (default: auto-assign)
- `--headless`: Run browser in headless mode (default: true)

## CI Configuration

### New GitHub Actions Job

Add a `test-wasm` job to `.github/workflows/dotnet.yml` (runs on all PRs):

```yaml
test-wasm:
  name: WASM Runtime Tests
  runs-on: ubuntu-latest
  needs:
    - build

  steps:
  - uses: actions/checkout@v4
    name: Checkout sources
    with:
      fetch-depth: 0

  - name: Setup .NET
    uses: actions/setup-dotnet@v4
    with:
      dotnet-version: '10.0.x'

  - name: Download NuGet Artifacts
    uses: actions/download-artifact@v4
    with:
      name: NuGet
      path: artifacts

  - name: Setup local NuGet source
    run: |
      dotnet nuget add source ${{ github.workspace }}/artifacts --name local

  - name: Run uno-check
    run: |
      dotnet tool install -g uno.check
      uno-check --target wasm --fix --non-interactive --ci

  - name: Install WASM Runner tool
    run: |
      dotnet tool install -g Uno.UI.RuntimeTests.Engine.Wasm.Runner --version ${{ needs.build.outputs.semver }}

  - name: Install Chromium browser
    run: npx playwright install chromium

  - name: Build WASM TestApp
    run: dotnet publish src/TestApp/Uno.UI.RuntimeTests.Engine.TestApp.csproj -c Release -f net10.0-browserwasm -p:PublishTrimmed=false

  - name: Run WASM Runtime Tests
    run: |
      mkdir -p test-results
      uno-runtimetests-wasm \
        --app-path ./src/TestApp/bin/Release/net10.0-browserwasm/publish/wwwroot \
        --output ./test-results/wasm-runtime-tests.xml \
        --filter '!HotReloadTests & !SecondaryAppTests & !Is_SecondaryApp_Supported' \
        --timeout 600

  - name: Publish Test Results
    uses: dorny/test-reporter@v1
    if: always()
    with:
      name: WASM Runtime Tests
      path: test-results/wasm-runtime-tests.xml
      reporter: dotnet-nunit
      fail-on-error: true
```

## Documentation Updates

### README.md Additions

1. **WebAssembly Section:**
   - Explain HTTP-based result reporting
   - Environment variable configuration
   - CI integration example

2. **CI Examples:**
   - Add WASM-specific GitHub Actions example
   - Document the test runner script usage

## File Changes Summary

| File | Action | Description |
|------|--------|-------------|
| `Engine/ExternalRunner/_Private/WasmTestResultReporter.cs` | Create | HTTP POST helper class |
| `Engine/ExternalRunner/RuntimeTestEmbeddedRunner.cs` | Modify | Support URL-based output |
| `Uno.UI.RuntimeTests.Engine.Library.projitems` | Modify | Add new source file |
| `Uno.UI.RuntimeTests.Engine.Wasm.Runner/` | Create | .NET global tool for WASM test orchestration |
| `Uno.UI.RuntimeTests.Engine-dotnet-build.slnf` | Modify | Add WASM Runner project |
| `.github/workflows/dotnet.yml` | Modify | Add WASM test job (runs on all PRs) |
| `README.md` | Modify | Add WASM documentation |

## Testing Plan

1. **Local Testing:**
   - Build TestApp for `net10.0-browserwasm`: `dotnet publish src/TestApp -c Release -f net10.0-browserwasm -p:PublishTrimmed=false`
   - Install Chromium browser: `npx playwright install chromium`
   - Install and run the .NET tool:
     ```bash
     dotnet pack src/Uno.UI.RuntimeTests.Engine.Wasm.Runner -c Release
     dotnet tool install -g Uno.UI.RuntimeTests.Engine.Wasm.Runner --add-source ./src/Uno.UI.RuntimeTests.Engine.Wasm.Runner/bin/Release
     uno-runtimetests-wasm --app-path ./src/TestApp/bin/Release/net10.0-browserwasm/publish/wwwroot --output ./results.xml
     ```
   - Verify test results are received and written to disk

2. **CI Testing:**
   - Run new WASM job in GitHub Actions
   - Verify test results are collected and reported

## Build Requirements

- **Trimming must be disabled:** The WASM app must be built with `-p:PublishTrimmed=false`. The runtime test engine uses reflection to discover and invoke test methods, which is incompatible with IL trimming.

## Error Handling

- **POST Failures:** 3 retries with 1s, 2s, 3s delays
- **Timeout:** 30-second HTTP timeout per request
- **Logging:** Detailed console output for debugging
- **Exit Code:** Reflects test results, not POST success

## Skia Renderer Considerations

When using the Skia renderer on WASM (via WebGPU), the runtime test engine requires special handling due to asynchronous rendering surface initialization.

### Key Findings

#### 1. WebGPU Initialization is Asynchronous

On Skia WASM, the WebGPU rendering surface initializes asynchronously. This affects:

- **`Window.Current.Content`** - May remain `null` even after the app's `OnLaunched` completes
- **`FrameworkElement.Loaded` event** - May never fire because the visual tree isn't fully connected until WebGPU is ready

#### 2. Cached Build Artifacts Can Cause Issues

When switching between native rendering and Skia rendering, cached build artifacts can cause `System.TypeInitializationException`:

```
System.TypeInitializationException: Unable to find Uno.UI.Runtime.WebAssembly.CompileAnchor
```

**Solution**: Clean `bin/` and `obj/` folders when switching renderer modes:
```bash
rm -rf bin obj
dotnet build
```

#### 3. Default Timeouts Are Too Short

The default 1-second timeout in `TestHelper.DefaultTimeout` is insufficient for Skia WASM initialization. WebGPU surface creation can take several seconds.

### RuntimeTestEmbeddedRunner.cs Modifications

The embedded runner was modified to handle Skia WASM initialization:

#### 1. Extended Window Wait Loop

```csharp
// Wait for Window.Current to be available with a dispatcher
Window? window = null;
for (var i = 0; i < 300; i++)  // Up to 30 seconds
{
    window = Window.Current;
    if (window is { Dispatcher: not null })
    {
        break;
    }
    await Task.Delay(100, ct.Token).ConfigureAwait(false);
}
```

#### 2. Content Check with Fallback

```csharp
// Try to wait for content, but proceed if it's still null
for (var i = 0; i < 50; i++)  // Up to 5 seconds
{
    window = Window.Current;
    if (window?.Content is not null)
    {
        break;
    }
    await Task.Delay(100, ct.Token).ConfigureAwait(false);
}

// Proceed even if content is null (expected on Skia/WebGPU)
if (window.Content is null)
{
    Log("Window.Current.Content is still null - proceeding anyway");
}
```

#### 3. Loaded Event Fallback

```csharp
try
{
    await WaitForLoadedWithTimeout(engine, TimeSpan.FromSeconds(5), ct);
}
catch (TimeoutException)
{
    // On Skia WASM, Loaded event may never fire
    // Wait for WebGPU initialization instead
    await Task.Delay(3000, ct);
}
```

### Known Platform Limitations

The following tests are expected to fail on WASM with Skia renderer:

| Test | Reason |
|------|--------|
| `HotReloadTests` | SecondaryApp not supported on WASM platform |
| `When_TapCoordinates() [Mouse]` | Pointer injection requires fully loaded UI; timing issues with WebGPU |
| `When_TapCoordinates() [Touch]` | Same as above |

### URL Query Parameters Override

On WASM, URL query parameters take precedence over environment variables for output configuration (`UNO_RUNTIME_TESTS_OUTPUT_URL`, `UNO_RUNTIME_TESTS_OUTPUT_PATH`). This avoids issues with cached `uno-config.js` files containing stale server addresses from previous test runs.

### Troubleshooting

#### Tests Not Starting

1. Check browser console for WebGPU initialization errors
2. Verify `UNO_RUNTIME_TESTS_RUN_TESTS` is set
3. Ensure output URL/path is configured

#### TypeInitializationException

Clean build artifacts and rebuild:
```bash
rm -rf src/TestApp/bin src/TestApp/obj
dotnet build src/TestApp -c Release
```

#### Timeout Errors

If tests timeout waiting for UI elements, the WebGPU surface may not be initializing. Check:
- Browser supports WebGPU
- No GPU-related errors in console
- Sufficient delay for renderer initialization
