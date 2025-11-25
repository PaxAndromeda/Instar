using Discord;

namespace PaxAndromeda.Instar.Embeds;

public class InstarCheckEligibilityAMHEmbed() : InstarAutoMemberSystemEmbed(Strings.Command_CheckEligibility_EmbedTitle)
{
	protected override EmbedBuilder BuildParts(EmbedBuilder builder)
	{
		var fields = new List<EmbedFieldBuilder>
		{
			new EmbedFieldBuilder()
				.WithName("Why?")
				.WithValue(Strings.Command_CheckEligibility_AMH_Why),
			new EmbedFieldBuilder()
				.WithName("What can I do?")
				.WithValue(Strings.Command_CheckEligibility_AMH_WhatToDo),
			new EmbedFieldBuilder()
				.WithName("Should I contact Staff?")
				.WithValue(Strings.Command_CheckEligibility_AMH_ContactStaff)
		};

		return builder
			.WithDescription(Strings.Command_CheckEligibility_AMH_MembershipWithheld)
			.WithFields(fields);
	}
}