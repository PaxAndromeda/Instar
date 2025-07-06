using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Services;
using Serilog;

namespace PaxAndromeda.Instar.Commands;

[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class CheckEligibilityCommand(
    IDynamicConfigService dynamicConfig,
    AutoMemberSystem autoMemberSystem,
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
        
        var eligibility = await autoMemberSystem.CheckEligibility(Context.User);

        Log.Debug("Building response embed...");
        var fields = new List<EmbedFieldBuilder>();
        if (eligibility.HasFlag(MembershipEligibility.NotEligible))
        {
            fields.Add(new EmbedFieldBuilder()
                .WithName("Missing Items")
                .WithValue(await BuildMissingItemsText(eligibility, Context.User)));
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
                .WithText("Instar Auto Member System System")
                .WithIconUrl("https://spacegirl.s3.us-east-1.amazonaws.com/instar.png"))
            .WithTitle("Membership Eligibility")
            .WithDescription(BuildEligibilityText(eligibility))
            .WithFields(fields);

        Log.Debug("Responding...");
        await RespondAsync(embed: builder.Build(), ephemeral: true);
        await metricService.Emit(Metric.AMS_EligibilityCheck, 1);
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