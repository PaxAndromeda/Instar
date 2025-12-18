using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;
using Discord;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using Serilog;
using Metric = PaxAndromeda.Instar.Metrics.Metric;

namespace PaxAndromeda.Instar.Services;

public sealed class BirthdaySystem (
	IDynamicConfigService dynamicConfig,
	IDiscordService discord,
	IDatabaseService ddbService,
	IMetricService metricService,
	TimeProvider timeProvider)
	: ScheduledService("*/5 * * * *", timeProvider, metricService, "Birthday System"), IBirthdaySystem
{
	/// <summary>
	/// The maximum age to be considered 'valid' for age role assignment.
	/// </summary>
	private const int MaximumAge = 150;

	[ExcludeFromCodeCoverage]
	internal override Task Initialize()
	{
		// nothing to do
		return Task.CompletedTask;
	}

	public override async Task RunAsync()
	{
		var cfg = await dynamicConfig.GetConfig();
		var currentTime = timeProvider.GetUtcNow().UtcDateTime;

		await RemoveBirthdays(cfg, currentTime);
		var successfulAdds = await GrantBirthdays(cfg, currentTime);

		await metricService.Emit(Metric.BirthdaySystem_Grants, successfulAdds.Count);

		// Now we can create a happy announcement message
		if (successfulAdds.Count == 0)
			return;

		// Now let's draft up a birthday message
		await AnnounceBirthdays(cfg, successfulAdds);
	}

	private async Task AnnounceBirthdays(InstarDynamicConfiguration cfg, IEnumerable<Snowflake> users)
	{
		var channel = await discord.GetChannel(cfg.BirthdayConfig.BirthdayAnnounceChannel.ID);
		if (channel is not ITextChannel textChannel)
		{
			Log.Error("Cannot send birthday announcement, channel {ChannelID} not found.", cfg.BirthdayConfig.BirthdayAnnounceChannel.ID);
			return;
		}

		string mentions = Conjoin(users.Select(s => $"<@{s.ID}>").ToList());
		string message = string.Format(Strings.Birthday_Announcement, cfg.BirthdayConfig.BirthdayRole.ID, mentions);

		await textChannel.SendMessageAsync(message);
	}

	private static string Conjoin(IList<string> strings)
	{
		return strings.Count switch
		{
			0 => string.Empty,
			1 => strings[0],
			2 => $"{strings[0]} {Strings.JoiningConjunction} {strings[1]}",
			_ => string.Join(", ", strings.Take(strings.Count - 1)) + $", {Strings.JoiningConjunction} {strings.Last()}"
		};
	}

	private async Task RemoveBirthdays(InstarDynamicConfiguration cfg, DateTime currentTime)
	{
		var currentAppliedUsers = discord.GetAllUsersWithRole(cfg.BirthdayConfig.BirthdayRole).Select(n => new Snowflake(n.Id));

		var batchedUsers = await ddbService.GetBatchUsersAsync(currentAppliedUsers);

		List<Snowflake> toRemove = [ ];
		foreach (var user in batchedUsers)
		{
			if (user.Data.Birthday is null)
			{
				toRemove.Add(user.Data.UserID!);
				continue;
			}

			var birthDate = user.Data.Birthday.Birthdate;

			var thisYearBirthday = new DateTime(
				currentTime.Year,
				birthDate.Month, birthDate.Day, birthDate.Hour, birthDate.Minute, 0, DateTimeKind.Utc);

			if (thisYearBirthday > currentTime)
				thisYearBirthday = thisYearBirthday.AddYears(-1);


			if (currentTime - thisYearBirthday >= TimeSpan.FromDays(1))
				toRemove.Add(user.Data.UserID!);
		}

		foreach (var snowflake in toRemove)
		{
			var guildUser = discord.GetUser(snowflake);
			if (guildUser is null)
			{
				Log.Warning("Cannot grant birthday role to {UserID} as they were not found on the server.", snowflake.ID);
				continue;
			}

			await guildUser.RemoveRoleAsync(cfg.BirthdayConfig.BirthdayRole);
		}
	}

	private async Task<List<Snowflake>> GrantBirthdays(InstarDynamicConfiguration cfg, DateTime currentTime)
	{
		List<InstarDatabaseEntry<InstarUserData>> dbResults = [ ];

		await discord.SyncUsers();

		try
		{
			// Get all users with birthdays ±15 minutes from the current time.
			// BUG: could be off if the timer drifts. maybe we need a metric for expected runtime vs actual runtime?
			dbResults.AddRange(await ddbService.GetUsersByBirthday(currentTime, TimeSpan.FromMinutes(10)));
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to run birthday routine");
			await metricService.Emit(Metric.BirthdaySystem_Failures, 1);
			return [ ];
		}

		// list of user IDs to mention in the happy birthday message
		List<Snowflake> toMention = [ ];

		foreach (var result in dbResults)
		{
			var userId = result.Data.UserID;
			if (userId is null)
				continue;

			try
			{
				var guildUser = discord.GetUser(userId);
				if (guildUser is null)
				{
					Log.Warning("Cannot grant birthday role to {UserID} as they were not found on the server.", userId.ID);
					continue;
				}

				toMention.Add(userId);

				await GrantBirthdayRole(cfg, guildUser, result.Data.Birthday);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to apply birthday role to {UserID}", userId.ID);
			}
		}

		return toMention;
	}

	private static async Task UpdateAgeRole(InstarDynamicConfiguration cfg, IGuildUser user, int ageYears)
	{
		Guard.Against.Null(cfg);
		Guard.Against.Null(user);

		// TODO: update this whenever someone in the world turns 200 years old
		Guard.Against.OutOfRange(ageYears, nameof(ageYears), 0, 200);

		// If the user's age is below the youngest age role, there's nothing to do
		if (ageYears < cfg.BirthdayConfig.AgeRoleMap.Min(n => n.Age))
			return;

		// Find the appropriate age role to assign. If the current age exceeds
		// the maximum age role mapping, we assign the maximum age role.
		Snowflake? roleToAssign;

		var maxAgeMap = cfg.BirthdayConfig.AgeRoleMap.MaxBy(n => n.Age);
		if (maxAgeMap is null)
			throw new BadStateException("Failed to read age role map from dynamic configuration");

		if (ageYears >= maxAgeMap.Age)
		{
			roleToAssign = maxAgeMap.Role;
		} else
		{
			// Find the closest age role that does not exceed the user's age
			roleToAssign = cfg.BirthdayConfig.AgeRoleMap
				.Where(n => n.Age <= ageYears)
				.OrderByDescending(n => n.Age)
				.Select(n => n.Role)
				.FirstOrDefault();
		}

		// Maybe missing a mapping somewhere?
		if (roleToAssign is null)
		{
			Log.Warning("Failed to find appropriate age role for user who is {UserAge} years old.", ageYears);
			return;
		}

		// We need to identify every age role the user has and remove them first
		await user.RemoveRolesAsync(cfg.BirthdayConfig.AgeRoleMap.Select(n => n.Role.ID).Where(user.RoleIds.Contains));
		await user.AddRoleAsync(roleToAssign);
	}

	private async Task GrantBirthdayRole(InstarDynamicConfiguration cfg, IGuildUser user, Birthday? birthday)
	{
		await user.AddRoleAsync(cfg.BirthdayConfig.BirthdayRole);

		if (birthday is null)
			return;

		int yearsOld = birthday.Age;

		if (yearsOld < MaximumAge)
			await UpdateAgeRole(cfg, user, yearsOld);
	}

	public async Task GrantUnexpectedBirthday(IGuildUser user, Birthday birthday)
	{
		var cfg = await dynamicConfig.GetConfig();

		await GrantBirthdayRole(cfg, user, birthday);
		await AnnounceBirthdays(cfg, [new Snowflake(user.Id)]);
	}
}