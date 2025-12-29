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
public class AutoMemberHoldCommand(IDatabaseService ddbService, IDynamicConfigService dynamicConfigService, IMetricService metricService, INotificationService notificationService, TimeProvider timeProvider) : BaseCommand
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

		var date = timeProvider.GetUtcNow().UtcDateTime;
		Snowflake modId = Context.User.Id;

		try
		{
			if (user is not IGuildUser guildUser)
			{
				
				await RespondAsync(string.Format(Strings.Command_AutoMemberHold_Error_NotGuildMember, user.Id), ephemeral: true);
				return;
			}

			var config = await dynamicConfigService.GetConfig();

			if (guildUser.RoleIds.Contains(config.MemberRoleID))
			{
				await RespondAsync(string.Format(Strings.Command_AutoMemberHold_Error_AlreadyMember, user.Id), ephemeral: true);
				return;
			}

			var dbUser = await ddbService.GetOrCreateUserAsync(guildUser);

			if (dbUser.Data.AutoMemberHoldRecord is not null)
			{
				await RespondAsync(string.Format(Strings.Command_AutoMemberHold_Error_AMHAlreadyExists, user.Id), ephemeral: true);
				return;
			}

			dbUser.Data.AutoMemberHoldRecord = new AutoMemberHoldRecord
			{
				ModeratorID = modId,
				Reason = reason,
				Date = date
			};
			await dbUser.CommitAsync();

			// Create a notification for the future
			await notificationService.QueueNotification(new Notification
			{
				Actor = modId,
				Channel = config.StaffAnnounceChannel,
				Type = NotificationType.AutoMemberHold,
				Priority = NotificationPriority.Normal,
				Subject = "Auto Member Hold Reminder",
				Targets = [
					new NotificationTarget { Id = config.StaffRoleID, Type = NotificationTargetType.Role },
					new NotificationTarget { Id = modId, Type = NotificationTargetType.User }
				],
				Data = new NotificationData
				{
					Message = Strings.Command_AutoMemberHold_NotificationMessage,
					Fields = [
						new NotificationEmbedField { Name = "**User**", Value = $"<@{user.Id}>\r\n`{user.Id}`", Inline = true },
						new NotificationEmbedField { Name = "**Issuer**", Value = $"<@{modId.ID}>\r\n`{modId.ID}`", Inline = true },
						new NotificationEmbedField { Name = "**Reason**", Value = $"```{reason}```" }
					]
				},
				ReferenceUser = user.Id
			}, TimeSpan.FromDays(7));

			// TODO: configurable duration?
			await RespondAsync(string.Format(Strings.Command_AutoMemberHold_Success, user.Id), ephemeral: true);
		} catch (Exception ex)
		{
			await metricService.Emit(Metric.AMS_AMHFailures, 1);
			Log.Error(ex, "Failed to apply auto member hold requested by {ModID} to {UserID} for reason: \"{Reason}\"", modId.ID, user.Id, reason);
			
			try
			{
				// It is entirely possible that RespondAsync threw this error.
				await RespondAsync(string.Format(Strings.Command_AutoMemberHold_Error_Unexpected, user.Id), ephemeral: true);
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
			await RespondAsync(string.Format(Strings.Command_AutoMemberUnhold_Error_NotGuildMember, user.Id), ephemeral: true);
			return;
		}

		try
		{
			var dbUser = await ddbService.GetOrCreateUserAsync(guildUser);
			if (dbUser.Data.AutoMemberHoldRecord is null)
			{
				await RespondAsync(string.Format(Strings.Command_AutoMemberUnhold_Error_NoActiveHold, user.Id), ephemeral: true);
				return;
			}

			dbUser.Data.AutoMemberHoldRecord = null;
			await dbUser.CommitAsync();

			// Purge any pending notifications asynchronously.
			_ = Task.Factory.StartNew(() => PurgeNotification(user.Id));

			await RespondAsync(string.Format(Strings.Command_AutoMemberUnhold_Success, user.Id), ephemeral: true);
		}
		catch (Exception ex)
		{
			await metricService.Emit(Metric.AMS_AMHFailures, 1);
			Log.Error(ex, "Failed to remove auto member hold requested by {ModID} from {UserID}", Context.User.Id, user.Id);

			try
			{
				await RespondAsync(string.Format(Strings.Command_AutoMemberUnhold_Error_Unexpected, user.Id), ephemeral: true);
			}
			catch
			{
				// swallow the exception
			}
		}
	}

	private async Task PurgeNotification(Snowflake userId)
	{
		try
		{
			var results = await ddbService.GetNotificationsByTypeAndReferenceUser(NotificationType.AutoMemberHold, userId);

			foreach (var result in results)
			{
				Log.Debug("Deleting AMH notification for {UserID} dated {Date}", userId.ID, result.Data.Date);
				await result.DeleteAsync();
			}
		} catch (Exception ex)
		{
			Log.Error(ex, "Failed to remove AMH notification for user {UserID}", userId.ID);
		}
	}
}