using Discord;
using PaxAndromeda.Instar;
using MessageProperties = Discord.MessageProperties;

namespace InstarBot.Tests.Models;

public sealed class TestMessage : IUserMessage, IMessage
{

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
		Content = text;
		IsTTS = isTTS;
		Flags= flags;

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

    public Task DeleteAsync(RequestOptions options = null!)
    {
        throw new NotImplementedException();
    }

    public Task AddReactionAsync(IEmote emote, RequestOptions options = null!)
    {
        throw new NotImplementedException();
    }

    public Task RemoveReactionAsync(IEmote emote, IUser user, RequestOptions options = null!)
    {
        throw new NotImplementedException();
    }

    public Task RemoveReactionAsync(IEmote emote, ulong userId, RequestOptions options = null!)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAllReactionsAsync(RequestOptions options = null!)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAllReactionsForEmoteAsync(IEmote emote, RequestOptions options = null!)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetReactionUsersAsync(IEmote emoji, int limit, RequestOptions? options = null, ReactionType type = ReactionType.Normal)
    {
        throw new NotImplementedException();
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
    public IMessageChannel Channel => null!;
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

    public PurchaseNotification PurchaseNotification => throw new NotImplementedException();

    public MessageCallData? CallData => throw new NotImplementedException();
    public Task ModifyAsync(Action<MessageProperties> func, RequestOptions options = null)
    {
	    throw new NotImplementedException();
    }

    public Task PinAsync(RequestOptions options = null)
    {
	    throw new NotImplementedException();
    }

    public Task UnpinAsync(RequestOptions options = null)
    {
	    throw new NotImplementedException();
    }

    public Task CrosspostAsync(RequestOptions options = null)
    {
	    throw new NotImplementedException();
    }

    public string Resolve(TagHandling userHandling = TagHandling.Name, TagHandling channelHandling = TagHandling.Name, TagHandling roleHandling = TagHandling.Name,
	    TagHandling everyoneHandling = TagHandling.Ignore, TagHandling emojiHandling = TagHandling.Name)
    {
	    throw new NotImplementedException();
    }

    public Task EndPollAsync(RequestOptions options)
    {
	    throw new NotImplementedException();
    }

    public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetPollAnswerVotersAsync(uint answerId, int? limit = null, ulong? afterId = null,
	    RequestOptions options = null)
    {
	    throw new NotImplementedException();
    }

    public MessageResolvedData ResolvedData { get; set; }
    public IUserMessage ReferencedMessage { get; set; }
    public IMessageInteractionMetadata InteractionMetadata { get; set; }
    public IReadOnlyCollection<MessageSnapshot> ForwardedMessages { get; set; }
    public Poll? Poll { get; set; }
}