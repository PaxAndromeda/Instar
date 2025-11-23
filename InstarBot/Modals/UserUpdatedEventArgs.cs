using Discord;

namespace PaxAndromeda.Instar.Modals;

public class UserUpdatedEventArgs(Snowflake id, IGuildUser before, IGuildUser after)
{
	public Snowflake ID { get; } = id;

	public IGuildUser Before { get; } = before;

	public IGuildUser After { get; } = after;

	// TODO: add additional parts to this for the data we care about

	/// <summary>
	/// A flag indicating whether data we care about (e.g. nicknames, usernames) has changed.
	/// </summary>
	public bool HasUpdated => Before.Username != After.Username || Before.Nickname != After.Nickname;
}