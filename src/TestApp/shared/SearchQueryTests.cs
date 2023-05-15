using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Engine.Extensions;
using static Uno.UI.RuntimeTests.UnitTestsControl;

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
public partial class SearchQueryTests
{
	[TestMethod]
	[DataRow("asd")] // simple
	[DataRow("^asd")] // match start
	[DataRow("asd$")] // match end
	[DataRow("^asd$")] // full match
	public void When_SearchPredicatePart_Parse(string input)
	{
		var actual = SearchPredicatePart.Parse(input);
		var expected = input switch
		{
			"asd" => new SearchPredicatePart(input, "asd"),
			"^asd" => new SearchPredicatePart(input, "asd", MatchStart: true),
			"asd$" => new SearchPredicatePart(input, "asd", MatchEnd: true),
			"^asd$" => new SearchPredicatePart(input, "asd", MatchStart: true, MatchEnd: true),

			_ => throw new ArgumentOutOfRangeException(input),
		};

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow("asd")] // simple
	[DataRow("^asd,qwe$,^zxc$")] // multi parts
	[DataRow("-asd")] // exclusion
	[DataRow("tag:asd")] // tagged
	public void When_SearchPredicate_ParseFragment(string input)
	{
		var actual = SearchPredicate.ParseFragment(input);
		var expected = input switch
		{
			"asd" => new SearchPredicate("asd", "asd", Parts: new SearchPredicatePart("asd", "asd")),
			"^asd,qwe$,^zxc$" => new SearchPredicate("^asd,qwe$,^zxc$", "^asd,qwe$,^zxc$", Parts: new[]{
				new SearchPredicatePart("^asd", "asd", MatchStart: true),
				new SearchPredicatePart("qwe$", "qwe", MatchEnd: true),
				new SearchPredicatePart("^zxc$", "zxc", MatchStart: true, MatchEnd: true),
			}),
			"-asd" => new SearchPredicate("-asd", "asd", Exclusion: true, Parts: new[]{
				new SearchPredicatePart("asd", "asd"),
			}),
			"tag:asd" => new SearchPredicate("tag:asd", "asd", Tag: "tag", Parts: new[]{
				new SearchPredicatePart("asd", "asd"),
			}),

			_ => throw new ArgumentOutOfRangeException(input),
		};

		Assert.That.AreEqual(expected, actual, SearchPredicate.DefaultComparer);
	}

	[TestMethod]
	[DataRow("asd")] // simple
	[DataRow("asd qwe")] // multi fragments
	[DataRow("class:asd method:qwe display_name:zxc -123")] // tags
	[DataRow("c:asd m:qwe d:zxc -123")] // aliased tags
	[DataRow("d:\"^asd \\\", asd$\"")] // quoted with escape
	[DataRow("at:asd\"asd\"asd,^asd,asd$")] // inner literal quote
	[DataRow("asd @0")] // custom prefix
	[DataRow("p:\"index 0\",\"index 1\" -p:\"asd\"")] // multiple quotes
	
	public void When_SearchPredicate_Parse(string input)
	{
		var actual = SearchPredicate.ParseQuery(input);
		var predicates = input switch
		{
			"asd" => new SearchPredicate[] { new("asd", "asd", Parts: new SearchPredicatePart[] { new("asd", "asd"), }), },
			"asd qwe" => new SearchPredicate[] {
				new("asd", "asd", Parts: new SearchPredicatePart[] { new("asd", "asd"), }),
				new("qwe", "qwe", Parts: new SearchPredicatePart[] { new("qwe", "qwe"), }),
			},
			"class:asd method:qwe display_name:zxc -123" => new SearchPredicate[] {
				new("class:asd", "asd", Tag: "class", Parts: new SearchPredicatePart[] { new("asd", "asd"), }),
				new("method:qwe", "qwe", Tag: "method", Parts: new SearchPredicatePart[] { new("qwe", "qwe"), }),
				new("display_name:zxc", "zxc", Tag: "display_name", Parts: new SearchPredicatePart[] { new("zxc", "zxc"), }),
				new("-123", "123", Exclusion: true, Parts: new SearchPredicatePart[] { new("123", "123"), }),
			},
			"c:asd m:qwe d:zxc -123" => new SearchPredicate[] {
				new("c:asd", "asd", Tag: "class", Parts: new SearchPredicatePart[] { new("asd", "asd"), }),
				new("m:qwe", "qwe", Tag: "method", Parts: new SearchPredicatePart[] { new("qwe", "qwe"), }),
				new("d:zxc", "zxc", Tag: "display_name", Parts: new SearchPredicatePart[] { new("zxc", "zxc"), }),
				new("-123", "123", Exclusion: true, Parts: new SearchPredicatePart[] { new("123", "123"), }),
			},
			"d:\"^asd \\\", asd$\"" => new SearchPredicate[] {
				new("d:\"^asd \\\", asd$\"", "\"^asd \\\", asd$\"", Tag: "display_name", Parts: new SearchPredicatePart[] {
					new("\"^asd \\\", asd$\"", "asd \", asd", MatchStart: true, MatchEnd: true),
				}),
			},
			"at:asd\"asd\"asd,^asd,asd$" => new SearchPredicate[] {
				new("at:asd\"asd\"asd,^asd,asd$", "asd\"asd\"asd,^asd,asd$", Tag: "at", Parts: new SearchPredicatePart[] {
					new("asd\"asd\"asd", "asd\"asd\"asd"),
					new ("^asd", "asd", MatchStart: true),
					new ("asd$", "asd", MatchEnd: true),
				}),
			},
			"asd @0" => new SearchPredicate[] {
				new("asd", "asd", Parts: new SearchPredicatePart[] { new ("asd", "asd"), }),
				new("@0", "0", Tag: "at", Parts: new SearchPredicatePart[] { new ("0", "0"), }),
			},
			"p:\"index 0\",\"index 1\" -p:\"asd\"" => new SearchPredicate[] {
				new("p:\"index 0\",\"index 1\"", "\"index 0\",\"index 1\"", Tag: "params", Parts: new SearchPredicatePart[] { new("\"index 0\"", "index 0"), new("\"index 1\"", "index 1") }),
				new("-p:\"asd\"", "\"asd\"", Exclusion: true, Tag: "params", Parts: new SearchPredicatePart[] { new("\"asd\"", "asd") })
			},

			_ => throw new ArgumentOutOfRangeException(input),
		};
		var expected = new SearchPredicateCollection(predicates!);

		Assert.That.AreEqual(expected, actual, SearchPredicate.DefaultQueryComparer);
	}
}
