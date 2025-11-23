using System.Linq.Expressions;
using Discord;
using Discord.Interactions;
using FluentAssertions;
using InstarBot.Tests.Models;
using InstarBot.Tests.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.Services;
using Serilog;
using Serilog.Events;

namespace InstarBot.Tests;

public static class TestUtilities
{
    private static IConfiguration? _config;
    private static IDynamicConfigService? _dynamicConfig;

    private static IConfiguration GetTestConfiguration()
    {
        if (_config is not null)
            return _config;

        _config = new ConfigurationBuilder()
            .AddJsonFile("Config/Instar.test.bare.conf.json")
            .Build();

        return _config;
    }

    public static IDynamicConfigService GetDynamicConfiguration()
    {
        if (_dynamicConfig is not null)
            return _dynamicConfig;

        #if DEBUG
        var conf = new MockDynamicConfigService("Config/Instar.dynamic.test.debug.conf.json");
        #else
        var conf = new MockDynamicConfigService("Config/Instar.dynamic.test.conf.json");
        #endif

        _dynamicConfig = conf;
        return conf;
    }

    public static TeamService GetTeamService()
    {
        return new TeamService(GetDynamicConfiguration());
    }

    public static IServiceProvider GetServices()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(GetTestConfiguration());
        sc.AddSingleton(GetTeamService());
        sc.AddSingleton<IInstarDDBService, MockInstarDDBService>();
        sc.AddSingleton(GetDynamicConfiguration());

        return sc.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that the command responded to the user with the correct <paramref name="message"/>.
    /// </summary>
    /// <param name="command">A mockup of the command.</param>
    /// <param name="message">The message to check for.</param>
    /// <param name="ephemeral">A flag indicating whether the message should be ephemeral.</param>
    /// <typeparam name="T">The type of command.  Must implement <see cref="InteractionModuleBase&lt;T&gt;"/>.</typeparam>
    public static void VerifyMessage<T>(Mock<T> command, string message, bool ephemeral = false)
        where T : BaseCommand
    {
        command.Protected().Verify(
            "RespondAsync", Times.Once(),
            message, ItExpr.IsAny<Embed[]>(),
            false, ephemeral, ItExpr.IsAny<AllowedMentions>(), ItExpr.IsAny<RequestOptions>(),
            ItExpr.IsAny<MessageComponent>(), ItExpr.IsAny<Embed>(), ItExpr.IsAny<PollProperties>(), ItExpr.IsAny<MessageFlags>());
    }

    public static IDiscordService SetupDiscordService(TestContext context = null!)
        => new MockDiscordService(SetupGuild(context));

    public static IGaiusAPIService SetupGaiusAPIService(TestContext context = null!)
        => new MockGaiusAPIService(context.Warnings, context.Caselogs, context.InhibitGaius);

    private static TestGuild SetupGuild(TestContext context = null!)
    {
        var guild = new TestGuild
        {
            Id = Snowflake.Generate(),
            TextChannels = context.Channels.Values,
            Roles = context.Roles.Values,
            Users = context.GuildUsers
        };

        return guild;
    }

    public static Mock<T> SetupCommandMock<T>(Expression<Func<T>> newExpression, TestContext context = null!)
        where T : BaseCommand
    {
        var commandMock = new Mock<T>(newExpression);
        ConfigureCommandMock(commandMock, context);
        return commandMock;
    }

    public static Mock<T> SetupCommandMock<T>(TestContext context = null!)
        where T : BaseCommand
    {
        // Quick check: Do we have a constructor that takes IConfiguration?
        var iConfigCtor = typeof(T).GetConstructors()
            .Any(n => n.GetParameters().Any(info => info.ParameterType == typeof(IConfiguration)));

        var commandMock = iConfigCtor ? new Mock<T>(GetTestConfiguration()) : new Mock<T>();
        ConfigureCommandMock(commandMock, context);
        return commandMock;
    }

    private static void ConfigureCommandMock<T>(Mock<T> mock, TestContext? context)
        where T : BaseCommand
    {
        context ??= new TestContext();

        mock.SetupGet<InstarContext>(n => n.Context).Returns(SetupContext(context).Object);

        mock.Protected().Setup<Task>("RespondAsync", ItExpr.IsNull<string>(), ItExpr.IsNull<Embed[]>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(), ItExpr.IsNull<AllowedMentions>(), ItExpr.IsNull<RequestOptions>(),
                ItExpr.IsNull<MessageComponent>(),
                ItExpr.IsNull<Embed>(),
                ItExpr.IsNull<PollProperties>(),
				ItExpr.IsNull<MessageFlags>())
            .Returns(Task.CompletedTask);
    }

    public static Mock<InstarContext> SetupContext(TestContext context)
    {
        var mock = new Mock<InstarContext>();

        mock.SetupGet(static n => n.User!).Returns(SetupUserMock<IGuildUser>(context).Object);
        mock.SetupGet(static n => n.Channel!).Returns(SetupChannelMock<ITextChannel>(context).Object);
        // Note: The following line must occur after the mocking of GetChannel.
        mock.SetupGet(static n => n.Guild).Returns(SetupGuildMock(context).Object);

        return mock;
    }

    private static Mock<IInstarGuild> SetupGuildMock(TestContext? context)
    {
        context.Should().NotBeNull();

        var guildMock = new Mock<IInstarGuild>();
        guildMock.Setup(n => n.Id).Returns(TestContext.GuildID);
        guildMock.Setup(n => n.GetTextChannel(It.IsAny<ulong>()))
            .Returns(context.TextChannelMock.Object);

        return guildMock;
    }

    public static Mock<T> SetupUserMock<T>(ulong userId)
        where T : class, IUser
    {
        var userMock = new Mock<T>();
        userMock.Setup(n => n.Id).Returns(userId);

        return userMock;
    }

    private static Mock<T> SetupUserMock<T>(TestContext? context)
        where T : class, IUser
    {
        var userMock = SetupUserMock<T>(context!.UserID);

        if (typeof(T) == typeof(IGuildUser))
            userMock.As<IGuildUser>().Setup(n => n.RoleIds).Returns(context.UserRoles.Select(n => n.ID).ToList);

        return userMock;
    }

    public static Mock<T> SetupChannelMock<T>(ulong channelId)
        where T : class, IChannel
    {
        var channelMock = new Mock<T>();
        channelMock.Setup(n => n.Id).Returns(channelId);

        return channelMock;
    }

    private static Mock<T> SetupChannelMock<T>(TestContext context)
        where T : class, IChannel
    {
        var channelMock = SetupChannelMock<T>(TestContext.ChannelID);

        if (typeof(T) != typeof(ITextChannel))
            return channelMock;

        channelMock.As<ITextChannel>().Setup(n => n.SendMessageAsync(It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<Embed>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<AllowedMentions>(),
                It.IsAny<MessageReference>(),
                It.IsAny<MessageComponent>(),
                It.IsAny<ISticker[]>(),
                It.IsAny<Embed[]>(),
                It.IsAny<MessageFlags>(),
                It.IsAny<PollProperties>()))
            .Callback((string _, bool _, Embed embed, RequestOptions _, AllowedMentions _,
                MessageReference _, MessageComponent _, ISticker[] _, Embed[] _,
                MessageFlags _, PollProperties _) =>
            {
                context.EmbedCallback(embed);
            })
            .Returns(Task.FromResult(new Mock<IUserMessage>().Object));

        context.TextChannelMock = channelMock.As<ITextChannel>();

        return channelMock;
    }

    public static async IAsyncEnumerable<Team> GetTeams(PageTarget pageTarget)
    {
        var config = await GetDynamicConfiguration().GetConfig();
        var teamsConfig = config.Teams.ToDictionary(n => n.InternalID, n => n);

        teamsConfig.Should().NotBeNull();

        var teamRefs = pageTarget.GetAttributesOfType<TeamRefAttribute>()?.Select(n => n.InternalID) ??
                       [];

        foreach (var internalId in teamRefs)
        {
            if (!teamsConfig.TryGetValue(internalId, out var value))
                throw new KeyNotFoundException("Failed to find team with internal ID " + internalId);

            yield return value;
        }
    }

    public static void SetupLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .WriteTo.Console()
            .CreateLogger();
        Log.Warning("Logging is enabled for this unit test.");
    }
}