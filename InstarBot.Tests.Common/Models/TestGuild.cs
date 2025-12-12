using Discord;
using PaxAndromeda.Instar;

namespace InstarBot.Tests.Models;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class TestGuild : IInstarGuild
{
    public ulong Id { get; init; }
    public IEnumerable<ITextChannel> TextChannels { get; init; } = [ ];


	public IEnumerable<IRole> Roles { get; init; } = null!;

	public List<IGuildUser> Users { get; init; } = [];

    public virtual ITextChannel GetTextChannel(ulong channelId)
    {
        return TextChannels.First(n => n.Id.Equals(channelId));
    }

    public virtual IRole GetRole(Snowflake roleId)
    {
        return Roles.First(n => n.Id.Equals(roleId));
	}

	public void AddUser(TestGuildUser user)
	{
		Users.Add(user);
	}
}