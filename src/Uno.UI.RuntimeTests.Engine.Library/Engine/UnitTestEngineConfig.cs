#nullable enable

using System;
using System.Linq;

namespace Uno.UI.RuntimeTests;


#if !UNO_RUNTIMETESTS_DISABLE_UI || !UNO_RUNTIMETESTS_DISABLE_EMBEDDEDRUNNER

public record UnitTestEngineConfig
{
	public const int DefaultRepeatCount = 3;

	public static UnitTestEngineConfig Default { get; } = new();

	public UnitTestFilter? Filter { get; init; }

	public int Attempts { get; init; } = DefaultRepeatCount;

	public bool IsConsoleOutputEnabled { get; init; }

	public bool IsRunningIgnored { get; init; }
}

#endif