#nullable enable

using System;
using System.Linq;
using Windows.Devices.Input;

namespace Uno.UI.RuntimeTests;

#if !UNO_RUNTIMETESTS_DISABLE_UI

internal record TestCase
{
	public object[] Parameters { get; init; } = Array.Empty<object>();

	public string? DisplayName { get; init; }

	public PointerDeviceType? Pointer { get; init; }

	/// <inheritdoc />
	public override string ToString()
	{
		var result = $"({string.Join(",", Parameters.Select(p => p?.ToString() ?? "<null>"))})";

		if (Pointer is {} pt)
		{
			result += $" [{pt}]";
		}

		return result;
	}
}

#endif