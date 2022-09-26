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
- Add the Uno.UI.RuntimeTests.Engine nuget package to your application
- Add the following xaml to your application:
    ```xaml
    <Page .... xmlns:runtimetests="using:Uno.UI.RuntimeTests">
        <runtimetests:UnitTestsControl />
    </Page>
    ```
- In the uwp/winui head project, add the following:
    ```xml
    <PropertyGroup>
    	<DefineConstants>$(DefineConstants);WINDOWS</DefineConstants>
    </PropertyGroup>
    ```

The test control will appear on your screen.

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

## Attributes for handling UI specific behavior

The behavior of the test engine regarding tests can be altered using attributes.

### RequiresFullWindowAttribute
Placing this attribute on a test will switch the rendering of the test render zone to be full screen, allowing for certain kind of tests to be executed.

### RunsOnUIThreadAttribute
Placing this attribute on a test or test class will force the tests to run on the dispatcher. By default, tests run off of the dispatcher.

### InjectedPointerAttribute
_TBD_

## Placing tests in a separate assembly
- In a separate assembly, add the following attributes code:
    ```csharp
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
- Then define the following in your `csproj`:
   ```xml
    <PropertyGroup>
    	<DefineConstants>$(DefineConstants);UNO_RUNTIMETESTS_DISABLE_INJECTEDPOINTERATTRIBUTE</DefineConstants>
    	<DefineConstants>$(DefineConstants);UNO_RUNTIMETESTS_DISABLE_REQUIRESFULLWINDOWATTRIBUTE</DefineConstants>
    	<DefineConstants>$(DefineConstants);UNO_RUNTIMETESTS_DISABLE_RUNSONUITHREADATTRIBUTE</DefineConstants>
    </PropertyGroup>
   ```

These attributes will ask for the runtime test engine to replace the ones defined by the `Uno.UI.RuntimeTests.Engine` package.

## Running the tests automatically during CI
_TBD_