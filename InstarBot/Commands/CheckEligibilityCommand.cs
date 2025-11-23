using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Services;
using Serilog;

namespace PaxAndromeda.Instar.Commands;

[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class CheckEligibilityCommand(
    IDynamicConfigService dynamicConfig,
    IAutoMemberSystem autoMemberSystem,
	IInstarDDBService ddbService,
    IMetricService metricService)
    : BaseCommand
{
    [UsedImplicitly]
    [SlashCommand("checkeligibility", "This command checks your membership eligibility.")]
    public async Task CheckEligibility()
    {
        var config = await dynamicConfig.GetConfig();
        
        if (Context.User is null)
        {
            Log.Error("Checking eligibility, but Context.User is null");
            await RespondAsync("An internal error has occurred.  Please try again later.", ephemeral: true);
        }

        if (!Context.User!.RoleIds.Contains(config.MemberRoleID) && !Context.User!.RoleIds.Contains(config.NewMemberRoleID))
        {
            await RespondAsync("You do not have the New Member or Member roles.  Please contact staff to have this corrected.", ephemeral: true);
            return;
        }

        if (Context.User!.RoleIds.Contains(config.MemberRoleID))
        {
            await RespondAsync("You are already a member!", ephemeral: true);
            return;
        }

		if (Context.User!.RoleIds.Contains(config.AutoMemberConfig.HoldRole))
		{
			// User is on hold
			await RespondAsync(embed: BuildAMHEmbed(), ephemeral: true);
			return;
		}

		var embed = await BuildEligibilityEmbed(config, Context.User);

        Log.Debug("Responding...");
        await RespondAsync(embed: embed, ephemeral: true);
        await metricService.Emit(Metric.AMS_EligibilityCheck, 1);
    }

	[UsedImplicitly]
	[SlashCommand("eligibility", "Checks the eligibility of another user on the server.")]
	public async Task CheckOtherEligibility(IUser user)
	{
		if (user is not IGuildUser guildUser)
		{
			await RespondAsync($"Cannot check the eligibility for {user.Id} since they are not on this server.", ephemeral: true);
			return;
		}

		var cfg = await dynamicConfig.GetConfig();

		var eligibility = autoMemberSystem.CheckEligibility(cfg, guildUser);

		// Let's build a fancy embed
		var fields = new List<EmbedFieldBuilder>();

		bool hasAMH = false;
		try
		{
			var dbUser = await ddbService.GetOrCreateUserAsync(guildUser);
			if (dbUser.Data.AutoMemberHoldRecord is not null)
			{
				StringBuilder amhContextBuilder = new();
				amhContextBuilder.AppendLine($"**Mod:** <@{dbUser.Data.AutoMemberHoldRecord.ModeratorID.ID}>");
				amhContextBuilder.AppendLine("**Reason:**");
				amhContextBuilder.AppendLine($"```{dbUser.Data.AutoMemberHoldRecord.Reason}```");

				amhContextBuilder.Append("**Date:** ");

				var secondsSinceEpoch = (long) Math.Floor((dbUser.Data.AutoMemberHoldRecord.Date - DateTime.UnixEpoch).TotalSeconds);
				amhContextBuilder.Append($"<t:{secondsSinceEpoch}:f> (<t:{secondsSinceEpoch}:R>)");

				fields.Add(new EmbedFieldBuilder()
					.WithName(":warning: Auto Member Hold")
					.WithValue(amhContextBuilder.ToString()));
				hasAMH = true;
			}
		} catch (Exception ex)
		{
			Log.Error(ex, "Failed to retrieve user from DynamoDB while checking eligibility: {UserID}", user.Id);

			// Since we can't give exact details, we'll just note that there was an error
			// and just confirm that the member's AMH status is unknown.
			fields.Add(new EmbedFieldBuilder()
					.WithName(":warning: Possible Auto Member Hold")
					.WithValue("Instar encountered an error while attempting to retrieve AMH details. If the user is otherwise eligible but is not being granted membership, it is possible that they are AMHed. Please try again later."));
		}

		// Only add eligibility requirements if the user is not AMHed
		if (!hasAMH)
		{
			fields.Add(new EmbedFieldBuilder()
				.WithName(":small_blue_diamond: Requirements")
				.WithValue(BuildEligibilityText(eligibility)));
		}

		var builder = new EmbedBuilder()
			.WithCurrentTimestamp()
			.WithTitle("Membership Eligibility")
			.WithFooter(new EmbedFooterBuilder()
				.WithText("Instar Auto Member System")
				.WithIconUrl("https://spacegirl.s3.us-east-1.amazonaws.com/instar.png"))
			.WithAuthor(new EmbedAuthorBuilder()
				.WithName(user.Username)
				.WithIconUrl(user.GetAvatarUrl()))
			.WithDescription($"At this time, <@{user.Id}> is " + (!eligibility.HasFlag(MembershipEligibility.Eligible) ? "__not__ " : "") + " eligible for membership.")
			.WithFields(fields);

		await RespondAsync(embed: builder.Build(), ephemeral: true);
	}

	private static Embed BuildAMHEmbed()
	{
		var fields = new List<EmbedFieldBuilder>
		{
			new EmbedFieldBuilder()
				.WithName("Why?")
				.WithValue("Your activity has been flagged as unusual by our system. This can happen for a variety of reasons, including antisocial behavior, repeated rule violations in a short period of time, or other behavior that is deemed disruptive."),
			new EmbedFieldBuilder()
				.WithName("What can I do?")
				.WithValue("The staff will override an administrative hold in time when an evaluation of your behavior has been completed by the staff."),
			new EmbedFieldBuilder()
				.WithName("Should I contact Staff?")
				.WithValue("No, the staff will not accelerate this process by request.")
		};

		var builder = new EmbedBuilder()
			// Set up all the basic stuff first
			.WithCurrentTimestamp()
			.WithFooter(new EmbedFooterBuilder()
				.WithText("Instar Auto Member System")
				.WithIconUrl("https://spacegirl.s3.us-east-1.amazonaws.com/instar.png"))
			.WithTitle("Membership Eligibility")
			.WithDescription("Your membership is currently on hold due to an administrative override. This means that you will not be granted the Member role automatically.")
			.WithFields(fields);

		return builder.Build();
	}

	private async Task<Embed> BuildEligibilityEmbed(InstarDynamicConfiguration config, IGuildUser user)
	{
		var eligibility = autoMemberSystem.CheckEligibility(config, user);

		Log.Debug("Building response embed...");
		var fields = new List<EmbedFieldBuilder>();
		if (eligibility.HasFlag(MembershipEligibility.NotEligible))
		{
			fields.Add(new EmbedFieldBuilder()
				.WithName("Missing Items")
				.WithValue(await BuildMissingItemsText(eligibility, user)));
		}

		var nextRun = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month,
						  DateTimeOffset.UtcNow.Day, DateTimeOffset.UtcNow.Hour, 0, 0, TimeSpan.Zero)
					  + TimeSpan.FromHours(1);
		var unixTime = nextRun.UtcTicks / 10000000-62135596800; // UTC ticks since year 0 to Unix Timestamp

		fields.Add(new EmbedFieldBuilder()
			.WithName("Note")
			.WithValue($"The Auto Member System will run <t:{unixTime}:R>. Membership eligibility is subject to change at the time of evaluation."));

		var builder = new EmbedBuilder()
            // Set up all the basic stuff first
            .WithCurrentTimestamp()
			.WithColor(0x0c94e0)
			.WithFooter(new EmbedFooterBuilder()
				.WithText("Instar Auto Member System")
				.WithIconUrl("https://spacegirl.s3.us-east-1.amazonaws.com/instar.png"))
			.WithTitle("Membership Eligibility")
			.WithDescription(BuildEligibilityText(eligibility))
			.WithFields(fields);
		
		return builder.Build();
	}

    private async Task<string> BuildMissingItemsText(MembershipEligibility eligibility, IGuildUser user)
    {
        var config = await dynamicConfig.GetConfig();
        
        if (eligibility == MembershipEligibility.Eligible)
            return string.Empty;
        
        var missingItemsBuilder = new StringBuilder();

        if (eligibility.HasFlag(MembershipEligibility.MissingRoles))
        {
            // What roles are we missing?
            foreach (var roleGroup in config.AutoMemberConfig.RequiredRoles)
            {
                if (user.RoleIds.Intersect(roleGroup.Roles.Select(n => n.ID)).Any()) continue;
                var prefix = "aeiouAEIOU".Contains(roleGroup.GroupName[0]) ? "an" : "a"; // grammar hack :)
                missingItemsBuilder.AppendLine(
                    $"- You are missing {prefix} {roleGroup.GroupName.ToLowerInvariant()} role.");
            }
        }

        if (eligibility.HasFlag(MembershipEligibility.MissingIntroduction))
            missingItemsBuilder.AppendLine($"- You have not posted an introduction in {Snowflake.GetMention(() => config.AutoMemberConfig.IntroductionChannel)}.");

        if (eligibility.HasFlag(MembershipEligibility.TooYoung))
            missingItemsBuilder.AppendLine(
                $"- You have not been on the server for {config.AutoMemberConfig.MinimumJoinAge / 3600} hours yet.");

        if (eligibility.HasFlag(MembershipEligibility.PunishmentReceived))
            missingItemsBuilder.AppendLine("- You have received a warning or moderator action.");

        if (eligibility.HasFlag(MembershipEligibility.NotEnoughMessages))
            missingItemsBuilder.AppendLine($"- You have not posted {config.AutoMemberConfig.MinimumMessages} messages in the past {config.AutoMemberConfig.MinimumMessageTime/3600} hours.");

        return missingItemsBuilder.ToString();
    }

	private static string BuildEligibilityText(MembershipEligibility eligibility)
	{
		var eligibilityBuilder = new StringBuilder();
		eligibilityBuilder.Append(eligibility.HasFlag(MembershipEligibility.MissingRoles)
			? ":x:"
			: ":white_check_mark:");
		eligibilityBuilder.AppendLine(" **Roles**");
		eligibilityBuilder.Append(eligibility.HasFlag(MembershipEligibility.MissingIntroduction)
			? ":x:"
			: ":white_check_mark:");
		eligibilityBuilder.AppendLine(" **Introduction**");
		eligibilityBuilder.Append(eligibility.HasFlag(MembershipEligibility.TooYoung)
			? ":x:"
			: ":white_check_mark:");
		eligibilityBuilder.AppendLine(" **Join Age**");
		eligibilityBuilder.Append(eligibility.HasFlag(MembershipEligibility.PunishmentReceived)
			? ":x:"
			: ":white_check_mark:");
		eligibilityBuilder.AppendLine(" **Mod Actions**");
		eligibilityBuilder.Append(eligibility.HasFlag(MembershipEligibility.NotEnoughMessages)
			? ":x:"
			: ":white_check_mark:");
		eligibilityBuilder.AppendLine(" **Messages** (last 24 hours)");

		return eligibilityBuilder.ToString();
	}
}