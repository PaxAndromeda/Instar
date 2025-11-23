using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.Modals;
using PaxAndromeda.Instar.Wrappers;
using Serilog;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;

namespace PaxAndromeda.Instar.Services;

[ExcludeFromCodeCoverage]
public sealed class DiscordService : IDiscordService
{
    private string _botToken = null!;

    private readonly Dictionary<string, IContextCommand> _contextCommands;
    private readonly ulong _guild;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _provider;
    private readonly IDynamicConfigService _dynamicConfig;
    private readonly DiscordSocketClient _socketClient;
	private readonly AsyncEvent<IGuildUser> _userJoinedEvent = new();
	private readonly AsyncEvent<UserUpdatedEventArgs> _userUpdatedEvent = new();
	private readonly AsyncEvent<IMessage> _messageReceivedEvent = new();
    private readonly AsyncEvent<Snowflake> _messageDeletedEvent = new();

	public event Func<IGuildUser, Task> UserJoined
	{
		add => _userJoinedEvent.Add(value);
		remove => _userJoinedEvent.Remove(value);
	}

	public event Func<UserUpdatedEventArgs, Task> UserUpdated
	{
		add => _userUpdatedEvent.Add(value);
		remove => _userUpdatedEvent.Remove(value);
	}

	public event Func<IMessage, Task> MessageReceived
    {
        add => _messageReceivedEvent.Add(value);
        remove => _messageReceivedEvent.Remove(value);
    }

    public event Func<Snowflake, Task> MessageDeleted
    {
        add => _messageDeletedEvent.Add(value);
        remove => _messageDeletedEvent.Remove(value);
    }

    public DiscordService(IServiceProvider provider, IConfiguration config, IDynamicConfigService dynamicConfig)
    {
        _provider = provider;
        _dynamicConfig = dynamicConfig;

        _socketClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            // All privileges except for GuildScheduledEvents and GuildInvites.
            // This is written to prevent warnings about these two privileges.
            GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.AllUnprivileged
                             & ~(GatewayIntents.GuildScheduledEvents | GatewayIntents.GuildInvites),
            LogLevel = LogSeverity.Debug
        });

        _interactionService = new InteractionService(_socketClient, new InteractionServiceConfig
        {
            LogLevel = LogSeverity.Debug,
            ThrowOnError = true
        });

        _socketClient.Log += HandleDiscordLog;
        _socketClient.InteractionCreated += HandleInteraction;
        _socketClient.MessageCommandExecuted += HandleMessageCommand;
        _socketClient.MessageReceived += async message => await _messageReceivedEvent.Invoke(message);
        _socketClient.MessageDeleted += async (msgCache, _) => await _messageDeletedEvent.Invoke(msgCache.Id);
        _socketClient.UserJoined += async user => await _userJoinedEvent.Invoke(user);
		_socketClient.GuildMemberUpdated += HandleUserUpdate;
        _interactionService.Log += HandleDiscordLog;

        _contextCommands = provider.GetServices<IContextCommand>().ToDictionary(n => n.Name, n => n);

        _guild = config.GetValue("TargetGuild", 0ul);

        // Validate
        if (_guild == 0)
            throw new ConfigurationException("TargetGuild is not set");
    }

    private async Task HandleUserUpdate(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
	{
		// Can't do anything if we don't have a before state.
		// In the case of a user join, that is handled in UserJoined event.
		if (!before.HasValue)
			return;

		await _userUpdatedEvent.Invoke(new UserUpdatedEventArgs(after.Id, before.Value, after));
	}

    private async Task HandleMessageCommand(SocketMessageCommand arg)
    {
        Log.Information("Message command: {CommandName}", arg.CommandName);

        if (!_contextCommands.TryGetValue(arg.CommandName, out var command))
        {
            Log.Warning("Received message command interaction for unknown command by name {CommandName}",
                arg.CommandName);
            return;
        }

        await command.HandleCommand(new MessageCommandInteractionWrapper(arg));
    }

    private async Task HandleInteraction(SocketInteraction arg)
    {
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
            var ctx = new SocketInteractionContext(_socketClient, arg);

            Log.Verbose(
                "Handling interaction of type {InteractionType} with ID {InteractionID} from user {UserName} ({UserID})",
                ctx.Interaction.Type, ctx.Interaction.Id, ctx.User.Username, ctx.User.Id);

            await _interactionService.ExecuteCommandAsync(ctx, _provider);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to handle interaction");

            // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
            // response, or at least let the user know that something went wrong during the command execution.
            if (arg.Type == InteractionType.ApplicationCommand)
                await arg.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
        }
    }

    public async Task Start(IServiceProvider provider)
    {
        Log.Information("Attempting to connect to Discord...");

        _botToken = await _dynamicConfig.GetParameter("DiscordKey") ?? throw new ConfigurationException("Failed to obtain Discord key from AWS");
        if (string.IsNullOrEmpty(_botToken))
            throw new ConfigurationException("Token is not set");

        _socketClient.Ready += async () =>
        {
            Log.Information("Discord client is ready");
            await _interactionService.AddModulesAsync(typeof(DiscordService).Assembly, provider);

            Log.Information("Registering commands...");
            var result = await _interactionService.RegisterCommandsToGuildAsync(_guild);

            foreach (var globalCmd in result)
                Log.Information(
                    "Registered command {CommandName} with ID {CommandID} and default permission {DefaultPerm}",
                    globalCmd.Name, globalCmd.Id, globalCmd.DefaultMemberPermissions);

            var props = _contextCommands.Values.Select(n => n.CreateCommand())
                .Cast<ApplicationCommandProperties>().ToArray();

            await _socketClient.BulkOverwriteGlobalApplicationCommandsAsync(props);
        };

        Log.Verbose("Attempting login...");
        await _socketClient.LoginAsync(TokenType.Bot, _botToken);
        Log.Verbose("Starting Discord...");
        await _socketClient.StartAsync();
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    private static Task HandleDiscordLog(LogMessage arg)
    {
        var severity = arg.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };

        Log.Write(severity, arg.Exception, arg.Message);
        return Task.CompletedTask;
    }

    public async Task Stop()
    {
        Log.Information("Stopping!");
        await _socketClient.StopAsync();
        await _socketClient.LogoutAsync();
    }

    public IInstarGuild GetGuild()
    {
        return new SocketGuildWrapper(_socketClient.GetGuild(_guild));
    }

    public async Task<IEnumerable<IGuildUser>> GetAllUsers()
    {
        try
        {
            var guild = _socketClient.GetGuild(_guild);
            await _socketClient.DownloadUsersAsync([guild]);

            return guild.Users;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download users for guild {GuildID}", _guild);
            return [];
        }
    }
    
    public async IAsyncEnumerable<IMessage> GetMessages(IInstarGuild guild, DateTime afterTime)
    {
        Log.Debug("GetMessages({Guild}, {AfterTime})", guild.Id, afterTime);
        
        foreach (var channel in guild.TextChannels)
        {
            Log.Debug("Downloading #{Channel}", channel.Name);
            // Reference message will be the "current"
            // message we are looking at.  Since the
            // GetMessagesAsync() method returns messages
            // in order of newest to oldest, we can keep
            // a running log of the oldest message we've
            // encountered.
            var refMessage = (await channel.GetMessagesAsync(1).FlattenAsync()).FirstOrDefault();
            if (refMessage is null)
                continue;
            if (refMessage.Timestamp < afterTime)
                continue;
            
            yield return refMessage;

            var totalMessages = 1;
            var done = false;

            do
            {
                await foreach (var messageList in channel.GetMessagesAsync(refMessage, Direction.Before))
                {
                    if (messageList.Count == 0)
                    {
                        done = true;
                        break;
                    }

                    foreach (var message in messageList)
                    {
                        totalMessages++;
                        refMessage = message;

                        if (message.Timestamp < afterTime)
                        {
                            done = true;
                            break;
                        }

                        yield return refMessage;
                    }

                    if (done)
                        break;
                }
            } while (!done);

            Log.Verbose("Cached {TotalMessages} messages from #{ChannelName}", totalMessages, channel.Name);
        }
    }

    public async Task<IChannel> GetChannel(Snowflake channelId)
        => await _socketClient.GetChannelAsync(channelId);
}