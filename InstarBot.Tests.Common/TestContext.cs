using Discord;
using InstarBot.Tests.Models;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Gaius;

namespace InstarBot.Tests;

public sealed class TestContext
{
    public ulong UserID { get; init; }= 1420070400100;
    public const ulong ChannelID = 1420070400200;
    public const ulong GuildID = 1420070400300;

    public List<Snowflake> UserRoles { get; init; } = [];

    public Action<Embed> EmbedCallback { get; init; } = _ => { };

    public Mock<ITextChannel> TextChannelMock { get; internal set; } = null!;

    public List<IGuildUser> GuildUsers { get; } = [];

    public Dictionary<Snowflake, TestChannel> Channels { get; } = [];
    public Dictionary<Snowflake, TestRole> Roles { get; } = [];

    public Dictionary<Snowflake, List<Warning>> Warnings { get; } = [];
    public Dictionary<Snowflake, List<Caselog>> Caselogs { get; } = [];

    public bool InhibitGaius { get; set; }

    public void AddWarning(Snowflake userId, Warning warning)
    {
        if (!Warnings.TryGetValue(userId, out var list))
            Warnings[userId] = list = [];
        list.Add(warning);
    }

    public void AddCaselog(Snowflake userId, Caselog caselog)
    {
        if (!Caselogs.TryGetValue(userId, out var list))
            Caselogs[userId] = list = [];
        list.Add(caselog);
    }
    
    public void AddChannel(Snowflake channelId)
    {
        if (Channels.ContainsKey(channelId))
            throw new InvalidOperationException("Channel already exists.");

        Channels.Add(channelId, new TestChannel(channelId));
    }

    public TestChannel GetChannel(Snowflake channelId)
    {
        return Channels[channelId];
    }

    public void AddRoles(IEnumerable<Snowflake> roles)
    {
        foreach (var snowflake in roles)
            if (!Roles.ContainsKey(snowflake))
                Roles.Add(snowflake, new TestRole(snowflake));
    }
}