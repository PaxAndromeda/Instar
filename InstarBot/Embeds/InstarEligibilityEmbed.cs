using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord;
using PaxAndromeda.Instar.DynamoModels;

namespace PaxAndromeda.Instar.Embeds;

public class InstarEligibilityEmbed(IGuildUser user, MembershipEligibility eligibility, AutoMemberHoldRecord? amhRecord, bool error = false)
	: InstarAutoMemberSystemEmbed(Strings.Command_CheckEligibility_EmbedTitle)
{
	protected override EmbedBuilder BuildParts(EmbedBuilder builder)
	{
		var fields = new List<EmbedFieldBuilder>();

		bool hasAMH = false;
		if (amhRecord is not null)
		{
			var secondsSinceEpoch = (long) Math.Floor((amhRecord.Date - DateTime.UnixEpoch).TotalSeconds);
			fields.Add(new EmbedFieldBuilder()
				.WithName(Strings.Command_Eligibility_Section_Hold)
				.WithValue(string.Format(Strings.Command_Eligibility_HoldFormat, amhRecord.ModeratorID.ID, amhRecord.Reason, secondsSinceEpoch)));
			hasAMH = true;
		}

		if (!hasAMH && error)
		{
			fields.Add(new EmbedFieldBuilder()
					.WithName(Strings.Command_Eligibility_Section_AmbiguousHold)
					.WithValue(Strings.Command_Eligibility_Error_AmbiguousHold));
		}

		// Only add eligibility requirements if the user is not AMHed
		if (!hasAMH)
		{
			fields.Add(new EmbedFieldBuilder()
				.WithName(Strings.Command_Eligibility_Section_Requirements)
				.WithValue(BuildEligibilityText(eligibility)));
		}

		return builder
			.WithAuthor(new EmbedAuthorBuilder()
				.WithName(user.Username)
				.WithIconUrl(user.GetAvatarUrl())
			)
			.WithDescription(eligibility.HasFlag(MembershipEligibility.Eligible) ? Strings.Command_Eligibility_EligibleText : Strings.Command_Eligibility_IneligibleText)
			.WithFields(fields);
	}


	[ExcludeFromCodeCoverage(Justification = "This method's output is actually tested by observing the embed output.")]
	private static string BuildEligibilityText(MembershipEligibility eligibility)
	{
		var eligibilityBuilder = new StringBuilder();

		eligibilityBuilder.AppendLine(BuildEligibilitySnippet(Strings.Command_CheckEligibility_RolesEligibility, !eligibility.HasFlag(MembershipEligibility.MissingRoles)));
		eligibilityBuilder.AppendLine(BuildEligibilitySnippet(Strings.Command_CheckEligibility_IntroductionEligibility, !eligibility.HasFlag(MembershipEligibility.MissingIntroduction)));
		eligibilityBuilder.AppendLine(BuildEligibilitySnippet(Strings.Command_CheckEligibility_JoinAgeEligibility, !eligibility.HasFlag(MembershipEligibility.InadequateTenure)));
		eligibilityBuilder.AppendLine(BuildEligibilitySnippet(Strings.Command_CheckEligibility_ModActionsEligibility, !eligibility.HasFlag(MembershipEligibility.PunishmentReceived)));
		eligibilityBuilder.AppendLine(BuildEligibilitySnippet(Strings.Command_CheckEligibility_MessagesEligibility, !eligibility.HasFlag(MembershipEligibility.NotEnoughMessages)));

		return eligibilityBuilder.ToString();
	}

	[ExcludeFromCodeCoverage(Justification = "This method's output is actually tested by observing the embed output.")]
	private static string BuildEligibilitySnippet(string format, bool isEligible)
	{
		return string.Format(format, isEligible
			? Strings.Command_CheckEligibility_EligibleEmoji
			: Strings.Command_CheckEligibility_NotEligibleEmoji);
	}
}