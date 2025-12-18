using Discord;
using Moq;
using PaxAndromeda.Instar;
using MessageProperties = Discord.MessageProperties;

namespace InstarBot.Test.Framework.Models;

public sealed class TestMessage : IMockOf<IUserMessage>, IUserMessage, IMessage
{
	public Mock<IUserMessage> Mock { get; } = new();

	internal TestMessage(IUser user, string message)
	{
		Id = Snowflake.Generate();
		CreatedAt = DateTimeOffset.Now;
		Timestamp = DateTimeOffset.Now;
		Author = user;

		Content = message;
	}

	public TestMessage(string text, bool isTTS, Embed? embed, RequestOptions? options, AllowedMentions? allowedMentions, MessageReference? messageReference, MessageComponent? components, ISticker[]? stickers, Embed[]? embeds, MessageFlags? flags, PollProperties? poll)
	{
		Id = Snowflake.Generate();
		Content = text;
		IsTTS = isTTS;
		Flags = flags;

		var embedList = new List<Embed>();

		if (embed is not null)
			embedList.Add(embed);
		if (embeds is not null)
			embedList.AddRange(embeds);

		Flags = flags;
		Reference = messageReference;
	}

	public ulong Id { get; }
	public DateTimeOffset CreatedAt { get; }

	public Task AddReactionAsync(IEmote emote, RequestOptions? options = null)
	{
		return Mock.Object.AddReactionAsync(emote, options);
	}

	public Task RemoveReactionAsync(IEmote emote, IUser user, RequestOptions? options = null)
	{
		return Mock.Object.RemoveReactionAsync(emote, user, options);
	}

	public Task RemoveReactionAsync(IEmote emote, ulong userId, RequestOptions? options = null)
	{
		return Mock.Object.RemoveReactionAsync(emote, userId, options);
	}

	public Task RemoveAllReactionsAsync(RequestOptions? options = null)
	{
		return Mock.Object.RemoveAllReactionsAsync(options);
	}

	public Task RemoveAllReactionsForEmoteAsync(IEmote emote, RequestOptions? options = null)
	{
		return Mock.Object.RemoveAllReactionsForEmoteAsync(emote, options);
	}

	public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetReactionUsersAsync(IEmote emoji, int limit, RequestOptions? options = null,
		ReactionType type = ReactionType.Normal)
	{
		return Mock.Object.GetReactionUsersAsync(emoji, limit, options, type);
	}

	public MessageType Type => default;
	public MessageSource Source => default;
	public bool IsTTS { get; set; }

	public bool IsPinned => false;
	public bool IsSuppressed => false;
	public bool MentionedEveryone => false;
	public string Content { get; }
	public string CleanContent => null!;
	public DateTimeOffset Timestamp { get; }
	public DateTimeOffset? EditedTimestamp => null;
	public IMessageChannel Channel { get; set; } = null!;
	public IUser Author { get; }
	public IThreadChannel Thread => null!;
	public IReadOnlyCollection<IAttachment> Attachments => null!;
	public IReadOnlyCollection<IEmbed> Embeds => null!;
	public IReadOnlyCollection<ITag> Tags => null!;
	public IReadOnlyCollection<ulong> MentionedChannelIds => null!;
	public IReadOnlyCollection<ulong> MentionedRoleIds => null!;
	public IReadOnlyCollection<ulong> MentionedUserIds => null!;
	public MessageActivity Activity => null!;
	public MessageApplication Application => null!;
	public MessageReference Reference { get; set; }

	public IReadOnlyDictionary<IEmote, ReactionMetadata> Reactions => null!;
	public IReadOnlyCollection<IMessageComponent> Components => null!;
	public IReadOnlyCollection<IStickerItem> Stickers => null!;
	public MessageFlags? Flags { get; set; }

	public IMessageInteraction Interaction => null!;
	public MessageRoleSubscriptionData RoleSubscriptionData => null!;
	public PurchaseNotification PurchaseNotification => Mock.Object.PurchaseNotification;

	public MessageCallData? CallData => Mock.Object.CallData;

	public Task ModifyAsync(Action<MessageProperties> func, RequestOptions? options = null)
	{
		return Mock.Object.ModifyAsync(func, options);
	}

	public Task PinAsync(RequestOptions? options = null)
	{
		return Mock.Object.PinAsync(options);
	}

	public Task UnpinAsync(RequestOptions? options = null)
	{
		return Mock.Object.UnpinAsync(options);
	}

	public Task CrosspostAsync(RequestOptions? options = null)
	{
		return Mock.Object.CrosspostAsync(options);
	}

	public string Resolve(TagHandling userHandling = TagHandling.Name, TagHandling channelHandling = TagHandling.Name, TagHandling roleHandling = TagHandling.Name,
		TagHandling everyoneHandling = TagHandling.Ignore, TagHandling emojiHandling = TagHandling.Name)
	{
		return Mock.Object.Resolve(userHandling, channelHandling, roleHandling, everyoneHandling, emojiHandling);
	}

	public Task EndPollAsync(RequestOptions options)
	{
		return Mock.Object.EndPollAsync(options);
	}

	public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetPollAnswerVotersAsync(uint answerId, int? limit = null, ulong? afterId = null,
		RequestOptions? options = null)
	{
		return Mock.Object.GetPollAnswerVotersAsync(answerId, limit, afterId, options);
	}

	public MessageResolvedData ResolvedData { get; set; }
	public IUserMessage ReferencedMessage { get; set; }
	public IMessageInteractionMetadata InteractionMetadata { get; set; }
	public IReadOnlyCollection<MessageSnapshot> ForwardedMessages { get; set; }
	public Poll? Poll { get; set; }
	public Task DeleteAsync(RequestOptions? options = null)
	{
		return Mock.Object.DeleteAsync(options);
	}
}