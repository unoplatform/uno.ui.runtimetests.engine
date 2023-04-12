using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.UI.RuntimeTests.Extensions;

internal static partial class StringExtensions
{
	/// <summary>
	/// Like <see cref="string.Split(char[])"/>, but allows exception to be made with a Regex pattern.
	/// </summary>
	/// <param name="input"></param>
	/// <param name="separator"></param>
	/// <param name="ignoredPattern">segments matched by the regex will not be splited.</param>
	/// <param name="skipEmptyEntries"></param>
	/// <returns></returns>
	public static string[] SplitWithIgnore(this string input, char separator, string ignoredPattern, bool skipEmptyEntries)
	{
		var ignores = Regex.Matches(input, ignoredPattern);

		var shards = new List<string>();
		for (int i = 0; i < input.Length; i++)
		{
			var nextSpaceDelimiter = input.IndexOf(separator, i);

			// find the next space, if inside a quote
			while (nextSpaceDelimiter != -1 && ignores.FirstOrDefault(x => InRange(x, nextSpaceDelimiter)) is { } enclosingIgnore)
			{
				nextSpaceDelimiter = enclosingIgnore.Index + enclosingIgnore.Length is { } afterIgnore && afterIgnore < input.Length
					? input.IndexOf(separator, afterIgnore)
					: -1;
			}

			if (nextSpaceDelimiter != -1)
			{
				shards.Add(input.Substring(i, nextSpaceDelimiter - i));
				i = nextSpaceDelimiter;

				// skip multiple continuous spaces
				while (skipEmptyEntries && i + 1 < input.Length && input[i + 1] == separator) i++;
			}
			else
			{
				shards.Add(input.Substring(i));
				break;
			}
		}

		return shards.ToArray();

		bool InRange(Match x, int index) => x.Index <= index && index < (x.Index + x.Length);
	}
}
