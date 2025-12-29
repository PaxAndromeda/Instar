using Discord;
using PaxAndromeda.Instar.ConfigModels;

namespace PaxAndromeda.Instar.Embeds;

public class InstarUnderageUserWarningEmbed(InstarDynamicConfiguration cfg, IGuildUser user, bool isMember, Birthday requestedBirthday) : InstarEmbed
{
	public override Embed Build()
	{
		int yearsOld = requestedBirthday.Age;
		long birthdayTimestamp = requestedBirthday.Timestamp;

		return new EmbedBuilder()
			// Set up all the basic stuff first
			.WithCurrentTimestamp()
			.WithColor(0x0c94e0)
			.WithAuthor(cfg.BotName, Strings.InstarLogoUrl)
			.WithFooter(new EmbedFooterBuilder()
				.WithText(Strings.Embed_BirthdaySystem_Footer)
				.WithIconUrl(Strings.InstarLogoUrl))
			.WithDescription(string.Format(
				isMember ? Strings.Embed_UnderageUser_WarningTemplate_Member : Strings.Embed_UnderageUser_WarningTemplate_NewMember,
				user.Id,
				birthdayTimestamp,
				yearsOld
				)).Build();
	}
}