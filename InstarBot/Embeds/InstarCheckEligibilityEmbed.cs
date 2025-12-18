using System.Text;
using Discord;
using PaxAndromeda.Instar.ConfigModels;

namespace PaxAndromeda.Instar.Embeds;

public class InstarCheckEligibilityEmbed(IGuildUser user, MembershipEligibility eligibility, InstarDynamicConfiguration config)
	: InstarAutoMemberSystemEmbed(Strings.Command_CheckEligibility_EmbedTitle)
{
	protected override EmbedBuilder BuildParts(EmbedBuilder builder)
	{
		// We only have to focus on the description, author and fields here

		var fields = new List<EmbedFieldBuilder>();
		if (!eligibility.HasFlag(MembershipEligibility.Eligible))
		{
			fields.Add(new EmbedFieldBuilder()
				.WithName("Missing Items")
				.WithValue(BuildMissingItemsText()));
		}

		var nextRun = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month,
						  DateTimeOffset.UtcNow.Day, DateTimeOffset.UtcNow.Hour, 0, 0, TimeSpan.Zero)
					  + TimeSpan.FromHours(1);
		var unixTime = nextRun.UtcTicks / 10000000-62135596800; // UTC ticks since year 0 to Unix Timestamp

		fields.Add(new EmbedFieldBuilder()
			.WithName("Note")
			.WithValue(string.Format(Strings.Command_CheckEligibility_NextRuntimeNote, unixTime)));

		return builder
			.WithAuthor(new EmbedAuthorBuilder()
				.WithName(user.Username)
				.WithIconUrl(user.GetAvatarUrl()))
			.WithDescription(BuildEligibilityText())
			.WithFields(fields);
	}

	private string BuildEligibilityText()
	{
		var eligibilityBuilder = new StringBuilder();


		eligibilityBuilder.AppendLine(BuildEligibilityComponent(Strings.Command_CheckEligibility_RolesEligibility, !eligibility.HasFlag(MembershipEligibility.MissingRoles)));
		eligibilityBuilder.AppendLine(BuildEligibilityComponent(Strings.Command_CheckEligibility_IntroductionEligibility, !eligibility.HasFlag(MembershipEligibility.MissingIntroduction)));
		eligibilityBuilder.AppendLine(BuildEligibilityComponent(Strings.Command_CheckEligibility_JoinAgeEligibility, !eligibility.HasFlag(MembershipEligibility.InadequateTenure)));
		eligibilityBuilder.AppendLine(BuildEligibilityComponent(Strings.Command_CheckEligibility_ModActionsEligibility, !eligibility.HasFlag(MembershipEligibility.PunishmentReceived)));
		eligibilityBuilder.AppendLine(BuildEligibilityComponent(Strings.Command_CheckEligibility_MessagesEligibility, !eligibility.HasFlag(MembershipEligibility.NotEnoughMessages)));

		return eligibilityBuilder.ToString();
	}

	private static string BuildEligibilityComponent(string format, bool eligible)
	{
		return string.Format(
			format,
			eligible
				? Strings.Command_CheckEligibility_EligibleEmoji
				: Strings.Command_CheckEligibility_NotEligibleEmoji
			);
	}

	private string BuildMissingItemsText()
	{
		if (eligibility == MembershipEligibility.Eligible)
			return string.Empty;


		var missingItems = new List<string>();

		if (eligibility.HasFlag(MembershipEligibility.MissingRoles))
		{
			// What roles are we missing?
			missingItems.AddRange(
				from roleGroup in config.AutoMemberConfig.RequiredRoles
				where !user.RoleIds.Intersect(roleGroup.Roles.Select(n => n.ID)).Any()
				let article = "aeiouAEIOU".Contains(roleGroup.GroupName[0]) ? "an" : "a" // grammar hack
				select string.Format(Strings.Command_CheckEligibility_MissingItem_Role, article, roleGroup.GroupName.ToLowerInvariant()));
		}

		if (eligibility.HasFlag(MembershipEligibility.MissingIntroduction))
			missingItems.Add(string.Format(Strings.Command_CheckEligibility_MissingItem_Introduction, Snowflake.GetMention(() => config.AutoMemberConfig.IntroductionChannel)));
		
		if (eligibility.HasFlag(MembershipEligibility.InadequateTenure))
			missingItems.Add(string.Format(Strings.Command_CheckEligibility_MissingItem_TooYoung, config.AutoMemberConfig.MinimumJoinAge / 3600));

		if (eligibility.HasFlag(MembershipEligibility.PunishmentReceived))
			missingItems.Add(Strings.Command_CheckEligibility_MissingItem_PunishmentReceived);

		if (eligibility.HasFlag(MembershipEligibility.NotEnoughMessages))
			missingItems.Add(string.Format(Strings.Command_CheckEligibility_MissingItem_Messages, config.AutoMemberConfig.MinimumMessages, config.AutoMemberConfig.MinimumMessageTime / 3600));

		var missingItemsBuilder = new StringBuilder();
		foreach (var item in missingItems)
			missingItemsBuilder.AppendLine($"- {item}");

		return missingItemsBuilder.ToString();
	}
}