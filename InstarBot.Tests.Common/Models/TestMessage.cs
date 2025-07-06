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
    public bool IsTTS => false;
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
    public MessageReference Reference => null!;
    public IReadOnlyDictionary<IEmote, ReactionMetadata> Reactions => null!;
    public IReadOnlyCollection<IMessageComponent> Components => null!;
    public IReadOnlyCollection<IStickerItem> Stickers => null!;
    public MessageFlags? Flags => null;
    public IMessageInteraction Interaction => null!;
    public MessageRoleSubscriptionData RoleSubscriptionData => null!;

    public PurchaseNotification PurchaseNotification => throw new NotImplementedException();

    public MessageCallData? CallData => throw new NotImplementedException();
}