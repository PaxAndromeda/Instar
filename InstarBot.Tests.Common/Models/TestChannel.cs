using System.Diagnostics.CodeAnalysis;
using Discord;
using PaxAndromeda.Instar;
using MessageProperties = Discord.MessageProperties;

#pragma warning disable CS1998
#pragma warning disable CS8625

namespace InstarBot.Tests.Models;

[SuppressMessage("ReSharper", "ReplaceAutoPropertyWithComputedProperty")]
public sealed class TestChannel(Snowflake id) : ITextChannel
{
    public ulong Id { get; } = id;
    public DateTimeOffset CreatedAt { get; } = id.Time;

    public Task ModifyAsync(Action<GuildChannelProperties> func, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public OverwritePermissions? GetPermissionOverwrite(IRole role)
    {
        return null;
    }

    public OverwritePermissions? GetPermissionOverwrite(IUser user)
    {
        return null;
    }

    public Task RemovePermissionOverwriteAsync(IRole role, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task RemovePermissionOverwriteAsync(IUser user, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task AddPermissionOverwriteAsync(IRole role, OverwritePermissions permissions, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task AddPermissionOverwriteAsync(IUser user, OverwritePermissions permissions, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> GetUsersAsync(CacheMode mode = CacheMode.AllowDownload,
        RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    Task<IGuildUser> IGuildChannel.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
    {
        throw new NotImplementedException();
    }

    public int Position { get; } = 0;
    public ChannelFlags Flags { get; } = default!;
    public IGuild Guild { get; } = null!;
    public ulong GuildId { get; } = 0;
    public IReadOnlyCollection<Overwrite> PermissionOverwrites { get; } = null!;

    IAsyncEnumerable<IReadOnlyCollection<IUser>> IChannel.GetUsersAsync(CacheMode mode, RequestOptions options)
    {
        return GetUsersAsync(mode, options);
    }

    Task<IUser> IChannel.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
    {
        throw new NotImplementedException();
    }

    public string Name { get; } = null!;

    public Task<IMessage> GetMessageAsync(ulong id, CacheMode mode = CacheMode.AllowDownload,
        RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(int limit = 100,
        CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
    {
        yield return _messages;
    }

    public async IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(ulong fromMessageId, Direction dir,
        int limit = 100, CacheMode mode = CacheMode.AllowDownload,
        RequestOptions options = null)
    {
        yield break;
    }

    public async IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(IMessage fromMessage, Direction dir,
        int limit = 100, CacheMode mode = CacheMode.AllowDownload,
        RequestOptions options = null)
    {
        yield break;
    }

    public Task<IReadOnlyCollection<IMessage>> GetPinnedMessagesAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task DeleteMessageAsync(ulong messageId, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task DeleteMessageAsync(IMessage message, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<IUserMessage> ModifyMessageAsync(ulong messageId, Action<MessageProperties> func,
        RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task TriggerTypingAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public IDisposable EnterTypingState(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public string Mention { get; } = null!;

    public Task DeleteAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<ICategoryChannel> GetCategoryAsync(CacheMode mode = CacheMode.AllowDownload,
        RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task SyncPermissionsAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<IInviteMetadata> CreateInviteAsync(int? maxAge, int? maxUses = null, bool isTemporary = false,
        bool isUnique = false,
        RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<IInviteMetadata> CreateInviteToApplicationAsync(ulong applicationId, int? maxAge, int? maxUses = null,
        bool isTemporary = false,
        bool isUnique = false, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<IInviteMetadata> CreateInviteToApplicationAsync(DefaultApplications application, int? maxAge,
        int? maxUses = null,
        bool isTemporary = false, bool isUnique = false, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<IInviteMetadata> CreateInviteToStreamAsync(IUser user, int? maxAge, int? maxUses = null,
        bool isTemporary = false,
        bool isUnique = false, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<IInviteMetadata>> GetInvitesAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public ulong? CategoryId { get; } = null;

    public Task<IWebhook> CreateWebhookAsync(string name, Stream avatar = null, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<IWebhook> GetWebhookAsync(ulong id, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<IWebhook>> GetWebhooksAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task DeleteMessagesAsync(IEnumerable<IMessage> messages, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task DeleteMessagesAsync(IEnumerable<ulong> messageIds, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task ModifyAsync(Action<TextChannelProperties> func, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<IThreadChannel> CreateThreadAsync(string name, ThreadType type = ThreadType.PublicThread,
        ThreadArchiveDuration autoArchiveDuration = ThreadArchiveDuration.OneDay,
        IMessage message = null, bool? invitable = null, int? slowmode = null, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<IThreadChannel>> GetActiveThreadsAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public bool IsNsfw { get; } = false;
    public string Topic { get; } = null!;
    public int SlowModeInterval { get; } = 0;
    public ThreadArchiveDuration DefaultArchiveDuration { get; } = default!;

    public int DefaultSlowModeInterval => throw new NotImplementedException();

    public ChannelType ChannelType => throw new NotImplementedException();

    private readonly List<TestMessage> _messages = [];

    public void AddMessage(IGuildUser user, string message)
    {
        _messages.Add(new TestMessage(user, message));
    }

    public Task<IUserMessage> SendMessageAsync(string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent components = null, ISticker[] stickers = null, Embed[] embeds = null, MessageFlags flags = MessageFlags.None, PollProperties poll = null)
    {
        throw new NotImplementedException();
    }

    public Task<IUserMessage> SendFileAsync(string filePath, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent components = null, ISticker[] stickers = null, Embed[] embeds = null, MessageFlags flags = MessageFlags.None, PollProperties poll = null)
    {
        throw new NotImplementedException();
    }

    public Task<IUserMessage> SendFileAsync(Stream stream, string filename, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent components = null, ISticker[] stickers = null, Embed[] embeds = null, MessageFlags flags = MessageFlags.None, PollProperties poll = null)
    {
        throw new NotImplementedException();
    }

    public Task<IUserMessage> SendFileAsync(FileAttachment attachment, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent components = null, ISticker[] stickers = null, Embed[] embeds = null, MessageFlags flags = MessageFlags.None, PollProperties poll = null)
    {
        throw new NotImplementedException();
    }

    public Task<IUserMessage> SendFilesAsync(IEnumerable<FileAttachment> attachments, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent components = null, ISticker[] stickers = null, Embed[] embeds = null, MessageFlags flags = MessageFlags.None, PollProperties poll = null)
    {
        throw new NotImplementedException();
    }
}