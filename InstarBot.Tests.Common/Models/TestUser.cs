using Discord;
using System.Diagnostics.CodeAnalysis;
using PaxAndromeda.Instar;

namespace InstarBot.Tests.Models;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class TestUser : IUser
{
	public TestUser(Snowflake snowflake)
	{
		Id = snowflake;
		CreatedAt = snowflake.Time;
		Username = "username";
	}

	public TestUser(TestGuildUser guildUser)
	{
		Id = guildUser.Id;
		CreatedAt = guildUser.CreatedAt;
		Mention = guildUser.Mention;
		Status = guildUser.Status;
		ActiveClients = guildUser.ActiveClients;
		Activities = guildUser.Activities;
		AvatarId = guildUser.AvatarId;
		Discriminator = guildUser.Discriminator;
		DiscriminatorValue = guildUser.DiscriminatorValue;
		IsBot = guildUser.IsBot;
		IsWebhook = guildUser.IsWebhook;
		Username = guildUser.Username;
		PublicFlags = guildUser.PublicFlags;
		AvatarDecorationHash = guildUser.AvatarDecorationHash;
		AvatarDecorationSkuId = guildUser.AvatarDecorationSkuId;
		GlobalName = guildUser.GlobalName;
		PrimaryGuild = guildUser.PrimaryGuild;
	}

	public ulong Id { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public string Mention { get; set; }
	public UserStatus Status { get; set; }
	public IReadOnlyCollection<ClientType> ActiveClients { get; set; }
	public IReadOnlyCollection<IActivity> Activities { get; set; }

	public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
	{
		return string.Empty;
	}

	public string GetDefaultAvatarUrl()
	{
		return string.Empty;
	}

	public string GetDisplayAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
	{
		throw new NotImplementedException();
	}

	public Task<IDMChannel> CreateDMChannelAsync(RequestOptions? options = null)
	{
		throw new NotImplementedException();
	}

	public string GetAvatarDecorationUrl()
	{
		return string.Empty;
	}

	public string AvatarId { get; set; }
	public string Discriminator { get; set; }
	public ushort DiscriminatorValue { get; set; }
	public bool IsBot { get; set; }
	public bool IsWebhook { get; set; }
	public string Username { get; set; }
	public UserProperties? PublicFlags { get; set; }
	public string GlobalName { get; set; }
	public string AvatarDecorationHash { get; set; }
	public ulong? AvatarDecorationSkuId { get; set; }
	public PrimaryGuild? PrimaryGuild { get; set; }
}