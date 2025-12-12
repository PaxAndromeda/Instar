using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using PaxAndromeda.Instar.Preconditions;
using PaxAndromeda.Instar.Services;
using Serilog;

namespace PaxAndromeda.Instar.Commands;

public class ResetBirthdayCommand(IInstarDDBService ddbService, IDynamicConfigService dynamicConfig) : BaseCommand
{
	/*
	 * Concern: This command runs very slowly, and might end up hitting the 3-second limit from Discord.
	 * If we start seeing timeouts, we may need to make the end user notification asynchronous.
	 */
	[UsedImplicitly]
	[SlashCommand("resetbirthday", "Resets a user's birthday, allowing them to set it again.")]
	[RequireStaffMember]
	public async Task ResetBirthday(
			[Summary("user", "The user to reset the birthday of.")]
			IUser user
		)
	{
		try
		{
			var dbUser = await ddbService.GetUserAsync(user.Id);

			if (dbUser is null)
			{
				await RespondAsync(string.Format(Strings.Command_ResetBirthday_Error_UserNotFound, user.Id), ephemeral: true);
				return;
			}

			dbUser.Data.Birthday = null;
			dbUser.Data.Birthdate = null;
			await dbUser.UpdateAsync();

			if (user is IGuildUser guildUser)
			{
				var cfg = await dynamicConfig.GetConfig();

				if (guildUser.RoleIds.Contains(cfg.BirthdayConfig.BirthdayRole))
				{
					try
					{
						await guildUser.RemoveRoleAsync(cfg.BirthdayConfig.BirthdayRole);
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Failed to remove birthday role from user {UserID}", user.Id);

						await RespondAsync(string.Format(Strings.Command_ResetBirthday_Error_RemoveBirthdayRole, user.Id));
						return;
					}
				}

				try
				{
					await guildUser.SendMessageAsync(Strings.Command_ResetBirthday_EndUserNotification);
				} catch (Exception dmEx)
				{
					Log.Error(dmEx, "Failed to send a DM to user {UserID}", user.Id);
					// ignore
				}
			}

			await RespondAsync(string.Format(Strings.Command_ResetBirthday_Success, user.Id), ephemeral: true);
		} catch (Exception ex)
		{
			Log.Error(ex, "Failed to reset the birthday of {UserID}", user.Id);
			await RespondAsync(string.Format(Strings.Command_ResetBirthday_Error_Unknown, user.Id), ephemeral: true);
		}
	}
}