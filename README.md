# Uno.UI Runtime Tests Engine
Uno.UI Provides a in-app test runner for applications wanting to run unit tests in their target environment.

This engine uses the MSTest attributes to perform the discovery of tests.

### Supported features
- Running on or off the UI Thread
- Using Data Rows
- Running tests full screen
- Generating an nunit report for CI publishing

### Structure of the package

This package is built as a source package. It includes XAML files and cs files into the referencing project, making it independent from the Uno.UI version used by your project. It is built in such a way that Uno.UI itself can use this package while not referencing itself through nuget packages dependencies.

## Using the Runtime Tests Engine
- Add the [`Uno.UI.RuntimeTests.Engine`](https://www.nuget.org/packages/Uno.UI.RuntimeTests.Engine/) nuget package to the head projects.
- Add the following xaml to a page in your application:
    ```xml
    <Page xmlns:runtimetests="using:Uno.UI.RuntimeTests" ...>
        <runtimetests:UnitTestsControl />
    </Page>
    ```
- For `unoapp` (WinUI) solution only, add the following to the Windows head project:
    ```xml
    <PropertyGroup>
        <DefineConstants>$(DefineConstants);WINDOWS_WINUI</DefineConstants>
    </PropertyGroup>
    ```
    note: This should not be added to the Windows head project of an `unoapp-uwp` (UWP) solution.

The test control will appear on that page.

Write your tests as follows:
```csharp
[TestClass]
public class Given_MyClass
{
    [TestMethod]
    public async Task When_Condition()
    {
        // Use MSTests asserts or any other assertion frameworks
    }
}
```

## Interacting with content in a test
The right side of the Runtime Tests control can display actual controls created by tests.

To set content here, use the `UnitTestsUIContentHelper.Content` property:
```csharp
[TestClass]
public class Given_MyClass
{
    [TestMethod]
    public async Task When_Condition()
    {
        var SUT  = new TextBlock(); 
        UnitTestsUIContentHelper.Content = SUT;
        
        await UnitTestsUIContentHelper.WaitForIdle();
        // or await UnitTestsUIContentHelper.WaitForLoaded(SUT);
		
        // Use MSTests asserts or any other assertion frameworks
    }
}
```

### Using UnitTestsUIContentHelper in a separate assembly
If you want to use the `UnitTestsUIContentHelper` in a separate assembly, you will need to add the following clas to the separate project:

```csharp
public static class UnitTestsUIContentHelperProxy
{
	public static Action<UIElement?>? ContentSetter { get; set; }
	public static Func<UIElement?>? ContentGetter { get; set; }

	public static UIElement? Content 
    { 
        get => ContentGetter?.Invoke();
        set => ContentSetter?.Invoke(value);
    }
}
```

Then in the page that defines the `UnitTestsControl`, add the following:
```csharp
public MyPage()
{
    InitializeComponent();

	// Initialize the UnitTestsUIContentHelperProxy for the test assembly
    UnitTestsUIContentHelperProxy.ContentSetter = e => UnitTestsUIContentHelper.Content = e;
	UnitTestsUIContentHelperProxy.ContentGetter = () => UnitTestsUIContentHelper.Content;
}
```

## Attributes for handling UI specific behavior

The behavior of the test engine regarding tests can be altered using attributes.

### RequiresFullWindowAttribute
Placing this attribute on a test will switch the rendering of the test render zone to be full screen, allowing for certain kind of tests to be executed.

### RunsOnUIThreadAttribute
Placing this attribute on a test or test class will force the tests to run on the dispatcher. By default, tests run off of the dispatcher.

### InjectedPointerAttribute
This attribute configures the type of the pointer that is simulated when using helpers like `App.TapCoordinates()`. You can define the attribute more than once, the test will then be run for each configured type. When not defined, the test engine will use the common pointer type of the platform (touch for mobile OS like iOS and Android, mouse for browsers, skia and other desktop platforms).

## Placing tests in a separate assembly
- In your separated test assembly
	- Add a reference to the "Uno.UI.RuntimeTests.Engine" package
	- Then define the following in your `csproj`:
	   ```xml
		<PropertyGroup>
			<DefineConstants>$(DefineConstants);UNO_RUNTIMETESTS_DISABLE_UI</DefineConstants>
		</PropertyGroup>
	   ```

- In your test application
	- Add a reference to the "Uno.UI.RuntimeTests.Engine" package
	- Then define the following in your `csproj`:
	   ```xml
		<PropertyGroup>
			<DefineConstants>$(DefineConstants);UNO_RUNTIMETESTS_DISABLE_LIBRARY</DefineConstants>
		</PropertyGroup>
	   ```
	   
### Alternative method
Alternatively, if you have only limited needs, in your separated test assembly, add the following attributes code:
   ```csharp
   using System;
   using Windows.Devices.Input;

   namespace Uno.UI.RuntimeTests;

   public sealed class RequiresFullWindowAttribute : Attribute { }

   public sealed class RunsOnUIThreadAttribute : Attribute { }

   [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
   public sealed class InjectedPointerAttribute : Attribute
   {
       public PointerDeviceType Type { get; }

       public InjectedPointerAttribute(PointerDeviceType type)
       {
           Type = type;
       }
   }
   ```
and define the following in your `csproj`:
  ```xml
   <PropertyGroup>
	<DefineConstants>$(DefineConstants);UNO_RUNTIMETESTS_DISABLE_UI</DefineConstants>
       <DefineConstants>$(DefineConstants);UNO_RUNTIMETESTS_DISABLE_INJECTEDPOINTERATTRIBUTE</DefineConstants>
       <DefineConstants>$(DefineConstants);UNO_RUNTIMETESTS_DISABLE_REQUIRESFULLWINDOWATTRIBUTE</DefineConstants>
       <DefineConstants>$(DefineConstants);UNO_RUNTIMETESTS_DISABLE_RUNSONUITHREADATTRIBUTE</DefineConstants>
   </PropertyGroup>
  ```
These attributes will ask for the runtime test engine to replace the ones defined by the `Uno.UI.RuntimeTests.Engine` package.

## Running the tests automatically during CI
When your application references the runtime-test engine, as soon as you start it with the following environment variables, the runtime-test engine will automatically run the tests on application startup and then kill the app.

* **UNO_RUNTIME_TESTS_RUN_TESTS**: This can be either
	* A json serialized [test configuration](https://github.com/unoplatform/uno.ui.runtimetests.engine/blob/main/src/Uno.UI.RuntimeTests.Engine.Library/Engine/UnitTestEngineConfig.cs) (use `{}` to run with default configuration);
 	* A filter string.
* **UNO_RUNTIME_TESTS_OUTPUT_PATH**: This is the output path of the test result file

You can also define some other configuration variables:

* **UNO_RUNTIME_TESTS_OUTPUT_KIND**: Selects the kind of the test result file, possible values are `NUnit` (default) or `UnoRuntimeTests` (cf. [`TestResultKind`](https://github.com/unoplatform/uno.ui.runtimetests.engine/blob/main/src/Uno.UI.RuntimeTests.Engine.Library/Engine/ExternalRunner/RuntimeTestEmbeddedRunner.cs#L41))
* **UNO_RUNTIME_TESTS_OUTPUT_URL**: (For WebAssembly) An HTTP endpoint URL where results will be POSTed instead of written to a file. This is required for WASM targets since they cannot write files to disk.

Currently, the easiest way to run runtime-tests on the CI is using the Desktop (Skia) target. Here is an example of a GitHub Actions workflow:

```yaml
jobs:
  runtime-tests:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'  # Or your target .NET version

    - name: Run uno-check
      run: |
        dotnet tool install -g uno.check
        uno-check --target linux --fix --non-interactive --ci

    - name: Build Runtime Tests app
      run: dotnet build src/MyApp/MyApp.csproj -c Release -f net9.0-desktop

    - name: Run Runtime Tests
      run: |
        mkdir -p test-results
        xvfb-run --auto-servernum --server-args='-screen 0 1280x1024x24' \
          dotnet src/MyApp/bin/Release/net9.0-desktop/MyApp.dll
      env:
        UNO_RUNTIME_TESTS_RUN_TESTS: '{}'
        UNO_RUNTIME_TESTS_OUTPUT_PATH: 'test-results/runtime-tests.xml'

    - name: Publish Test Results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Runtime Tests
        path: test-results/runtime-tests.xml
        reporter: dotnet-nunit
        fail-on-error: true
```

Here is the equivalent Azure DevOps pipeline configuration:

```yaml
jobs:
- job: Runtime_Tests
  displayName: 'Runtime Tests - Desktop'
  timeoutInMinutes: 60

  pool:
    vmImage: 'ubuntu-latest'

  variables:
    NUGET_PACKAGES: $(build.sourcesdirectory)/.nuget

  steps:
  - checkout: self
    clean: true

  - task: UseDotNet@2
    displayName: 'Use .NET'
    inputs:
      packageType: 'sdk'
      version: '9.x'  # Or your target .NET version

  - script: |
      dotnet tool install -g uno.check
      uno-check --target linux --fix --non-interactive --ci
    displayName: 'Run uno-check'

  - script: dotnet build src/MyApp/MyApp.csproj -c Release -f net9.0-desktop -bl:$(Build.ArtifactStagingDirectory)/runtime-test-build.binlog
    displayName: 'Build Runtime Tests app'

  - task: PublishBuildArtifacts@1
    displayName: Publish Build Logs
    condition: always()
    inputs:
      PathtoPublish: $(build.artifactstagingdirectory)/runtime-test-build.binlog
      ArtifactName: runtime-test-build
      ArtifactType: Container

  - script: xvfb-run --auto-servernum --server-args='-screen 0 1280x1024x24' dotnet src/MyApp/bin/Release/net9.0-desktop/MyApp.dll
    displayName: 'Run Runtime Tests'
    env:
      UNO_RUNTIME_TESTS_RUN_TESTS: '{}'
      UNO_RUNTIME_TESTS_OUTPUT_PATH: '$(Common.TestResultsDirectory)/runtime-tests-results.xml'

  - task: PublishTestResults@2
    displayName: 'Publish Runtime Tests Results'
    condition: always()
    inputs:
      testRunTitle: 'Runtime Tests Run'
      testResultsFormat: 'NUnit'
      testResultsFiles: '$(Common.TestResultsDirectory)/runtime-tests-results.xml'
      failTaskOnFailedTests: true
```

Notes:
* This uses the Desktop (Skia) target with `net9.0-desktop` (or `net10.0-desktop` for .NET 10).
* The app runs headless using xvfb (virtual X server) on Linux.
* We use `{}` for the `UNO_RUNTIME_TESTS_RUN_TESTS` in order to run all tests with default configuration.
* If you want to test hot-reload scenarios (usually relevant only for library developers), you need to build your test project in debug (`-c Debug`).

### Running the tests automatically during CI on WebAssembly

For WebAssembly targets, a dedicated .NET tool (`Uno.UI.RuntimeTests.Engine.Wasm.Runner`) is available that:
1. Serves the WASM app via an embedded HTTP server
2. Provides an endpoint to receive test results (since WASM apps cannot write files to disk)
3. Launches a headless Chromium browser to run the tests
4. Collects results and writes them to disk

#### Installation

Install the tool globally:
```bash
dotnet tool install -g Uno.UI.RuntimeTests.Engine.Wasm.Runner
```

Or as a local tool in your project:
```bash
dotnet new tool-manifest  # if you don't have one
dotnet tool install Uno.UI.RuntimeTests.Engine.Wasm.Runner
```

#### Prerequisites

A Chromium-based browser must be available. The tool will search for:
1. Browsers installed via Playwright (`npx playwright install chromium`)
2. System-installed browsers (Chrome, Chromium, Edge)

To install Chromium via Playwright (recommended for CI):
```bash
npx playwright install chromium
```

#### Usage

```bash
# Run tests
uno-runtimetests-wasm --app-path ./publish/wwwroot --output ./results.xml
```

#### GitHub Actions Example

```yaml
jobs:
  wasm-runtime-tests:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Run uno-check
      run: |
        dotnet tool install -g uno.check
        uno-check --target wasm --fix --non-interactive --ci

    - name: Install WASM Runner tool
      run: dotnet tool install -g Uno.UI.RuntimeTests.Engine.Wasm.Runner

    - name: Install Chromium browser
      run: npx playwright install chromium

    - name: Build WASM app
      run: dotnet publish src/MyApp/MyApp.csproj -c Release -f net10.0-browserwasm -p:PublishTrimmed=false

    - name: Run WASM Runtime Tests
      run: |
        mkdir -p test-results
        uno-runtimetests-wasm \
          --app-path ./src/MyApp/bin/Release/net10.0-browserwasm/publish/wwwroot \
          --output ./test-results/wasm-runtime-tests.xml \
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

#### Command-line Options

* `--app-path`: Path to the published WASM app directory (required)
* `--output`: Path where test results will be written (required)
* `--filter`: Test filter expression (optional)
* `--timeout`: Timeout in seconds for test execution (default: 300)
* `--port`: Port to serve the WASM app on (default: auto-assign)
* `--headless`: Run browser in headless mode (default: true)

**Important notes for WASM testing:**
* Trimming must be disabled (`-p:PublishTrimmed=false`) when building the WASM app for runtime tests. The test engine uses reflection to discover and run tests, which is incompatible with trimming.
* A Chromium-based browser (Chrome, Chromium, or Edge) must be installed. Use `npx playwright install chromium` to install one.

### Running the tests automatically during CI on mobile targets
Alternatively, you can also run the runtime-tests on the CI using ["UI Tests"](https://github.com/unoplatform/Uno.UITest). Here is an example of how it's integrated in uno's core CI](https://github.com/unoplatform/uno/blob/master/src/SamplesApp/SamplesApp.UITests/RuntimeTests.cs#L32.
