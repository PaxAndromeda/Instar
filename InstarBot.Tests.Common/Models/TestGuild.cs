using Discord;
using PaxAndromeda.Instar;

namespace InstarBot.Tests.Models;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class TestGuild : IInstarGuild
{
    public ulong Id { get; init; }
    public IEnumerable<ITextChannel> TextChannels { get; init; } = null!;

    public IEnumerable<IRole> Roles { get; init; } = null!;

    public IEnumerable<IGuildUser> Users { get; init; } = null!;

    public virtual ITextChannel GetTextChannel(ulong channelId)
    {
        return TextChannels.First(n => n.Id.Equals(channelId));
    }

    public virtual IRole GetRole(Snowflake roleId)
    {
        return Roles.First(n => n.Id.Equals(roleId));
    }
}