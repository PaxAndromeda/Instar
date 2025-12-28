using Discord;
using PaxAndromeda.Instar.Modals;

namespace PaxAndromeda.Instar.Services;

public interface IDiscordService
{
	/// <summary>
	/// Occurs when a user joins the guild.
	/// </summary>
	event Func<IGuildUser, Task> UserJoined;
	/// <summary>
	/// Occurs when a user leaves the guild.
	/// </summary>
	event Func<IUser, Task> UserLeft;
	/// <summary>
	/// Occurs when a user's details are updated, either by the user or otherwise.
	/// </summary>
	event Func<UserUpdatedEventArgs, Task> UserUpdated;
	event Func<IMessage, Task> MessageReceived;
    event Func<Snowflake, Task> MessageDeleted;
    
    Task Start(IServiceProvider provider);
    IInstarGuild GetGuild();
    Task<IEnumerable<IGuildUser>> GetAllUsers();
    Task<IChannel?> GetChannel(Snowflake channelId);
    IAsyncEnumerable<IMessage> GetMessages(IInstarGuild guild, DateTime afterTime);
	IGuildUser? GetUser(Snowflake snowflake);
	IEnumerable<IGuildUser> GetAllUsersWithRole(Snowflake roleId);
	Task SyncUsers();
	Task Stop();
}