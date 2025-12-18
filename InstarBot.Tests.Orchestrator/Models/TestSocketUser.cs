using Discord;
using Discord.WebSocket;
using Moq;

namespace InstarBot.Test.Framework.Models;

public class TestSocketUser : IMockOf<SocketUser>
{
	public Mock<SocketUser> Mock { get; } = new();


	public bool IsBot
	{
		get => Mock.Object.IsBot;
		set => Mock.Setup(obj => obj.IsBot).Returns(value);
	}

	public string Username
	{
		get => Mock.Object.Username;
		set => Mock.Setup(obj => obj.Username).Returns(value);
	}

	public ushort DiscriminatorValue
	{
		get => Mock.Object.DiscriminatorValue;
		set => Mock.Setup(obj => obj.DiscriminatorValue).Returns(value);
	}

	public string AvatarId
	{
		get => Mock.Object.AvatarId;
		set => Mock.Setup(obj => obj.AvatarId).Returns(value);
	}

	public bool IsWebhook
	{
		get => Mock.Object.IsWebhook;
		set => Mock.Setup(obj => obj.IsWebhook).Returns(value);
	}

	public string GlobalName
	{
		get => Mock.Object.GlobalName;
		set => Mock.Setup(obj => obj.GlobalName).Returns(value);
	}

	public string AvatarDecorationHash
	{
		get => Mock.Object.AvatarDecorationHash;
		set => Mock.Setup(obj => obj.AvatarDecorationHash).Returns(value);
	}

	public ulong? AvatarDecorationSkuId
	{
		get => Mock.Object.AvatarDecorationSkuId;
		set => Mock.Setup(obj => obj.AvatarDecorationSkuId).Returns(value);
	}

	public PrimaryGuild? PrimaryGuild
	{
		get => Mock.Object.PrimaryGuild;
		set => Mock.Setup(obj => obj.PrimaryGuild).Returns(value);
	}

	public static TestSocketUser FromUser(IUser? user)
	{
		if (user is null)
			throw new ArgumentNullException(nameof(user));

		return new TestSocketUser
		{
			IsBot = user.IsBot,
			Username = user.Username,
			DiscriminatorValue = user.DiscriminatorValue,
			AvatarId = user.AvatarId,
			IsWebhook = user.IsWebhook,
			GlobalName = user.GlobalName,
			AvatarDecorationHash = user.AvatarDecorationHash,
			AvatarDecorationSkuId = user.AvatarDecorationSkuId,
			PrimaryGuild = user.PrimaryGuild
		};
	}
}