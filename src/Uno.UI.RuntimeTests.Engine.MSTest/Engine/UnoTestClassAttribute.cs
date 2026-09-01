#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests;

/// <summary>
/// Marks a test class whose test methods should be executed through <see cref="UnoTestMethodAttribute"/>
/// (dispatcher marshaling for <see cref="RunsOnUIThreadAttribute"/>, pointer injection for
/// <see cref="InjectedPointerAttribute"/>, etc.) even when the individual methods are decorated with
/// a plain <see cref="TestMethodAttribute"/> instead of <see cref="UnoTestMethodAttribute"/> directly.
/// </summary>
#pragma warning disable CA1813 // Avoid unsealed attributes: intentionally extensible, matching TestClassAttribute's own design.
public class UnoTestClassAttribute : TestClassAttribute
{
	/// <inheritdoc />
	public override TestMethodAttribute GetTestMethodAttribute(TestMethodAttribute testMethodAttribute)
		=> testMethodAttribute as UnoTestMethodAttribute ?? new UnoTestMethodAttribute();
}
