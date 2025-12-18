using Discord;
using InstarBot.Test.Framework.Models;
using InstarBot.Test.Framework.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.Services;
using InvalidOperationException = Amazon.CloudWatchLogs.Model.InvalidOperationException;

namespace InstarBot.Test.Framework;

public class TestServiceProviderBuilder
{
	private const string DefaultConfigPath = "Config/Instar.dynamic.test.debug.conf.json";

	private readonly Dictionary<Type, object> _serviceRegistry = new();
	private readonly Dictionary<Type, Type> _serviceTypeRegistry = new();
	private readonly HashSet<Type> _componentRegistry = new();
	private string _configPath = DefaultConfigPath;
	private TestDiscordContextBuilder? _discordContextBuilder = null;
	private TestDatabaseContextBuilder? _databaseContextBuilder = null;
	private readonly Dictionary<Type, Snowflake> _interactionCallerIds = new();
	private Snowflake _actor = Snowflake.Generate();

	private TestGuildUser _subject = new(Snowflake.Generate());

	public TestServiceProviderBuilder WithConfigPath(string configPath)
	{
		_configPath = configPath;
		return this;
	}

	public TestServiceProviderBuilder WithService<T, V>()
	{
		_serviceTypeRegistry[typeof(T)] = typeof(V);
		return this;
	}

	public TestServiceProviderBuilder WithService<T>(Mock<T> serviceMock) where T : class
	{
		return WithService(serviceMock.Object);
	}

	public TestServiceProviderBuilder WithService<T>(T serviceMock) where T : class
	{
		_serviceRegistry[typeof(T)] = serviceMock;
		return this;
	}

	public TestServiceProviderBuilder WithTime(DateTimeOffset? time)
	{
		return time is null ? this : WithService<TimeProvider>(new TestTimeProvider((DateTimeOffset) time));
	}

	public TestServiceProviderBuilder WithDiscordContext(Func<TestDiscordContextBuilder, TestDiscordContextBuilder> builderExpr)
	{
		_discordContextBuilder = builderExpr(TestDiscordContext.Builder);
		return this;
	}

	public TestServiceProviderBuilder WithDatabase(Func<TestDatabaseContextBuilder, TestDatabaseContextBuilder> builderExpr)
	{
		_databaseContextBuilder ??= new TestDatabaseContextBuilder(ref _discordContextBuilder);
		_databaseContextBuilder = builderExpr(_databaseContextBuilder);
		return this;
	}

	public TestServiceProviderBuilder WithActor(Snowflake userId)
	{
		_actor = userId;
		return this;
	}

	public TestServiceProviderBuilder WithSubject(TestGuildUser user)
	{
		_subject = user;
		return this;
	}

	public async Task<TestOrchestrator> Build()
	{
		var services = new ServiceCollection();
		foreach (var (type, implementation) in _serviceRegistry)
			services.AddSingleton(type, implementation);
		foreach (var (iType, implType) in _serviceTypeRegistry)
			services.AddSingleton(iType, implType);
		foreach (var type in _componentRegistry)
			services.AddTransient(type);

		IDynamicConfigService configService;
		if (_serviceRegistry.TryGetValue(typeof(IDynamicConfigService), out var registeredService) &&
		    registeredService is IDynamicConfigService resolved)
			configService = resolved;
		else
			configService = new TestDynamicConfigService(_configPath);

		_discordContextBuilder ??= TestDiscordContext.Builder;
		await _discordContextBuilder.LoadFromConfig(configService);

		// Register default services
		RegisterDefaultService(services, configService);
		RegisterDefaultService<TimeProvider>(services, TestTimeProvider.System);
		RegisterDefaultService<IDatabaseService, TestDatabaseService>(services);
		RegisterDefaultService<IDiscordService>(services, new TestDiscordService(_discordContextBuilder.Build()));
		RegisterDefaultService<IGaiusAPIService, TestGaiusAPIService>(services);
		RegisterDefaultService<IMetricService, TestMetricService>(services);
		RegisterDefaultService<IAutoMemberSystem, TestAutoMemberSystem>(services);
		RegisterDefaultService<IBirthdaySystem, BirthdaySystem>(services);
		RegisterDefaultService<TeamService>(services);

		return new TestOrchestrator(services.BuildServiceProvider(), _actor, _subject);
	}

	private void RegisterDefaultService<T>(ServiceCollection collection) where T : class
	{
		if (!_serviceRegistry.ContainsKey(typeof(T)) && !_serviceTypeRegistry.ContainsKey(typeof(T)))
			collection.AddSingleton<T>();
	}

	private void RegisterDefaultService<T>(ServiceCollection collection, T impl) where T : class
	{
		if (!_serviceRegistry.ContainsKey(typeof(T)) && !_serviceTypeRegistry.ContainsKey(typeof(T)))
			collection.AddSingleton(impl);
	}

	private void RegisterDefaultService<T, V>(ServiceCollection collection) where T : class where V : class, T
	{
		if (!_serviceRegistry.ContainsKey(typeof(T)) && !_serviceTypeRegistry.ContainsKey(typeof(T)))
			collection.AddSingleton<T, V>();
	}
}