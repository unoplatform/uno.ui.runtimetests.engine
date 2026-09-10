#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

#if !UNO_RUNTIMETESTS_DISABLE_UI
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests;

/// <summary>
/// <see cref="TestContext"/> implementation assigned to the <c>TestContext</c> property of a test class
/// instance before each test invocation, so that <see cref="TestContext.CancellationToken"/> reflects the
/// test's <see cref="TimeoutAttribute"/> when <see cref="TimeoutAttribute.CooperativeCancellation"/> is set.
/// </summary>
internal sealed class UnitTestContext : TestContext
{
	private readonly Dictionary<string, object?> _properties = new();
	private readonly CancellationTokenSource? _cancellationTokenSource;

	public UnitTestContext(string testName, string testDisplayName, string fullyQualifiedTestClassName, CancellationTokenSource? cancellationTokenSource = null)
	{
		TestName = testName;
		FullyQualifiedTestClassName = fullyQualifiedTestClassName;
		TestDisplayName = testDisplayName;
		_cancellationTokenSource = cancellationTokenSource;
	}

	public override IDictionary<string, object?> Properties => _properties;

	public override string TestName { get; }

	public override string FullyQualifiedTestClassName { get; }

	public override CancellationTokenSource CancellationTokenSource => _cancellationTokenSource ?? base.CancellationTokenSource;

	public override void AddResultFile(string fileName)
	{
	}

	public override void Write(string? message) => Console.Write(message);

	public override void Write(string format, params object?[] args) => Console.Write(format, args);

	public override void WriteLine(string? message) => Console.WriteLine(message);

	public override void WriteLine(string format, params object?[] args) => Console.WriteLine(format, args);

	public override void DisplayMessage(MessageLevel messageLevel, string message) => Console.WriteLine(message);
}
#endif
