using Discord;
using InstarBot.Tests;
using Moq;
using PaxAndromeda.Instar;
using System.Diagnostics.CodeAnalysis;
using MessageProperties = Discord.MessageProperties;

#pragma warning disable CS1998
#pragma warning disable CS8625

namespace InstarBot.Test.Framework.Models;

[SuppressMessage("ReSharper", "ReplaceAutoPropertyWithComputedProperty")]
public sealed class TestChannel (Snowflake id) : IMockOf<ITextChannel>, ITextChannel
{
    public ulong Id { get; } = id;
    public DateTimeOffset CreatedAt { get; } = id.Time;

    public Mock<ITextChannel> Mock { get; } = new();

	private readonly Dictionary<Snowflake, TestMessage> _messages = new();

	public IEnumerable<TestMessage> Messages => _messages.Values;

	public void VerifyMessage(string format, bool partial = false)
	{
		Mock.Verify(c => c.SendMessageAsync(
				It.Is<string>(s => TestUtilities.MatchesFormat(s, format, partial)),
				false,
				It.IsAny<Embed>(),
				It.IsAny<RequestOptions>(),
				It.IsAny<AllowedMentions>(),
				It.IsAny<MessageReference>(),
				It.IsAny<MessageComponent>(),
				It.IsAny<ISticker[]>(),
				It.IsAny<Embed[]>(),
				It.IsAny<MessageFlags>(),
				It.IsAny<PollProperties>()
			));
	}

	public void VerifyEmbed(EmbedVerifier verifier, string format, bool partial = false)
	{
		Mock.Verify(c => c.SendMessageAsync(
				It.Is<string>(n => TestUtilities.MatchesFormat(n, format, partial)),
				false,
				It.Is<Embed>(e => verifier.Verify(e)),
				It.IsAny<RequestOptions>(),
				It.IsAny<AllowedMentions>(),
				It.IsAny<MessageReference>(),
				It.IsAny<MessageComponent>(),
				It.IsAny<ISticker[]>(),
				It.IsAny<Embed[]>(),
				It.IsAny<MessageFlags>(),
				It.IsAny<PollProperties>()
			));
	}


	public Task<IMessage> GetMessageAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
	{
		return Mock.Object.GetMessageAsync(id, mode, options);
	}

	public async IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(int limit = 100,
        CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
    {
        yield return _messages.Values.ToList().AsReadOnly();
    }

    public async IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(ulong fromMessageId, Direction dir,
        int limit = 100, CacheMode mode = CacheMode.AllowDownload,
        RequestOptions options = null)
	{
		var snowflake = new Snowflake(fromMessageId);

		Func<TestMessage, bool> query = dir switch
		{
			Direction.Before => n => n.Id != fromMessageId && n.Timestamp.UtcDateTime > snowflake.Time.ToUniversalTime(),
			Direction.After => n => n.Id != fromMessageId && n.Timestamp.UtcDateTime < snowflake.Time.ToUniversalTime(),
			_ => throw new NotImplementedException()
		};

		var rz = _messages.Values.Where(query).ToList().AsReadOnly();

		yield return rz;
	}

	public IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(IMessage fromMessage, Direction dir,
		int limit = 100, CacheMode mode = CacheMode.AllowDownload,
		RequestOptions options = null)
		=> GetMessagesAsync(fromMessage.Id, dir, limit, mode, options);

	public Task<IReadOnlyCollection<IMessage>> GetPinnedMessagesAsync(RequestOptions options = null)
	{
		return Mock.Object.GetPinnedMessagesAsync(options);
	}

	public Task DeleteMessageAsync(ulong messageId, RequestOptions options = null)
	{
		return Mock.Object.DeleteMessageAsync(messageId, options);
	}

	public Task DeleteMessageAsync(IMessage message, RequestOptions options = null)
	{
		return Mock.Object.DeleteMessageAsync(message, options);
	}

	public Task<IUserMessage> ModifyMessageAsync(ulong messageId, Action<MessageProperties> func, RequestOptions options = null)
	{
		return Mock.Object.ModifyMessageAsync(messageId, func, options);
	}

	public Task TriggerTypingAsync(RequestOptions options = null)
	{
		return Mock.Object.TriggerTypingAsync(options);
	}

	public IDisposable EnterTypingState(RequestOptions options = null)
	{
		return Mock.Object.EnterTypingState(options);
	}

	public string Mention { get; } = null!;
	public Task<ICategoryChannel> GetCategoryAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
	{
		return Mock.Object.GetCategoryAsync(mode, options);
	}

	public Task SyncPermissionsAsync(RequestOptions options = null)
	{
		return Mock.Object.SyncPermissionsAsync(options);
	}

	public Task<IInviteMetadata> CreateInviteAsync(int? maxAge, int? maxUses = null, bool isTemporary = false, bool isUnique = false,
		RequestOptions options = null)
	{
		return Mock.Object.CreateInviteAsync(maxAge, maxUses, isTemporary, isUnique, options);
	}

	public Task<IInviteMetadata> CreateInviteToApplicationAsync(ulong applicationId, int? maxAge, int? maxUses = null, bool isTemporary = false,
		bool isUnique = false, RequestOptions options = null)
	{
		return Mock.Object.CreateInviteToApplicationAsync(applicationId, maxAge, maxUses, isTemporary, isUnique, options);
	}

	public Task<IInviteMetadata> CreateInviteToApplicationAsync(DefaultApplications application, int? maxAge, int? maxUses = null,
		bool isTemporary = false, bool isUnique = false, RequestOptions options = null)
	{
		return Mock.Object.CreateInviteToApplicationAsync(application, maxAge, maxUses, isTemporary, isUnique, options);
	}

	public Task<IInviteMetadata> CreateInviteToStreamAsync(IUser user, int? maxAge, int? maxUses = null, bool isTemporary = false,
		bool isUnique = false, RequestOptions options = null)
	{
		return Mock.Object.CreateInviteToStreamAsync(user, maxAge, maxUses, isTemporary, isUnique, options);
	}

	public Task<IReadOnlyCollection<IInviteMetadata>> GetInvitesAsync(RequestOptions options = null)
	{
		return Mock.Object.GetInvitesAsync(options);
	}

	public ulong? CategoryId { get; } = null;
	public Task DeleteMessagesAsync(IEnumerable<IMessage> messages, RequestOptions options = null)
	{
		return Mock.Object.DeleteMessagesAsync(messages, options);
	}

	public Task DeleteMessagesAsync(IEnumerable<ulong> messageIds, RequestOptions options = null)
	{
		return Mock.Object.DeleteMessagesAsync(messageIds, options);
	}

	public Task ModifyAsync(Action<TextChannelProperties> func, RequestOptions options = null)
	{
		return Mock.Object.ModifyAsync(func, options);
	}

	public Task<IThreadChannel> CreateThreadAsync(string name, ThreadType type = ThreadType.PublicThread, ThreadArchiveDuration autoArchiveDuration = ThreadArchiveDuration.OneDay,
		IMessage message = null, bool? invitable = null, int? slowmode = null, RequestOptions options = null)
	{
		return Mock.Object.CreateThreadAsync(name, type, autoArchiveDuration, message, invitable, slowmode, options);
	}

	public Task<IReadOnlyCollection<IThreadChannel>> GetActiveThreadsAsync(RequestOptions options = null)
	{
		return Mock.Object.GetActiveThreadsAsync(options);
	}

	public bool IsNsfw { get; } = false;
    public string Topic { get; } = null!;
    public int SlowModeInterval { get; } = 0;
    public int DefaultSlowModeInterval => Mock.Object.DefaultSlowModeInterval;

    public ThreadArchiveDuration DefaultArchiveDuration { get; } = default!;

    public TestMessage AddMessage(IGuildUser user, string messageContent)
    {
		var message = new TestMessage(user, messageContent)
		{
			Channel = this,
		};

        _messages.Add(message.Id, message);

		return message;
    }

    public Task<IUserMessage> SendMessageAsync(string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent components = null, ISticker[] stickers = null, Embed[] embeds = null, MessageFlags flags = MessageFlags.None, PollProperties poll = null)
    {
		Mock.Object.SendMessageAsync(text, isTTS, embed, options, allowedMentions, messageReference, components, stickers, embeds, flags, poll);

		var msg = new TestMessage(text, isTTS, embed, options, allowedMentions, messageReference, components, stickers, embeds, flags, poll);
		_messages.Add(msg.Id, msg);

		return Task.FromResult<IUserMessage>(msg);
    }

    public Task<IUserMessage> SendFileAsync(string filePath, string text = null, bool isTTS = false, Embed embed = null,
	    RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null,
	    MessageReference messageReference = null, MessageComponent components = null, ISticker[] stickers = null,
	    Embed[] embeds = null, MessageFlags flags = MessageFlags.None, PollProperties poll = null)
    {
	    return Mock.Object.SendFileAsync(filePath, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference, components, stickers, embeds, flags, poll);
    }

    public Task<IUserMessage> SendFileAsync(Stream stream, string filename, string text = null, bool isTTS = false, Embed embed = null,
	    RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null,
	    MessageReference messageReference = null, MessageComponent components = null, ISticker[] stickers = null,
	    Embed[] embeds = null, MessageFlags flags = MessageFlags.None, PollProperties poll = null)
    {
	    return Mock.Object.SendFileAsync(stream, filename, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference, components, stickers, embeds, flags, poll);
    }

    public Task<IUserMessage> SendFileAsync(FileAttachment attachment, string text = null, bool isTTS = false, Embed embed = null,
	    RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null,
	    MessageComponent components = null, ISticker[] stickers = null, Embed[] embeds = null, MessageFlags flags = MessageFlags.None,
	    PollProperties poll = null)
    {
	    return Mock.Object.SendFileAsync(attachment, text, isTTS, embed, options, allowedMentions, messageReference, components, stickers, embeds, flags, poll);
    }

    public Task<IUserMessage> SendFilesAsync(IEnumerable<FileAttachment> attachments, string text = null, bool isTTS = false, Embed embed = null,
	    RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null,
	    MessageComponent components = null, ISticker[] stickers = null, Embed[] embeds = null, MessageFlags flags = MessageFlags.None,
	    PollProperties poll = null)
    {
	    return Mock.Object.SendFilesAsync(attachments, text, isTTS, embed, options, allowedMentions, messageReference, components, stickers, embeds, flags, poll);
    }

    public Task ModifyAsync(Action<GuildChannelProperties> func, RequestOptions options = null)
    {
	    // ReSharper disable once SuspiciousTypeConversion.Global
	    return ((IGuildChannel) Mock).ModifyAsync(func, options);
    }

    public OverwritePermissions? GetPermissionOverwrite(IRole role)
    {
	    return Mock.Object.GetPermissionOverwrite(role);
    }

    public OverwritePermissions? GetPermissionOverwrite(IUser user)
    {
	    return Mock.Object.GetPermissionOverwrite(user);
    }

    public Task RemovePermissionOverwriteAsync(IRole role, RequestOptions options = null)
    {
	    return Mock.Object.RemovePermissionOverwriteAsync(role, options);
    }

    public Task RemovePermissionOverwriteAsync(IUser user, RequestOptions options = null)
    {
	    return Mock.Object.RemovePermissionOverwriteAsync(user, options);
    }

    public Task AddPermissionOverwriteAsync(IRole role, OverwritePermissions permissions, RequestOptions options = null)
    {
	    return Mock.Object.AddPermissionOverwriteAsync(role, permissions, options);
    }

    public Task AddPermissionOverwriteAsync(IUser user, OverwritePermissions permissions, RequestOptions options = null)
    {
	    return Mock.Object.AddPermissionOverwriteAsync(user, permissions, options);
    }

    public IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> GetUsersAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
    {
	    return Mock.Object.GetUsersAsync(mode, options);
    }

    Task<IGuildUser> IGuildChannel.GetUserAsync(ulong id, CacheMode mode  , RequestOptions options  )
    {
	    return Mock.Object.GetUserAsync(id, mode, options);
    }

    public int Position => Mock.Object.Position;

    public ChannelFlags Flags => Mock.Object.Flags;

    public IGuild Guild => Mock.Object.Guild;

    public ulong GuildId => Mock.Object.GuildId;

    public IReadOnlyCollection<Overwrite> PermissionOverwrites => Mock.Object.PermissionOverwrites;

    IAsyncEnumerable<IReadOnlyCollection<IUser>> IChannel.GetUsersAsync(CacheMode mode  , RequestOptions options  )
    {
	    // ReSharper disable once SuspiciousTypeConversion.Global
	    return ((IChannel)Mock).GetUsersAsync(mode, options);
    }

    Task<IUser> IChannel.GetUserAsync(ulong id, CacheMode mode  , RequestOptions options  )
    {
	    // ReSharper disable once SuspiciousTypeConversion.Global
	    return ((IChannel)Mock).GetUserAsync(id, mode, options);
    }

    public ChannelType ChannelType => Mock.Object.ChannelType;

    public string Name => Mock.Name;

    public Task DeleteAsync(RequestOptions options = null)
    {
	    return Mock.Object.DeleteAsync(options);
    }

    public Task<IWebhook> CreateWebhookAsync(string name, Stream avatar = null, RequestOptions options = null)
    {
	    return Mock.Object.CreateWebhookAsync(name, avatar, options);
    }

    public Task<IWebhook> GetWebhookAsync(ulong id, RequestOptions options = null)
    {
	    return Mock.Object.GetWebhookAsync(id, options);
    }

    public Task<IReadOnlyCollection<IWebhook>> GetWebhooksAsync(RequestOptions options = null)
    {
	    return Mock.Object.GetWebhooksAsync(options);
    }
}