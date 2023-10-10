#if !UNO_RUNTIMETESTS_DISABLE_UI || !UNO_RUNTIMETESTS_DISABLE_RC
using System;
using System.Linq;
using System.Reflection;

namespace Uno.UI.RuntimeTests;

public record UnitTestFilter
{
	private readonly IUnitTestEngineFilter _filter;
	private static readonly char[] _separators = { '(', ')', '&', '|', ';' };

	private UnitTestFilter(IUnitTestEngineFilter filter)
	{
		_filter = filter;
	}

	public UnitTestFilter()
	{
		_filter = new NullFilter();
	}

	public string Value
	{
		get => ToString();
		init => _filter = Parse(value);
	}

	public bool IsMatch(MethodInfo method) => _filter.IsMatch($"{method.DeclaringType?.FullName}.{method.Name}");
	public bool IsMatch(string methodFullname) => _filter.IsMatch(methodFullname);

	public override string ToString() => _filter.ToString() ?? string.Empty;

	public static implicit operator UnitTestFilter(string? syntax) => new(Parse(syntax));

	public static implicit operator string(UnitTestFilter? filter) => filter?.ToString() ?? string.Empty;

	private static IUnitTestEngineFilter Parse(string? syntax)
	{
		if (string.IsNullOrWhiteSpace(syntax))
		{
			return new NullFilter();
		}
		else
		{
			var index = 0;
			return Parse(ref index, syntax.AsSpan());
		}
	}

	private static IUnitTestEngineFilter Parse(ref int index, ReadOnlySpan<char> syntax)
	{
		// Simple syntax parser ... that does not have any operator priority!

		var pending = default(IUnitTestEngineFilter?);
		for (; index < syntax.Length; index++)
		{
			switch (syntax[index])
			{
				case '(':
					index++;
					pending = Parse(ref index, syntax);
					break;

				//case ' ':
				//	break;

				case '&' when pending is not null:
					index++;
					pending = new AndFilter(pending, Parse(ref index, syntax));
					break;

				case ';' when pending is not null: // Legacy support
				case '|' when pending is not null:
					index++;
					pending = new OrFilter(pending, Parse(ref index, syntax));
					break;

				case ')' when pending is not null:
					return pending;

				case ')':
					return new NullFilter();

				default:
				{
					var j = index;
					for (; j < syntax.Length && !_separators.Contains(syntax[j]); j++) { }
					pending = new TextFilter(syntax.Slice(index, j - index).ToString().Trim());
					index = j - 1;
					break;
				}
			}
		}

		return pending ?? default(NullFilter);
	}

	private interface IUnitTestEngineFilter
	{
		bool IsMatch(string methodFullname);
	}

	private readonly record struct AndFilter(IUnitTestEngineFilter Left, IUnitTestEngineFilter Right) : IUnitTestEngineFilter
	{
		public bool IsMatch(string methodFullname) => Left.IsMatch(methodFullname) && Right.IsMatch(methodFullname);
		public override string ToString() => $"({Left} & {Right})";
	}

	private readonly record struct OrFilter(IUnitTestEngineFilter Left, IUnitTestEngineFilter Right) : IUnitTestEngineFilter
	{
		public bool IsMatch(string methodFullname) => Left.IsMatch(methodFullname) || Right.IsMatch(methodFullname);
		public override string ToString() => $"({Left} | {Right})";
	}

	private readonly record struct TextFilter(string Text) : IUnitTestEngineFilter
	{
		public bool IsMatch(string methodFullname) => methodFullname.Contains(Text, StringComparison.InvariantCultureIgnoreCase);
		public override string ToString() => Text;
	}

	private readonly struct NullFilter : IUnitTestEngineFilter
	{
		public bool IsMatch(string methodFullname) => true;
		public override string ToString() => string.Empty;
	}
}
#endif