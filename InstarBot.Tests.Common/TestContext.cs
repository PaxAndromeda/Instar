using System.Diagnostics.CodeAnalysis;
using Discord;
using InstarBot.Tests.Models;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Gaius;

namespace InstarBot.Tests;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public sealed class TestContext
{
    public ulong UserID { get; init; } = 1420070400100;
    public ulong ChannelID { get; init; } = 1420070400200;
    public ulong GuildID { get; init; } = 1420070400300;

    public List<Snowflake> UserRoles { get; init; } = [];

    public Action<Embed> EmbedCallback { get; init; } = _ => { };

    public Mock<ITextChannel> TextChannelMock { get; internal set; } = null!;

    public List<IGuildUser> GuildUsers { get; init; } = [];

    public Dictionary<Snowflake, TestChannel> Channels { get; init; } = new();
    public Dictionary<Snowflake, TestRole> Roles { get; init; } = new();

    public Dictionary<Snowflake, List<Warning>> Warnings { get; init; } = new();
    public Dictionary<Snowflake, List<Caselog>> Caselogs { get; init; } = new();

    public bool InhibitGaius { get; set; }

    public void AddWarning(Snowflake userId, Warning warning)
    {
        if (!Warnings.ContainsKey(userId))
            Warnings.Add(userId, [warning]);
        else
            Warnings[userId].Add(warning);
    }

    public void AddCaselog(Snowflake userId, Caselog caselog)
    {
        if (!Caselogs.ContainsKey(userId))
            Caselogs.Add(userId, [caselog]);
        else
            Caselogs[userId].Add(caselog);
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