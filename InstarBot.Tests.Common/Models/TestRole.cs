using System.Diagnostics.CodeAnalysis;
using Discord;
using PaxAndromeda.Instar;

#pragma warning disable CS8625

namespace InstarBot.Tests.Models;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public sealed class TestRole : IRole
{
    internal TestRole(Snowflake snowflake)
    {
        Id = snowflake;
        CreatedAt = snowflake.Time;
    }

    public ulong Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Task DeleteAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public string Mention { get; set; } = null!;

    public int CompareTo(IRole? other)
    {
        throw new NotImplementedException();
    }

    public Task ModifyAsync(Action<RoleProperties> func, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public string GetIconUrl()
    {
        throw new NotImplementedException();
    }

    public IGuild Guild { get; set; } = null!;
    public Color Color { get; set; } = default!;
    public bool IsHoisted { get; set; } = false;
    public bool IsManaged { get; set; } = false;
    public bool IsMentionable { get; set; } = false;
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public Emoji Emoji { get; set; } = null!;
    public GuildPermissions Permissions { get; set; } = default!;
    public int Position { get; set; } = 0;
    public RoleTags Tags { get; set; } = null!;

    public RoleFlags Flags { get; set; } = default!;
}