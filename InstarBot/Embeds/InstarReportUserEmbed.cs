using Discord;
using PaxAndromeda.Instar.Modals;
using System;

namespace PaxAndromeda.Instar.Embeds;

public class InstarReportUserEmbed(ReportMessageModal modal, IGuildUser contextUser, IMessage message, IInstarGuild guild) : InstarEmbed
{
	public override Embed Build()
	{
		var fields = new List<EmbedFieldBuilder>
		{
			new EmbedFieldBuilder()
				.WithIsInline(false)
				.WithName("Message Content")
				.WithValue($"```{message.Content}```"),
			new EmbedFieldBuilder()
				.WithIsInline(false)
				.WithName("Reason")
				.WithValue($"```{modal.ReportReason}```")
		};

		if (message.Author is not null)
			fields.Add(new EmbedFieldBuilder().WithIsInline(true).WithName("User")
				.WithValue($"<@{message.Author.Id}>"));

		if (message.Channel is not null)
			fields.Add(new EmbedFieldBuilder().WithIsInline(true).WithName("Channel")
				.WithValue($"<#{message.Channel.Id}>"));

		fields.Add(new EmbedFieldBuilder()
			.WithIsInline(true)
			.WithName("Message")
			.WithValue($"https://discord.com/channels/{guild.Id}/{message.Channel?.Id}/{message.Id}"));

		fields.Add(new EmbedFieldBuilder().WithIsInline(false).WithName("Reported By")
			.WithValue($"<@{contextUser.Id}>"));

		var builder = new EmbedBuilder()
            // Set up all the basic stuff first
            .WithCurrentTimestamp()
			.WithColor(0x0c94e0)
			.WithAuthor(message.Author?.Username, message.Author?.GetAvatarUrl())
			.WithFooter(new EmbedFooterBuilder()
				.WithText(Strings.Embed_UserReport_Footer)
				.WithIconUrl(Strings.InstarLogoUrl))
			.WithFields(fields);

		return builder.Build();
	}
}