using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
public class FilterTests
{
	[TestMethod]
	[DataRow("abc & ghi", "abc", false)]
	[DataRow("abc & ghi", "ghi", false)]
	[DataRow("abc & ghi", "abc.ghi", true)]
	[DataRow("abc & (ghi)", "abc", false)]
	[DataRow("abc & (ghi)", "ghi", false)]
	[DataRow("abc & (ghi)", "abc.ghi", true)]
	[DataRow("a.b.c & ghi", "a.b", false)]
	[DataRow("a.b.c & ghi", "a.b.c.def.ghi", true)]
	[DataRow("a.b.c & (ghi)", "a.b", false)]
	[DataRow("a.b.c & (ghi)", "a.b.c.def.ghi", true)]
	[DataRow("abc & g.h.i", "g.h", false)]
	[DataRow("abc & g.h.i", "abc.def.g.h.i", true)]

	[DataRow("abc | ghi", "abc", true)]
	[DataRow("abc | ghi", "ghi", true)]
	[DataRow("abc | ghi", "abc.ghi", true)]
	[DataRow("abc | (ghi)", "abc", true)]
	[DataRow("abc | (ghi)", "ghi", true)]
	[DataRow("abc | (ghi)", "abc.ghi", true)]
	[DataRow("a.b.c | ghi", "a.b", false)]
	[DataRow("a.b.c | ghi", "a.b.c", true)]
	[DataRow("a.b.c | ghi", "a.b.c.def.ghi", true)]
	[DataRow("a.b.c | (ghi)", "a.b", false)]
	[DataRow("a.b.c | (ghi)", "a.b.c", true)]
	[DataRow("a.b.c | (ghi)", "a.b.c.def.ghi", true)]
	[DataRow("abc | g.h.i", "g.h", false)]
	[DataRow("abc | g.h.i", "g.h.i", true)]
	[DataRow("abc | g.h.i", "abc.def.g.h.i", true)]

	[DataRow("abc ; ghi", "abc", true)]
	[DataRow("abc ; ghi", "ghi", true)]
	[DataRow("abc ; ghi", "abc.ghi", true)]
	[DataRow("abc ; (ghi)", "abc", true)]
	[DataRow("abc ; (ghi)", "ghi", true)]
	[DataRow("abc ; (ghi)", "abc.ghi", true)]
	[DataRow("a.b.c ; ghi", "a.b", false)]
	[DataRow("a.b.c ; ghi", "a.b.c", true)]
	[DataRow("a.b.c ; ghi", "a.b.c.def.ghi", true)]
	[DataRow("a.b.c ; (ghi)", "a.b", false)]
	[DataRow("a.b.c ; (ghi)", "a.b.c", true)]
	[DataRow("a.b.c ; (ghi)", "a.b.c.def.ghi", true)]
	[DataRow("abc ; g.h.i", "g.h", false)]
	[DataRow("abc ; g.h.i", "g.h.i", true)]
	[DataRow("abc ; g.h.i", "abc.def.g.h.i", true)]

	[DataRow("!abc", "abc.def.g.h.i", false)]
	[DataRow("abc & !def", "abc.def.g.h.i", false)]
	[DataRow("!abc & def", "abc.def.g.h.i", false)]
	[DataRow("abc & !defg", "abc.def.g.h.i", true)]
	[DataRow("!abc & defg", "abc.def.g.h.i", false)]
	[DataRow("abcd & !def", "abc.def.g.h.i", false)]
	[DataRow("!abcd & def", "abc.def.g.h.i", true)]
	[DataRow("abc | !def", "abc.def.g.h.i", true)]
	[DataRow("!abc | def", "abc.def.g.h.i", true)]
	[DataRow("abc | !defg", "abc.def.g.h.i", true)]
	[DataRow("!abc | defg", "abc.def.g.h.i", false)]
	[DataRow("abcd | !def", "abc.def.g.h.i", false)]
	[DataRow("!abcd | def", "abc.def.g.h.i", true)]

	// Chained NOT filters (used in CI to exclude multiple test classes)
	[DataRow("!abc & !def", "abc.test", false)]
	[DataRow("!abc & !def", "def.test", false)]
	[DataRow("!abc & !def", "ghi.test", true)]
	[DataRow("!abc & !def & !ghi", "abc.test", false)]
	[DataRow("!abc & !def & !ghi", "def.test", false)]
	[DataRow("!abc & !def & !ghi", "ghi.test", false)]
	[DataRow("!abc & !def & !ghi", "xyz.test", true)]
	[DataRow("!HotReloadTests & !SecondaryAppTests", "Uno.UI.RuntimeTests.Engine.HotReloadTests.Is_CodeHotReload_Enabled", false)]
	[DataRow("!HotReloadTests & !SecondaryAppTests", "Uno.UI.RuntimeTests.Engine.SecondaryAppTests.Is_From_A_Secondary_App", false)]
	[DataRow("!HotReloadTests & !SecondaryAppTests", "Uno.UI.RuntimeTests.Engine.SanityTests.Is_Sane", true)]
	public void When_ParseAndMatch(string filter, string method, bool expectedResult)
	{
		UnitTestFilter sut = filter;
		var result = sut.IsMatch(method);

		Assert.AreEqual(expectedResult, result);
	}
}