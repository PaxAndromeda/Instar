using System.Diagnostics.CodeAnalysis;
using Discord;
using InstarBot.Tests;
using Moq;
using PaxAndromeda.Instar;

#pragma warning disable CS8625

namespace InstarBot.Test.Framework.Models;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class TestGuildUser : TestUser, IMockOf<IGuildUser>, IGuildUser
{
	public Mock<IGuildUser> Mock { get; } = new();

    private HashSet<ulong> _roleIds = [ ];

	public TestGuildUser() : this(Snowflake.Generate(), [ ]) { }

	public TestGuildUser(Snowflake snowflake) : this(snowflake, [ ])
	{ }

	public TestGuildUser(Snowflake snowflake, IEnumerable<Snowflake> roles) : base(snowflake)
	{
		_roleIds = roles.Select(n => n.ID).ToHashSet();
	}

	public bool IsDeafened { get; set; }
    public bool IsMuted { get; set; }
    public bool IsSelfDeafened { get; set; }
    public bool IsSelfMuted { get; set; }
    public bool IsSuppressed { get; set; }
    public IVoiceChannel VoiceChannel { get; set; } = null!;
    public string VoiceSessionId { get; set; } = null!;
    public bool IsStreaming { get; set; }
    public bool IsVideoing { get; set; }
    public DateTimeOffset? RequestToSpeakTimestamp { get; set; }

	public ChannelPermissions GetPermissions(IGuildChannel channel)
	{
		return Mock.Object.GetPermissions(channel);
	}

	public string GetGuildAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
	{
		return Mock.Object.GetGuildAvatarUrl(format, size);
	}

	public string GetGuildBannerUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
	{
		return Mock.Object.GetGuildBannerUrl(format, size);
	}

	public Task KickAsync(string reason = null, RequestOptions options = null)
	{
		return Mock.Object.KickAsync(reason, options);
	}

	public Task ModifyAsync(Action<GuildUserProperties> func, RequestOptions options = null)
	{
		return Mock.Object.ModifyAsync(func, options);
	}

	public Task AddRoleAsync(ulong roleId, RequestOptions options = null)
    {
        Changed = true;
        _roleIds.Add(roleId);
        return Task.CompletedTask;
    }

    public Task AddRoleAsync(IRole role, RequestOptions options = null)
    {
        Changed = true;
        _roleIds.Add(role.Id);
        return Task.CompletedTask;
    }

    public Task AddRolesAsync(IEnumerable<ulong> roleIds, RequestOptions options = null)
    {
        Changed = true;
		foreach (var id in roleIds)
			_roleIds.Add(id);
		
        return Task.CompletedTask;
    }

    public Task AddRolesAsync(IEnumerable<IRole> roles, RequestOptions options = null)
    {
        Changed = true;
		foreach (var role in roles)
			_roleIds.Add(role.Id);

        return Task.CompletedTask;
    }

    public Task RemoveRoleAsync(ulong roleId, RequestOptions options = null)
    {
        Changed = true;
        _roleIds.Remove(roleId);
        return Task.CompletedTask;
    }

    public Task RemoveRoleAsync(IRole role, RequestOptions options = null)
    {
        Changed = true;
        _roleIds.Remove(role.Id);
        return Task.CompletedTask;
    }

    public Task RemoveRolesAsync(IEnumerable<ulong> roleIds, RequestOptions options = null)
    {
        Changed = true;
        foreach (var roleId in roleIds) _roleIds.Remove(roleId);
        return Task.CompletedTask;
    }

    public Task RemoveRolesAsync(IEnumerable<IRole> roles, RequestOptions options = null)
    {
        Changed = true;
        foreach (var roleId in roles.Select(n => n.Id)) _roleIds.Remove(roleId);

        return Task.CompletedTask;
    }

    public Task SetTimeOutAsync(TimeSpan span, RequestOptions options = null)
    {
	    return Mock.Object.SetTimeOutAsync(span, options);
    }

    public Task RemoveTimeOutAsync(RequestOptions options = null)
    {
	    return Mock.Object.RemoveTimeOutAsync(options);
    }

    public DateTimeOffset? JoinedAt { get; init; }
    public string DisplayName { get; set; } = null!;
    public string Nickname { get; set; } = null!;
    public string DisplayAvatarId { get; set; } = null!;
    public string GuildAvatarId { get; set; } = null!;
    public GuildPermissions GuildPermissions { get; set; }
    public IGuild Guild { get; set; } = null!;
	public ulong GuildId { get; internal set; } = 0;
    public DateTimeOffset? PremiumSince { get; set; }

    public IReadOnlyCollection<ulong> RoleIds
	{
		get => _roleIds.AsReadOnly();
		set => _roleIds = new HashSet<ulong>(value);
	}

    public bool? IsPending { get; set; }
    public int Hierarchy { get; set; }
    public DateTimeOffset? TimedOutUntil { get; set; }
    public GuildUserFlags Flags { get; set; }

    /// <summary>
    /// Test flag indicating the user has been changed.
    /// </summary>
    public bool Changed { get; private set; }

    public string GuildBannerHash { get; set; } = null!;

    public TestGuildUser Clone()
    {
		return (TestGuildUser) MemberwiseClone();
    }

    public void Reset()
    {
		Changed = false;
    }
}