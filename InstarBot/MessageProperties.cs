using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PaxAndromeda.Instar;

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public readonly struct MessageProperties(ulong userId, ulong channelId, ulong guildId)
{
    public readonly ulong UserID = userId;
    public readonly ulong ChannelID = channelId;
    public readonly ulong GuildID = guildId;
}