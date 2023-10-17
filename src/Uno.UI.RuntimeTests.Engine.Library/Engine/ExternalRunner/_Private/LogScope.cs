#if !UNO_RUNTIMETESTS_DISABLE_UI
using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RuntimeTests.Internal.Helpers;

internal static class LoggerExtensions
{
	public static LogScope CreateScopedLog(this Type type, string scopeName)
#if false
		=> new(type.Log(), type.Log().BeginScope(scopeName));
#else
		=> new(Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.CreateLogger(type.FullName + "#" + scopeName));
#endif

	public static LogScope Scope<T>(this ILogger log, string scopeName)
#if false
		=> new(log, log.BeginScope(scopeName));
#else
		=> new (Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.CreateLogger(typeof(T).FullName + "#" + scopeName));
#endif
}

internal readonly struct LogScope : IDisposable, ILogger
{
	private readonly ILogger _logger;
	private readonly IDisposable? _scope;

	public LogScope(ILogger logger, IDisposable? scope = null)
	{
		_logger = logger;
		_scope = scope;
	}

	/// <inheritdoc />
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		=> _logger.Log(logLevel, eventId, state, exception, formatter);

	/// <inheritdoc />
	public bool IsEnabled(LogLevel logLevel)
		=> _logger.IsEnabled(logLevel);

	/// <inheritdoc />
	public IDisposable BeginScope<TState>(TState state)
		=> _logger.BeginScope(state);

	/// <inheritdoc />
	public void Dispose()
		=> _scope?.Dispose();
}
#endif