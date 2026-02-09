using Discord;

namespace PaxAndromeda.Instar.Modals;

public class UserUpdatedEventArgs(Snowflake id, IUser before, IUser after)
{
	public Snowflake ID { get; } = id;

	public IUser Before { get; } = before;

	public IUser After { get; } = after;

	// TODO: add additional parts to this for the data we care about

	/// <summary>
	/// A flag indicating whether data we care about (e.g. nicknames, usernames) has changed.
	/// </summary>
	public bool HasUpdated => Before.Username != After.Username || (Before is IGuildUser gBefore && After is IGuildUser gAfter && gBefore.Nickname != gAfter.Nickname);
}