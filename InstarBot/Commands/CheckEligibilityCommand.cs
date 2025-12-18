using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Embeds;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Services;
using Serilog;

namespace PaxAndromeda.Instar.Commands;

[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class CheckEligibilityCommand(
    IDynamicConfigService dynamicConfig,
    IAutoMemberSystem autoMemberSystem,
	IDatabaseService ddbService,
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
            await RespondAsync(Strings.Command_CheckEligibility_Error_Internal, ephemeral: true);
        }

        if (!Context.User!.RoleIds.Contains(config.MemberRoleID) && !Context.User!.RoleIds.Contains(config.NewMemberRoleID))
        {
            await RespondAsync(Strings.Command_CheckEligibility_Error_NoMemberRoles, ephemeral: true);
            return;
        }

        if (Context.User!.RoleIds.Contains(config.MemberRoleID))
        {
            await RespondAsync(Strings.Command_CheckEligibility_Error_AlreadyMember, ephemeral: true);
            return;
        }
		
		bool isDDBAMH = false;
		try
		{
			var ddbUser = await ddbService.GetOrCreateUserAsync(Context.User);
			isDDBAMH = ddbUser.Data.AutoMemberHoldRecord is not null;
		} catch (Exception ex)
		{
			await metricService.Emit(Metric.AMS_DynamoFailures, 1);
			Log.Error(ex, "Failed to retrieve AMH status for user {UserID} from DynamoDB", Context.User.Id);
		}

		if (Context.User!.RoleIds.Contains(config.AutoMemberConfig.HoldRole) || isDDBAMH)
		{
			// User is on hold
			await RespondAsync(embed: new InstarCheckEligibilityAMHEmbed().Build(), ephemeral: true);
			return;
		}

        Log.Debug("Responding...");

		var eligibility = autoMemberSystem.CheckEligibility(config, Context.User);

		await RespondAsync(embed: new InstarCheckEligibilityEmbed(Context.User, eligibility, config).Build(), ephemeral: true);
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

		AutoMemberHoldRecord? amhRecord = null;
		bool hasError = false;
		try
		{
			var dbUser = await ddbService.GetOrCreateUserAsync(guildUser);
			if (dbUser.Data.AutoMemberHoldRecord is not null)
			{
				amhRecord = dbUser.Data.AutoMemberHoldRecord;
			}
		} catch (Exception ex)
		{
			Log.Error(ex, "Failed to retrieve user from DynamoDB while checking eligibility: {UserID}", user.Id);

			// Since we can't give exact details, we'll just note that there was an error
			// and just confirm that the member's AMH status is unknown.
			hasError = true;
		}

		await RespondAsync(embed: new InstarEligibilityEmbed(guildUser, eligibility, amhRecord, hasError).Build(), ephemeral: true);
	}
}