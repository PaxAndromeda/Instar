using Discord;

namespace PaxAndromeda.Instar.Embeds;

public abstract class InstarAutoMemberSystemEmbed (string title) : InstarEmbed
{
	public string Title { get; } = title;

	// There are a couple of variants of this embed, such as the CheckEligibility embed,
	// staff eligibility check embed, and the auto-member hold notification embed.

	public override Embed Build()
	{
		var builder = new EmbedBuilder()
			.WithCurrentTimestamp()
			.WithTitle(Title)
			.WithFooter(new EmbedFooterBuilder()
				.WithText(Strings.Embed_AMS_Footer)
				.WithIconUrl(InstarLogoUrl));

		// Let subclasses build out the rest of the embed.
		builder = BuildParts(builder);

		return builder.Build();
	}

	protected abstract EmbedBuilder BuildParts(EmbedBuilder builder);
}