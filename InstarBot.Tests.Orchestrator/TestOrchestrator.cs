using Discord;
using InstarBot.Test.Framework.Models;
using InstarBot.Test.Framework.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Services;
using Serilog;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using BindingFlags = System.Reflection.BindingFlags;
using InvalidOperationException = Amazon.CloudWatchLogs.Model.InvalidOperationException;

namespace InstarBot.Test.Framework
{
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public class TestOrchestrator
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly Snowflake _actor;
		public static TestServiceProviderBuilder Builder => new();

		public IGuildUser Actor {
			get
			{
				var user = GetUser(_actor);

				if (user is null)
				{
					if (GetService<IDiscordService>() is not TestDiscordService tds)
						throw new InvalidOperationException("Discord service was not mocked correctly.");

					tds.CreateUser(_actor);
					user = GetUser(_actor);
				}

				return user!;
			}
		}

		public TestGuildUser Subject { get; set; }

		public Snowflake GuildID => GetService<IDiscordService>().GetGuild().Id;

		// Shortcuts for common services
		public IDatabaseService Database => GetService<IDatabaseService>();
		public IDiscordService Discord => GetService<IDiscordService>();
		public IDynamicConfigService DynamicConfigService => GetService<IDynamicConfigService>();
		public TimeProvider TimeProvider => GetService<TimeProvider>();

		public InstarDynamicConfiguration Configuration => DynamicConfigService.GetConfig().Result;
		public TestMetricService Metrics => (TestMetricService) GetService<IMetricService>();
		public TestGuild Guild => (TestGuild) Discord.GetGuild();

		public IServiceProvider ServiceProvider => _serviceProvider;

		internal TestOrchestrator(IServiceProvider serviceProvider, Snowflake actor)
		{
			SetupLogging();

			_serviceProvider = serviceProvider;
			_actor = actor;

			InitializeActor();
			InitializeSubject(CreateUser());
		}

		internal TestOrchestrator(IServiceProvider serviceProvider, Snowflake actor, TestGuildUser subject)
		{
			SetupLogging();

			_serviceProvider = serviceProvider;
			_actor = actor;

			InitializeActor();
			InitializeSubject(subject);

			// We need to make sure the user is also in the DiscordService
			if (Discord.GetGuild() is not TestGuild tg)
				throw new InvalidOperationException("Discord service was not mocked correctly.");
			tg.AddUser(subject);
		}

		private void InitializeSubject(TestGuildUser subject)
		{
			subject.GuildId = GuildID;
			Subject = subject;

			if (GetService<IDatabaseService>() is not TestDatabaseService tdbs)
				throw new InvalidOperationException("Database service was not mocked correctly.");

			tdbs.CreateUserAsync(InstarUserData.CreateFrom(Subject)).Wait();
		}

		private void InitializeActor()
		{
			if (GetService<IDiscordService>() is not TestDiscordService tds)
				throw new InvalidOperationException("Discord service was not mocked correctly.");

			if (tds.GetUser(_actor) is not { } user)
				user = tds.CreateUser(_actor);

			if (GetService<IDatabaseService>() is not TestDatabaseService tdbs)
				throw new InvalidOperationException("Database service was not mocked correctly.");

			tdbs.CreateUserAsync(InstarUserData.CreateFrom(user)).Wait();
		}

		private static void SetupLogging()
		{
			Log.Logger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.MinimumLevel.Is(LogEventLevel.Verbose)
				.WriteTo.Console()
				.CreateLogger();
			Log.Warning("Logging is enabled for this unit test.");
		}

		public T GetService<T>() where T : class {
			return _serviceProvider.GetRequiredService<T>();
		}

		public IGuildUser? GetUser(Snowflake snowflake)
		{
			var discordService = _serviceProvider.GetRequiredService<IDiscordService>();

			return discordService.GetUser(snowflake);
		}

		public Mock<T> GetCommand<T>(Func<T> constructor) where T : BaseCommand
		{
			var constructors = typeof(T).GetConstructors();

			var mock = new Mock<T>(() => constructor());
			var executionChannel = Snowflake.Generate();

			if (Discord is not TestDiscordService tds)
				throw new InvalidOperationException("Discord service is not an instance of TestDiscordService");

			tds.CreateChannel(executionChannel);

			mock.SetupGet(obj => obj.Context).Returns(new TestInteractionContext(this, _actor, executionChannel));

			return mock;
		}

		public Mock<T> GetCommand<T>() where T : BaseCommand
		{
			var constructors = typeof(T).GetConstructors();
			
			// Sift through the constructors to find one that works
			foreach (var constructor in constructors)
			{
				var parameters = constructor.GetParameters().Select(n => n.ParameterType).Select(ty => _serviceProvider.GetService(ty)).ToList();

				if (!parameters.All(obj => obj is not null))
					continue;

				var executionChannel = Snowflake.Generate();

				if (Discord is not TestDiscordService tds)
					throw new InvalidOperationException("Discord service is not an instance of TestDiscordService");

				tds.CreateChannel(executionChannel);

				var mock = new Mock<T>(parameters.ToArray()!);

				mock.SetupGet(obj => obj.Context).Returns(new TestInteractionContext(this, _actor, executionChannel));

				return mock;
			}

			throw new InvalidOperationException($"Failed to find a suitable constructor for {typeof(T).FullName}!");
		}

		public TestChannel GetChannel(Snowflake channelId)
		{
			return Discord.GetChannel(channelId).Result as TestChannel ?? throw new InvalidOperationException("Channel was not registered for mocking.");
		}

		public void SetTime(DateTimeOffset date)
		{
			if (GetService<TimeProvider>() is not TestTimeProvider testTimeProvider)
				throw new InvalidOperationException("Time provider was not an instance of TestTimeProvider");

			testTimeProvider.SetTime(date);
		}

		public static TestOrchestrator Default => Builder.Build().Result;

		public TestGuildUser CreateUser()
		{
			if (Discord is not TestDiscordService tds)
				throw new InvalidOleVariantTypeException("Discord service was not an instance of TestDiscordService");

			return tds.CreateUser(Snowflake.Generate());
		}

		public async Task CreateAutoMemberHold(IGuildUser user, string reason = "Test reason")
		{
			var dbUser = await Database.GetOrCreateUserAsync(user);
			dbUser.Data.AutoMemberHoldRecord = new AutoMemberHoldRecord
			{
				Date = TimeProvider.GetUtcNow().UtcDateTime,
				ModeratorID = Actor.Id,
				Reason = reason
			};
			await dbUser.CommitAsync();
		}

		public TestChannel CreateChannel(Snowflake channelId)
		{
			TestGuild guild = (TestGuild) Discord.GetGuild();
			var channel = new TestChannel(channelId);
			guild.AddChannel(channel);

			return channel;
		}
	}
}
