using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Engine.Extensions;

internal static class AssertExtensions
{
	public static void AreEqual<T>(this Assert assert, T expected, T actual, IEqualityComparer<T> comparer)
	{
		if (!comparer.Equals(expected, actual))
		{
			Assert.Fail(string.Join("\n",
				"AreEqual failed.",
				$"Expected: {expected}",
				$"Actual: {actual}"
			));
		}
	}
}
