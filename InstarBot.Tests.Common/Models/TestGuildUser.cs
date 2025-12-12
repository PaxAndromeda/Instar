using System.Diagnostics.CodeAnalysis;
using Discord;
using Moq;

#pragma warning disable CS8625

namespace InstarBot.Tests.Models;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public  class TestGuildUser : IGuildUser
{
    private readonly List<ulong> _roleIds = [ ];

    public ulong Id { get; init; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Mention { get; set; } = null!;
    public UserStatus Status { get; set; }
    public IReadOnlyCollection<ClientType> ActiveClients { get; set; } = null!;
    public IReadOnlyCollection<IActivity> Activities { get; set; } = null!;
    public string AvatarId { get; set; } = null!;
    public string Discriminator { get; set; } = null!;
    public ushort DiscriminatorValue { get; set; }
    public bool IsBot { get; set; }
    public bool IsWebhook { get; set; }
    public string Username { get; set; } = null!;
    public UserProperties? PublicFlags { get; set; }
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

	private readonly Mock<IDMChannel> _dmChannelMock = new();

	public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
    {
        return string.Empty;
    }

    public string GetDefaultAvatarUrl()
    {
        return string.Empty;
    }

    public Task<IDMChannel> CreateDMChannelAsync(RequestOptions options = null!)
    {
		return Task.FromResult(_dmChannelMock.Object);
    }

    public ChannelPermissions GetPermissions(IGuildChannel channel)
    {
        throw new NotImplementedException();
    }

    public string GetGuildAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
    {
        throw new NotImplementedException();
    }

    public string GetDisplayAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
    {
        throw new NotImplementedException();
    }

    public Task KickAsync(string reason = null, RequestOptions options = null!)
    {
        throw new NotImplementedException();
    }

    public Task ModifyAsync(Action<GuildUserProperties> func, RequestOptions options = null)
    {
        throw new NotImplementedException();
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
        _roleIds.AddRange(roleIds);
        return Task.CompletedTask;
    }

    public Task AddRolesAsync(IEnumerable<IRole> roles, RequestOptions options = null)
    {
        Changed = true;
        _roleIds.AddRange(roles.Select(role => role.Id));
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
        throw new NotImplementedException();
    }

    public Task RemoveTimeOutAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public string GetGuildBannerUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
    {
        throw new NotImplementedException();
    }

    public string GetAvatarDecorationUrl()
    {
        throw new NotImplementedException();
    }

    public DateTimeOffset? JoinedAt { get; init; }
    public string DisplayName { get; set; } = null!;
    public string Nickname { get; set; } = null!;
    public string DisplayAvatarId { get; set; } = null!;
    public string GuildAvatarId { get; set; } = null!;
    public GuildPermissions GuildPermissions { get; set; }
    public IGuild Guild { get; set; } = null!;
	public ulong GuildId => TestUtilities.GuildID;
    public DateTimeOffset? PremiumSince { get; set; }

    public IReadOnlyCollection<ulong> RoleIds
    {
        get => _roleIds.AsReadOnly();
        init => _roleIds = value.ToList();
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

	public string GlobalName { get; set; } = null!;

	public string AvatarDecorationHash { get; set; } = null!;

	public ulong? AvatarDecorationSkuId { get; set; } = null!;
	public PrimaryGuild? PrimaryGuild { get; set; } = null!;

	public TestGuildUser Clone()
	{
		return (TestGuildUser) MemberwiseClone();
	}
}