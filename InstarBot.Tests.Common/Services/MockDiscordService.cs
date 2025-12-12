using Discord;
using InstarBot.Tests.Models;
using JetBrains.Annotations;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Modals;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Tests.Services;

public sealed class MockDiscordService : IDiscordService
{
    private IInstarGuild _guild;
	private readonly AsyncEvent<IGuildUser> _userJoinedEvent = new();
	private readonly AsyncEvent<IUser> _userLeftEvent = new();
	private readonly AsyncEvent<UserUpdatedEventArgs> _userUpdatedEvent = new();
	private readonly AsyncEvent<IMessage> _messageReceivedEvent = new();
	private readonly AsyncEvent<Snowflake> _messageDeletedEvent = new();

	public event Func<IGuildUser, Task> UserJoined
    {
        add => _userJoinedEvent.Add(value);
        remove => _userJoinedEvent.Remove(value);
	}

	public event Func<IUser, Task> UserLeft
	{
		add => _userLeftEvent.Add(value);
		remove => _userLeftEvent.Remove(value);
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
    
    internal MockDiscordService(IInstarGuild guild)
    {
        _guild = guild;
    }

    public IInstarGuild Guild
    {
	    get => _guild;
		set => _guild = value;
	}

    public Task Start(IServiceProvider provider)
    {
        return Task.CompletedTask;
    }

    public IInstarGuild GetGuild()
    {
        return _guild;
    }

    public Task<IEnumerable<IGuildUser>> GetAllUsers()
    {
        return Task.FromResult(((TestGuild)_guild).Users.AsEnumerable());
    }

    public Task<IChannel> GetChannel(Snowflake channelId)
    {
        return Task.FromResult<IChannel>(_guild.GetTextChannel(channelId));
    }

    public async IAsyncEnumerable<IMessage> GetMessages(IInstarGuild guild, DateTime afterTime)
    {
        foreach (var channel in guild.TextChannels)
        await foreach (var messageList in channel.GetMessagesAsync())
        foreach (var message in messageList)
            yield return message;
	}

    public IGuildUser? GetUser(Snowflake snowflake)
    {
		return ((TestGuild) _guild).Users.FirstOrDefault(n => n.Id.Equals(snowflake.ID));
    }

    public IEnumerable<IGuildUser> GetAllUsersWithRole(Snowflake roleId)
	{
		return ((TestGuild) _guild).Users.Where(n => n.RoleIds.Contains(roleId.ID));
	}

	public Task SyncUsers()
	{
		return Task.CompletedTask;
	}

	public async Task TriggerUserJoined(IGuildUser user)
	{
		await _userJoinedEvent.Invoke(user);
	}

	public async Task TriggerUserUpdated(UserUpdatedEventArgs args)
	{
		await _userUpdatedEvent.Invoke(args);
	}

	[UsedImplicitly]
    public async Task TriggerMessageReceived(IMessage message)
    {
        await _messageReceivedEvent.Invoke(message);
    }

	public void AddUser(TestGuildUser user)
	{
		var guild = _guild as TestGuild;
		guild?.AddUser(user);
	}
}