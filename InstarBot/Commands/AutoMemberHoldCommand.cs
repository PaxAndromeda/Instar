using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;
using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Services;
using Serilog;

namespace PaxAndromeda.Instar.Commands;

[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class AutoMemberHoldCommand(IInstarDDBService ddbService, IDynamicConfigService dynamicConfigService, IMetricService metricService) : BaseCommand
{
	[UsedImplicitly]
	[SlashCommand("amh", "Withhold automatic membership grants to a user.")]
	public async Task HoldMember(
		[Summary("user", "The user to withhold automatic membership from.")]
		IUser user,
		[Summary("reason", "The reason for withholding automatic membership.")]
		string reason
		)
	{
		Guard.Against.NullOrEmpty(reason);
		Guard.Against.Null(Context.User);
		Guard.Against.Null(user);

		var date = DateTime.UtcNow;
		Snowflake modId = Context.User.Id;

		try
		{
			if (user is not IGuildUser guildUser)
			{
				await RespondAsync($"Error while attempting to withhold membership for <@{user.Id}>: User is not a guild member.", ephemeral: true);
				return;
			}

			var config = await dynamicConfigService.GetConfig();

			if (guildUser.RoleIds.Contains(config.MemberRoleID))
			{
				await RespondAsync($"Error while attempting to withhold membership for <@{user.Id}>: User is already a member.", ephemeral: true);
				return;
			}

			var dbUser = await ddbService.GetOrCreateUserAsync(guildUser);

			dbUser.Data.AutoMemberHoldRecord = new AutoMemberHoldRecord
			{
				ModeratorID = modId,
				Reason = reason,
				Date = date
			};
			await dbUser.UpdateAsync();

			// TODO: configurable duration?
			await RespondAsync($"Membership for user <@{user.Id}> has been withheld. Staff will be notified in one week to review.", ephemeral: true);
		} catch (Exception ex)
		{
			await metricService.Emit(Metric.AMS_AMHFailures, 1);
			Log.Error(ex, "Failed to apply auto member hold requested by {ModID} to {UserID} for reason: \"{Reason}\"", modId.ID, user.Id, reason);
			
			try
			{
				// It is entirely possible that RespondAsync threw this error.
				await RespondAsync($"Error while attempting to withhold membership for <@{user.Id}>: An unexpected error has occurred while configuring the AMH.", ephemeral: true);
			} catch
			{
				// swallow the exception
			}
		}
	}

	[UsedImplicitly]
	[SlashCommand("removeamh", "Removes an auto member hold from the user.")]
	public async Task UnholdMember(
		[Summary("user", "The user to remove the auto member hold from.")]
		IUser user
		)
	{
		Guard.Against.Null(Context.User);
		Guard.Against.Null(user);

		if (user is not IGuildUser guildUser)
		{
			await RespondAsync($"Error while attempting to remove auto member hold for <@{user.Id}>: User is not a guild member.", ephemeral: true);
			return;
		}

		try
		{
			var dbUser = await ddbService.GetOrCreateUserAsync(guildUser);
			if (dbUser.Data.AutoMemberHoldRecord is null)
			{
				await RespondAsync($"Error while attempting to remove auto member hold for <@{user.Id}>: User does not have an active auto member hold.", ephemeral: true);
				return;
			}

			dbUser.Data.AutoMemberHoldRecord = null;
			await dbUser.UpdateAsync();

			await RespondAsync($"Auto member hold for user <@{user.Id}> has been removed.", ephemeral: true);
		}
		catch (Exception ex)
		{
			await metricService.Emit(Metric.AMS_AMHFailures, 1);
			Log.Error(ex, "Failed to remove auto member hold requested by {ModID} from {UserID}", Context.User.Id, user.Id);

			try
			{
				await RespondAsync($"Error while attempting to remove auto member hold for <@{user.Id}>: An unexpected error has occurred while removing the AMH.", ephemeral: true);
			}
			catch
			{
				// swallow the exception
			}
		}
	}
}