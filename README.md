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
When your app references the runtime-test engine, as soon as you start your application with the following environement variables, the runtime-test engine will automatically run the tests on application startup and then kill the app.

* **UNO_RUNTIME_TESTS_RUN_TESTS**: This can be either
	* A json serialized [test configuration](https://github.com/unoplatform/uno.ui.runtimetests.engine/blob/main/src/Uno.UI.RuntimeTests.Engine.Library/Engine/UnitTestEngineConfig.cs) (use `{}` to run with default configuration);
 	* A filter string.
* **UNO_RUNTIME_TESTS_OUTPUT_PATH**: This is the output path of the test result file

You can also define some other configurations variables:

* **UNO_RUNTIME_TESTS_OUTPUT_KIND**: Selects the kind of the test result file, possible values are `NUnit` (default) or `UnoRuntimeTests` (cf. [`TestResultKind`](https://github.com/unoplatform/uno.ui.runtimetests.engine/blob/main/src/Uno.UI.RuntimeTests.Engine.Library/Engine/ExternalRunner/RuntimeTestEmbeddedRunner.cs#L41))

Currently the easiest way to run runtime-tests on the CI is using the skia-GTK, here an example of a azure pipeline configuration file:

```yaml
jobs:
- job: Skia_Tests
  displayName: 'Runtime Tests - Skia GTK'
  timeoutInMinutes: 60
  
  pool:
    vmImage: 'ubuntu-20.04'

  variables:
    NUGET_PACKAGES: $(build.sourcesdirectory)/.nuget

  steps:
  - checkout: self
    clean: true
    
  - task: UseDotNet@2
    displayName: 'Use .NET'
    inputs:
      packageType: 'sdk'
      version: '7.x'
      
  - script: |
        dotnet tool install -g uno.check
        uno-check --target skiagtk --fix --non-interactive --ci
    
    displayName: 'Run uno-check'    

  - script: dotnet build Uno.Extensions.RuntimeTests.Skia.Gtk.csproj -c Release -p:UnoTargetFrameworkOverride=net7.0 -p:GeneratePackageOnBuild=false -bl:$(Build.ArtifactStagingDirectory)/skia-gtk-runtime-test-build.binlog
    displayName: 'Build Runtime Tests app (GTK)'
    workingDirectory: $(Build.SourcesDirectory)/src/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests.Skia.Gtk

  - task: PublishBuildArtifacts@1
    displayName: Publish Build Logs
    retryCountOnTaskFailure: 3
    condition: always()
    inputs:
      PathtoPublish: $(build.artifactstagingdirectory)/skia-gtk-runtime-test-build.binlog
      ArtifactName: skia-runtime-test-build
      ArtifactType: Container

  - script: xvfb-run --auto-servernum --server-args='-screen 0 1280x1024x24' dotnet Uno.Extensions.RuntimeTests.Skia.Gtk.dll
    displayName: 'Run Runtime Tests (GTK)'
    workingDirectory: $(Build.SourcesDirectory)/src/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests.Skia.Gtk/bin/Debug/net7.0
    env:
      UNO_RUNTIME_TESTS_RUN_TESTS: '{}'
      UNO_RUNTIME_TESTS_OUTPUT_PATH: '$(Common.TestResultsDirectory)/skia-gtk-runtime-tests-results.xml'

  - task: PublishTestResults@2
    displayName: 'Publish GTK Runtime Tests Results'
    condition: always()
    retryCountOnTaskFailure: 3
    inputs:
      testRunTitle: 'GTK Runtime Tests Run'
      testResultsFormat: 'NUnit'
      testResultsFiles: '$(Common.TestResultsDirectory)/skia-gtk-runtime-tests-results.xml'
      failTaskOnFailedTests: true 
```

Notes:
* This is running the GTK head using a virtual x-server (xvfb).
* We use `{}` for the `UNO_RUNTIME_TESTS_RUN_TESTS` in order to run all tests with default configuration.
* If you want to test hot-relaod scenarios (usually relevant only for library developers), you need to build your test project in debug (`-c Debug`).
