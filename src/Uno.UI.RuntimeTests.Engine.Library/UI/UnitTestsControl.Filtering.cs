#if !UNO_RUNTIMETESTS_DISABLE_UI

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Windows.Devices.Input;
using static System.StringComparison;
using KnownTags = Uno.UI.RuntimeTests.UnitTestsControl.SearchPredicate.KnownTags;

namespace Uno.UI.RuntimeTests;

partial class UnitTestsControl
{
	private IEnumerable<UnitTestClassInfo> GetTestClasses(SearchPredicateCollection? filters)
	{
		var testAssemblies = AppDomain.CurrentDomain.GetAssemblies()
			.Where(x => x.GetName()?.Name?.EndsWith("Tests", OrdinalIgnoreCase) ?? false)
			.Concat(new[] { GetType().GetTypeInfo().Assembly })
			.Distinct();
		var types = testAssemblies
			.SelectMany(x => x.GetTypes())
			.Where(x => filters?.MatchAll(KnownTags.ClassName, x.Name) ?? true);

		if (_ciTestGroupCache != -1)
		{
			Console.WriteLine($"Filtering with group #{_ciTestGroupCache} (Groups {_ciTestsGroupCountCache})");
		}

		return
			from type in types
			where type.GetTypeInfo().GetCustomAttribute(typeof(TestClassAttribute)) != null
			where _ciTestsGroupCountCache == -1 || (_ciTestsGroupCountCache != -1 && (UnitTestsControl.GetTypeTestGroup(type) % _ciTestsGroupCountCache) == _ciTestGroupCache)
			orderby type.Name
			let info = BuildType(type)
			where info is { }
			select info;
	}
	private static IEnumerable<MethodInfo> GetTestMethods(UnitTestClassInfo @class, SearchPredicateCollection? filters)
	{
		var tests = @class.Tests
			.Select(x => new
			{
				Type = @class.Type.Name,
				x.Name,
				mc = filters?.MatchAll(KnownTags.Empty, @class.Type.Name),
				mm = filters?.MatchAll(KnownTags.Empty, x.Name),
			})
			.ToArray();
		return @class.Tests
			.Where(x => filters is { }
				? (filters.MatchAll(KnownTags.Empty, @class.Type.Name) || filters.MatchAll(KnownTags.Empty, x.Name))
				: true)
			.Where(x => filters?.MatchAll(KnownTags.MethodName, x.Name) ?? true)
			.Where(x => filters?.MatchAll(KnownTags.FullName, $"{@class.Type.FullName}.{x.Name}") ?? true);
	}
	private static IEnumerable<TestCase> GetTestCases(UnitTestMethodInfo method, SearchPredicateCollection? filters)
	{
		return
		(
			from source in method.DataSources // get [DataRow, DynamicData] attributes
			from data in source.GetData(method.Method) // flatten [DataRow, DynamicData] (mostly the latter)
			from injectedPointerType in method.InjectedPointerTypes.Cast<PointerDeviceType?>().DefaultIfEmpty()
			select new TestCase
			{
				Parameters = data,
				DisplayName = source.GetDisplayName(method.Method, data)
			}
		)
			// convert empty (without [DataRow, DynamicData]) to a single null case, so we dont lose them
			.DefaultIfEmpty()//.Select(x => x ?? new TestCase())
			.Select((x, i) => new
			{
				Case = x,
				Index = i,
				IsDataSourced = x is not null,
				Parameters = x?.Parameters.Any() == true ? string.Join(",", x.Parameters) : null,
			})
			.Where(x => filters?.MatchAllOnlyIfValuePresent(KnownTags.Parameters, x.Parameters) ?? true)
			.Where(x => filters?.MatchAllOnlyIf(KnownTags.AtIndex, x.IsDataSourced, y => MatchIndex(y, x.Index)) ?? true)
			// fixme: DisplayName will also matches parameters
			//     ^ because ITestDataSource.GetDisplayName returns args if DisplayName is not specified
			.Where(x => filters?.MatchAllOnlyIfValuePresent(KnownTags.DisplayName, x.Case?.DisplayName) ?? true)
			.Select(x => x.Case ?? new());

		bool MatchIndex(SearchPredicate predicate, int index)
		{
			return predicate.Parts.Any(x => x.Raw == index.ToString());
		}
	}

	public record SearchPredicate(string Raw, string Text, bool Exclusion = false, string? Tag = null, params SearchPredicatePart[] Parts)
	{
		public static class KnownTags
		{
			public const string? Empty = null;
			public const string ClassName = "class";
			public const string MethodName = "method";
			public const string FullName = "full_name";
			public const string DisplayName = "display_name";
			public const string AtIndex = "at";
			public const string Parameters = "params";
		}

		public static SearchPredicateComparer DefaultComparer = new();
		public static SearchQueryComparer DefaultQueryComparer = new();

		private static readonly IReadOnlyDictionary<string, string> NamespaceAliases = // aliases for tag
			new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
			{
				["c"] = KnownTags.ClassName,
				["m"] = KnownTags.MethodName,
				["f"] = KnownTags.FullName,
				["d"] = KnownTags.DisplayName,
				["p"] = KnownTags.Parameters,
			};
		private static readonly IReadOnlyDictionary<string, string> SpecialPrefixAliases = // prefixes are tags that don't need ':'
			new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
			{
				["@"] = KnownTags.AtIndex,
			};

		public static SearchPredicateCollection? ParseQuery(string? query)
		{
			if (string.IsNullOrWhiteSpace(query)) return null;

			return new SearchPredicateCollection(query
				!.SplitWithIgnore(' ', @""".*?(?<!\\)""", skipEmptyEntries: true)
				.Select(ParseFragment)
				.OfType<SearchPredicate>() // trim null
				.Where(x => x.Text.Length > 0) // ignore empty tag query eg: "c:"
				.ToArray()
			);
		}

		public static SearchPredicate? ParseFragment(string criteria)
		{
			if (string.IsNullOrWhiteSpace(criteria)) return null;

			var raw = criteria.Trim();
			var text = raw;
			if (text.StartsWith('-') is var exclusion && exclusion)
			{
				text = text.Substring(1);
			}
			var tag = default(string?);
			if (SpecialPrefixAliases.FirstOrDefault(x => text.StartsWith(x.Key)) is { Key.Length: > 0 } prefix)
			{
				// process prefixes (tags that dont need ':'): '@0' -> 'at:0'
				tag = prefix.Value;
				text = text.Substring(prefix.Key.Length);
			}
			else if (text.Split(':', 2) is { Length: 2 } tagParts)
			{
				// process tag aliases
				tag = NamespaceAliases.TryGetValue(tagParts[0], out var value) ? value : tagParts[0];
				text = tagParts[1];
			}
			var parts = text.SplitWithIgnore(',', @""".*?(?<!\\)""", skipEmptyEntries: false)
				.Select(SearchPredicatePart.Parse)
				.ToArray();

			return new(raw, text, exclusion, tag, parts);
		}

		public bool IsMatch(string input) =>
			Exclusion ^ // use xor to flip the result based on Exclusion
			Parts.Any(x => (x.MatchStart, x.MatchEnd) switch
			{
				(true, false) => input.StartsWith(x.Text, InvariantCultureIgnoreCase),
				(false, true) => input.EndsWith(x.Text, InvariantCultureIgnoreCase),

				_ => input.Contains(x.Text, InvariantCultureIgnoreCase),
			});

		public class SearchPredicateComparer : IEqualityComparer<SearchPredicate?>
		{
			public int GetHashCode(SearchPredicate? obj) => obj?.GetHashCode() ?? -1;
			public bool Equals(SearchPredicate? x, SearchPredicate? y)
			{
				return (x, y) switch
				{
					(null, null) => true,
					(null, _) => false,
					(_, null) => false,
					_ =>
						x.Raw == y.Raw &&
						x.Text == y.Text &&
						x.Exclusion == y.Exclusion &&
						x.Tag == y.Tag &&
						x.Parts.SequenceEqual(y.Parts),
				};
			}
		}

		public class SearchQueryComparer : IEqualityComparer<SearchPredicateCollection?>
		{
			public int GetHashCode(SearchPredicateCollection? obj) => obj?.GetHashCode() ?? -1;
			public bool Equals(SearchPredicateCollection? x, SearchPredicateCollection? y)
			{
				return (x, y) switch
				{
					(null, null) => true,
					(null, _) => false,
					(_, null) => false,
					_ => x.SequenceEqual(y, DefaultComparer),
				};
			}
		}
	}
	public record SearchPredicatePart(string Raw, string Text, bool MatchStart = false, bool MatchEnd = false)
	{
		public static SearchPredicatePart Parse(string part)
		{
			var raw = part;
			var text = raw;

			if (text.Length >= 2 && text.StartsWith('"') && text.EndsWith('"'))
			{
				// within quoted string, unquote and unescape \" to "
				text = text
					.Substring(1, text.Length - 2)
					.Replace("\\\"", "\"");
			}
			if (text.StartsWith("^") is { } matchStart && matchStart)
			{
				text = text.Substring(1);
			}
			if (text.EndsWith("$") is { } matchEnd && matchEnd)
			{
				text = text.Substring(0, text.Length - 1);
			}

			return new(raw, text, matchStart, matchEnd);
		}
	}
	public class SearchPredicateCollection : ReadOnlyCollection<SearchPredicate>
	{
		public SearchPredicateCollection(IList<SearchPredicate> list) : base(list) { }

		/// <summary>
		/// Match value against all filters of specified tag.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="value"></param>
		/// <returns>If all 'tag' filters matches the value, or true if the none filters are of 'tag'.</returns>
		public bool MatchAll(string? tag, string value) => this
			.Where(x => x.Tag == tag)
			.All(x => x.IsMatch(value));

		public bool MatchAllOnlyIfValuePresent(string? tag, string? value) =>
			MatchAllOnlyIf(tag, value is not null, x => x.IsMatch(value!));

		public bool MatchAllOnlyIf(string? tag, bool condition, Func<SearchPredicate, bool> match)
		{
			var tagFilters = this.Where(x => x.Tag == tag).ToArray();

			return (condition, tagFilters.Any()) switch
			{
				(true, true) => tagFilters.All(x => match(x)),
				(false, true) => false,
				(_, false) => true,
			};
		}
	}
}

#endif