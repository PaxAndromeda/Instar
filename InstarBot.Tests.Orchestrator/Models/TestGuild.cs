using Discord;
using PaxAndromeda.Instar;

namespace InstarBot.Test.Framework.Models;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class TestGuild : IInstarGuild
{
    public ulong Id { get; init; }
	private readonly List<TestChannel> _channels = [];

	public IEnumerable<ITextChannel> TextChannels
	{
		get => _channels;
		init => _channels = value.OfType<TestChannel>().ToList();
	}


	public Dictionary<Snowflake, IRole> Roles { get; init; } = null!;

	public List<IGuildUser> Users { get; init; } = [];

    public virtual ITextChannel GetTextChannel(ulong channelId)
    {
        return TextChannels.First(n => n.Id.Equals(channelId));
    }

    public virtual IRole? GetRole(Snowflake roleId)
    {
	    return Roles.GetValueOrDefault(roleId);
    }

	public void AddUser(TestGuildUser user)
	{
		Users.Add(user);
	}

	public void AddChannel(TestChannel testChannel)
	{
		_channels.Add(testChannel);
	}
}