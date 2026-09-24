#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_UI
/// <summary>
/// A plain <see cref="TestClassAttribute"/> subclass, so that test classes can use <c>[UnoTestClass]</c>
/// uniformly regardless of which runtime-tests engine is active. The hand-rolled engine (this project)
/// discovers test classes via <see cref="TestClassAttribute"/> matching (which also matches this
/// subclass), so no special behavior is needed here -- it only exists so the *same* test source
/// compiles and behaves correctly whether or not <c>$(UseMSTest)</c> is set. See
/// <c>Uno.UI.RuntimeTests.Engine.MSTest</c>'s <c>UnoTestClassAttribute</c> for the MSTest-native engine,
/// where this attribute is what makes <see cref="RunsOnUIThreadAttribute"/>/<see cref="InjectedPointerAttribute"/>/
/// etc. actually take effect.
/// </summary>
#pragma warning disable CA1813 // Avoid unsealed attributes: intentionally extensible, matching TestClassAttribute's own design.
public class UnoTestClassAttribute : TestClassAttribute
{
}
#endif
