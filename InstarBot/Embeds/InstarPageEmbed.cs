using Discord;
using PaxAndromeda.Instar.ConfigModels;
using System.Threading.Channels;

namespace PaxAndromeda.Instar.Embeds;

public class InstarPageEmbed (
	string reason,
	string message,
	IUser? targetUser,
	IChannel? targetChannel,
	Team pagingTeam,
	IGuildUser pagingUser)
	: InstarEmbed
{
	public override Embed Build()
	{
		var fields = new List<EmbedFieldBuilder>();

		if (targetUser is not null)
			fields.Add(new EmbedFieldBuilder().WithIsInline(true).WithName("User").WithValue($"<@{targetUser.Id}>"));

		if (targetChannel is not null)
			fields.Add(new EmbedFieldBuilder().WithIsInline(true).WithName("Channel").WithValue($"<#{targetChannel.Id}>"));

		if (!string.IsNullOrEmpty(message))
			fields.Add(new EmbedFieldBuilder().WithIsInline(true).WithName("Message").WithValue(message));

		var builder = new EmbedBuilder()
            // Set up all the basic stuff first
            .WithCurrentTimestamp()
			.WithColor(pagingTeam.Color)
			.WithAuthor(pagingUser.Nickname ?? pagingUser.Username, pagingUser.GetAvatarUrl())
			.WithFooter(new EmbedFooterBuilder()
				.WithText(Strings.Embed_Page_Footer)
				.WithIconUrl(Strings.InstarLogoUrl))
            // Description
            .WithDescription($"```{reason}```")
			.WithFields(fields);

		var embed = builder.Build();
		return embed;
	}
}