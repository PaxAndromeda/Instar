using System.Diagnostics.CodeAnalysis;
using Discord;

namespace PaxAndromeda.Instar;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public interface IInstarGuild
{
    ulong Id { get; }
    IEnumerable<ITextChannel> TextChannels { get; }

    ITextChannel? GetTextChannel(ulong channelId);
    IRole? GetRole(Snowflake newMemberRole);
}