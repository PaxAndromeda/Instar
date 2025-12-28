using Discord;
using InstarBot.Test.Framework.Models;
using JetBrains.Annotations;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Modals;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Test.Framework.Services;

public class TestDiscordService : IDiscordService
{
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

	private readonly TestGuild _guild;

	public TestDiscordService(TestDiscordContext context)
	{
		var users = context.Users.Select(IGuildUser (n) => n).ToList();
		var channels = context.Channels.Select(ITextChannel (n) => n);
		var roles = context.Roles.ToDictionary(n => new Snowflake(n.Id), IRole (n) => n);

		_guild = new TestGuild
		{
			Id = context.GuildId,
			Roles = roles,
			TextChannels = channels,
			Users = users
		};
	}

	public Task Start(IServiceProvider provider) => Task.CompletedTask;

	public IInstarGuild GetGuild()
	{
		return _guild;
	}

	public Task<IEnumerable<IGuildUser>> GetAllUsers()
	{
		return Task.FromResult(_guild.Users.AsEnumerable());
	}

	public Task<IChannel?> GetChannel(Snowflake channelId)
	{
		return Task.FromResult<IChannel?>(_guild.GetTextChannel(channelId));
	}

	public IAsyncEnumerable<IMessage> GetMessages(IInstarGuild guild, DateTime afterTime)
	{
		var result = from channel in guild.TextChannels.OfType<TestChannel>()
					 from message in channel.Messages
					 where message.Timestamp > afterTime
					 select message;

		return result.ToAsyncEnumerable();
	}

	public IGuildUser? GetUser(Snowflake snowflake)
	{
		return _guild.Users.FirstOrDefault(n => n.Id == snowflake.ID);
	}

	public IEnumerable<IGuildUser> GetAllUsersWithRole(Snowflake roleId)
	{
		return _guild.Users.Where(n => n.RoleIds.Contains(roleId));
	}

	public Task SyncUsers()
	{
		return Task.CompletedTask;
	}

	public Task Stop()
	{
		return Task.CompletedTask;
	}

	public void CreateChannel(Snowflake channelId)
	{
		_guild.AddChannel(new TestChannel(channelId));
	}

	public TestGuildUser CreateUser(Snowflake userId)
	{
		return CreateUser(new TestGuildUser(userId)
		{
			GuildId = GetGuild().Id
		});
	}

	public TestGuildUser CreateUser(TestGuildUser user)
	{
		user.GuildId = GetGuild().Id;
		_guild.AddUser(user);
		return user;
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
}