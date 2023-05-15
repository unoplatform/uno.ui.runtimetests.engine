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

## Test runner (UnitTestsControl) filtering syntax
- Search terms are separated by space. Multiple consecutive spaces are treated same as one. 
- Multiple search terms are chained with AND logic.
- Search terms are case insensitive.
- `-` can be used before any term for exclusion, effectively inverting the results.
- Special tags can be used to match certain part of the test: // syntax: tag:term
    - `class` or `c` matches the class name
    - `method` or `m` matches the method name
    - `displayname` or `d` matches the display name in [DataRow]
- Search term without a prefixing tag will match either of method name or class name.

Examples:
- `listview`
- `listview measure`
- `listview measure -recycle`
- `c:listview m:measure -m:recycle`

## Running the tests automatically during CI
_TBD_
