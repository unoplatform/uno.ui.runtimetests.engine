using System.Reflection;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.TestHost;
using Microsoft.Testing.Platform.TestHostControllers;
using Microsoft.Testing.Platform.TestHostOrchestrator;

namespace Uno.HotTesting.Engine;

public sealed class MSTestEngine(IReadOnlyCollection<Assembly> assemblies, Action<ITestApplicationBuilder>? configureTestBuilder = null, string[]? additionalBuilderArguments = null)
{
	public async Task<IReadOnlyList<TestNode>> DiscoverTestsAsync()
	{
		var discovered = new List<TestNode>();
		var inner = await TestApplication.CreateBuilderAsync([.. additionalBuilderArguments ?? [], "--list-tests"]);
		var builder = new CapturingTestApplicationBuilder<DiscoverTestExecutionRequest>(inner, (dataProducer, data) =>
		{
			if (data is TestNodeUpdateMessage { TestNode: var node } &&
					node.Properties.SingleOrDefault<TestNodeStateProperty>() is DiscoveredTestNodeStateProperty)
			{
				lock (discovered)
					discovered.Add(node);		
			}
		});
		builder.AddMSTest(() => assemblies);
		configureTestBuilder?.Invoke(builder);

		using var app = await builder.BuildAsync();
		await app.RunAsync();
		return discovered;
	}

	public Task<IReadOnlyList<TestNode>> RunTestsAsync(IEnumerable<TestNode> tests)
        => RunTestsAsync(tests.Select(t => t.Uid.Value));

	public async Task<IReadOnlyList<TestNode>> RunTestsAsync(IEnumerable<string> uids)
	{
		var ran = new List<TestNode>();
		var inner = await Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync([.. additionalBuilderArguments ?? [], "--filter-uid", .. uids]);
		var builder = new CapturingTestApplicationBuilder<RunTestExecutionRequest>(inner, (dataProducer, data) =>
		{
			if (data is TestNodeUpdateMessage { TestNode: var node })
			{
				lock (ran)
					ran.Add(node);		
			}
		});
		builder.AddMSTest(() => assemblies);
		configureTestBuilder?.Invoke(builder);

		using var app = await builder.BuildAsync();
		await app.RunAsync();
		return ran;
	}
}

#pragma warning disable TPEXP // TestHostOrchestrator, Configuration, Logging, IExecuteRequestCompletionNotifier are experimental
sealed class CapturingTestApplicationBuilder<TExecutionRequest>(ITestApplicationBuilder inner, Action<IDataProducer, IData> onPublish) : ITestApplicationBuilder
{
	public ITestHostManager TestHost => inner.TestHost;
	public ITestHostControllersManager TestHostControllers => inner.TestHostControllers;
	public ITestHostOrchestratorManager TestHostOrchestrator => inner.TestHostOrchestrator;
	public ICommandLineManager CommandLine => inner.CommandLine;
	public IConfigurationManager Configuration => inner.Configuration;
	public ILoggingManager Logging => inner.Logging;

	public ITestApplicationBuilder RegisterTestFramework(
		Func<IServiceProvider, ITestFrameworkCapabilities> capabilitiesFactory,
		Func<ITestFrameworkCapabilities, IServiceProvider, ITestFramework> frameworkFactory)
	{
		inner.RegisterTestFramework(capabilitiesFactory,
			(capabilities, services) => new CapturingTestFramework<TExecutionRequest>(frameworkFactory(capabilities, services), onPublish));
		return this;
	}

	public Task<ITestApplication> BuildAsync() => inner.BuildAsync();
}

sealed class CapturingTestFramework<TExecutionRequest>(ITestFramework inner, Action<IDataProducer, IData> onPublish)
	: ITestFramework, IDataProducer, IAsyncInitializableExtension, IAsyncCleanableExtension
{
	public string Uid => inner.Uid;
	public string Version => inner.Version;
	public string DisplayName => inner.DisplayName;
	public string Description => inner.Description;
	public Type[] DataTypesProduced => (inner as IDataProducer)?.DataTypesProduced ?? [];

	public Task<bool> IsEnabledAsync() => inner.IsEnabledAsync();
	public Task InitializeAsync() => (inner as IAsyncInitializableExtension)?.InitializeAsync() ?? Task.CompletedTask;
	public Task CleanupAsync() => (inner as IAsyncCleanableExtension)?.CleanupAsync() ?? Task.CompletedTask;

	public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context) => inner.CreateTestSessionAsync(context);
	public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context) => inner.CloseTestSessionAsync(context);

	public Task ExecuteRequestAsync(ExecuteRequestContext context)
	{
		if (context.Request is not TExecutionRequest)
			return inner.ExecuteRequestAsync(context);

		var wrapped = new ExecuteRequestContext(
			context.Request,
			new CapturingMessageBus(context.MessageBus, onPublish),
			new CompletionForwarder(context),
			context.CancellationToken);
		return inner.ExecuteRequestAsync(wrapped);
	}

	sealed class CompletionForwarder(ExecuteRequestContext original) : IExecuteRequestCompletionNotifier
	{
		public void Complete() => original.Complete();
	}

	sealed class CapturingMessageBus(IMessageBus inner, Action<IDataProducer, IData> onPublish) : IMessageBus
	{
		public Task PublishAsync(IDataProducer dataProducer, IData data)
		{
			onPublish(dataProducer, data);
			return inner.PublishAsync(dataProducer, data);
		}
	}
}
