using Discord;
using PaxAndromeda.Instar;

namespace InstarBot.Tests.Models;

public sealed class TestMessage : IMessage
{

    internal TestMessage(IUser user, string message)
    {
        Id = Snowflake.Generate();
        CreatedAt = DateTimeOffset.Now;
        Timestamp = DateTimeOffset.Now;
        Author = user;

        Content = message;
    }

    public ulong Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

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

    public MessageType Type { get; set; } = default;
    public MessageSource Source { get; set; } = default;
    public bool IsTTS { get; set; } = false;
    public bool IsPinned { get; set; } = false;
    public bool IsSuppressed { get; set; } = false;
    public bool MentionedEveryone { get; set; } = false;
    public string Content { get; set; }
    public string CleanContent { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset? EditedTimestamp { get; set; } = null;
    public IMessageChannel Channel { get; set; } = null!;
    public IUser Author { get; set; }
    public IThreadChannel Thread { get; set; } = null!;
    public IReadOnlyCollection<IAttachment> Attachments { get; set; } = null!;
    public IReadOnlyCollection<IEmbed> Embeds { get; set; } = null!;
    public IReadOnlyCollection<ITag> Tags { get; set; } = null!;
    public IReadOnlyCollection<ulong> MentionedChannelIds { get; set; } = null!;
    public IReadOnlyCollection<ulong> MentionedRoleIds { get; set; } = null!;
    public IReadOnlyCollection<ulong> MentionedUserIds { get; set; } = null!;
    public MessageActivity Activity { get; set; } = null!;
    public MessageApplication Application { get; set; } = null!;
    public MessageReference Reference { get; set; } = null!;
    public IReadOnlyDictionary<IEmote, ReactionMetadata> Reactions { get; set; } = null!;
    public IReadOnlyCollection<IMessageComponent> Components { get; set; } = null!;
    public IReadOnlyCollection<IStickerItem> Stickers { get; set; } = null!;
    public MessageFlags? Flags { get; set; } = null;
    public IMessageInteraction Interaction { get; set; } = null!;
    public MessageRoleSubscriptionData RoleSubscriptionData { get; set; } = null!;

    public PurchaseNotification PurchaseNotification => throw new NotImplementedException();

    public MessageCallData? CallData => throw new NotImplementedException();
}