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
	public void When_ParseAndMatch(string filter, string method, bool expectedResult)
	{
		UnitTestFilter sut = filter;
		var result = sut.IsMatch(method);

		Assert.AreEqual(expectedResult, result);
	}
}